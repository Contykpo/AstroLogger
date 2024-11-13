using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;

namespace AstroLogger
{
    /// <summary>
    /// Helper class for applying a format to any instance of <see cref="Message"/>
    /// </summary>
    public static class FormatConverter
    {
        #region Fields

        /// <summary>
        /// Dictionary that associates a format to a function that applies said format.
        /// </summary>
        private static Dictionary<string, Func<Message, string>> mCurrentFormats = new Dictionary<string, Func<Message, string>>();

        #endregion

        #region Methods

        public static string ApplyFormat(this Message message, string format)
        {
            // If the format is empty, return the given message content with no format.
            if (string.IsNullOrWhiteSpace(format)) return message.Content;

            // If the format does not exist:
            if (!mCurrentFormats.ContainsKey(format))
            {
                // We try to add it as a new format to our dictionary.
                // If we couldn't add the new format, return the given message content with no format.
                if (!AddNewFormat(format)) return message.Content;
            }

            // Call the function associated to the given format.
            return mCurrentFormats[format](message);
        }

        /// <summary>
        /// Builds a function that formats the content of a message and then adding said function to the dictionary.
        /// This is an expensive function, so we might want to call it on separate thread.
        /// </summary>
        /// <param name="format">Format for which the function will be built</param>
        /// <returns>A <see cref="bool"/> value that indicates whether the function could be built and added to the dictionary</returns>
        public static bool AddNewFormat(string format)
        {
            // Create a new converter and call its function to create the function that will apply the format.
            var formatFunction = new Converter().CreateFormatFunction(format);

            // If the converter failed to create the function, return false.
            if (formatFunction == null) return false;

            // If it didn't fail, we add it to the dictionary.
            mCurrentFormats.Add(format, formatFunction);

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Internal class for generating a <see cref="Func{Message, string}"/> from a format string.
    /// </summary>
    internal class Converter
    {
        #region Fields

        private bool mAppendedMessage = false;

        private ParameterExpression messageParameter = Expression.Parameter(typeof(Message), "message");

        #endregion

        #region Methods

        /// <summary>
        /// Takes a format and creates a function that applies that format.
        /// </summary>
        /// <param name="format"><see cref="string"/> that contains the format of the message content.</param>
        /// <returns>A lambda that takes a <see cref="Message"/> and returns the message content of type <see cref="string"/> which has the given format applied to it.</returns>
        public Func<Message, string> CreateFormatFunction(string format)
        {
            // String Builder instance to build the content string.
            ParameterExpression stringBuilder = Expression.Parameter(typeof(StringBuilder), "StringBuilder");

            MethodInfo appendMethodStringBuilder = typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), new Type[] { typeof(string) });

            if (appendMethodStringBuilder == null) return null;

            // Regular Expression that separates the format arguments from simple chracters.
            Regex separateFormat = new Regex(@"(?<Argumento>%[a-z](?(?=-)-(?(?=[a-z0-9])(?(?=[a-z])[a-z]|[0-9](\.[0-9]?)?)|\.[0-9]?)|))|(?<Palabra>[^%]*)", RegexOptions.IgnoreCase);

            // Collection of the parts of the string that matched the expression.
            MatchCollection matches = separateFormat.Matches(format);

            // List with the parts of the string separated into arguments and characters.
            List<string> formatSections = new List<string>(matches.Count);

            for (int i = 0; i < matches.Count; ++i)
            {
                formatSections.Add(matches[i].Groups["Palabra"].Length > 0 ? matches[i].Groups["Palabra"].Value : matches[i].Groups["Argumento"].Value);
            }

            // List containing the expressions in order of execution.
            List<Expression> expressions = new List<Expression>();

            // We create a new instance of a String Builder.
            expressions.Add(Expression.Assign(stringBuilder, Expression.New(typeof(StringBuilder))));

            // For section found:
            for (int i = 0; i < formatSections.Count; ++i)
            {
                // If the section is a format argument:
                if (formatSections[i].StartsWith('%'))
                {
                    // We split it.
                    string[] sectionArgs = formatSections[i].Split("-");

                    // And pass it to the GetExpression function and assign returned expression to the stringToAppend parameter.
                    expressions.Add(Expression.Call(stringBuilder, appendMethodStringBuilder, GetExpression(sectionArgs[0], sectionArgs.Length > 1 ? sectionArgs[1] : string.Empty)));
                }
                else
                {
                    // If the section is not an argument, we add it as new argument.
                    expressions.Add(Expression.Call(stringBuilder, appendMethodStringBuilder, Expression.Constant(formatSections[i], typeof(string))));
                }
            }

            // If the message hasn't been appended yet, we append it now:
            if (!mAppendedMessage)
            {
                expressions.Add(Expression.Call(stringBuilder, appendMethodStringBuilder, Expression.Field(messageParameter, nameof(Message.Content))));
            }

            MethodInfo stringBuilderToString = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), new Type[] { });

            if (stringBuilderToString == null) return null!;

            // Expression that calls the StringBuilder's ToString method.
            MethodCallExpression strBldToStrMthd = Expression.Call(stringBuilder, stringBuilderToString);

            expressions.Add(strBldToStrMthd);

            expressions.TrimExcess();

            // Expression block containing all the expressions we just created.
            BlockExpression fullExpression = Expression.Block(new[] { stringBuilder }, expressions);

            // We reate a lambda using that block of expressions, compile it and return the result.
            return Expression.Lambda<Func<Message, string>>(fullExpression, messageParameter).Compile();
        }

        /// <summary>
        /// Generates an expression that translates a format argument and its options to a <see cref="string"/>
        /// </summary>
        /// <param name="formatArgument">Format argument</param>
        /// <param name="options">Argument options</param>
        /// <returns>An <see cref="Expression"/> that translates the given argument to a <see cref="string"/></returns>
        private Expression GetExpression(string formatArgument, string options)
        {
            // Remove whitespaces to avoid problems while getting the argument.
            formatArgument = formatArgument.Trim();
            options = options.Trim();

            try
            {
                switch (formatArgument)
                {
                    // Date:
                    case "%t":

                        // Get the property of the DateTime class that gives us the current date and time.
                        PropertyInfo propertyInfo = typeof(DateTime).GetProperty(nameof(DateTime.Now), BindingFlags.Static | BindingFlags.Public);

                        // Expression that calls the Get method of the property.
                        MethodCallExpression getExpressionDateMethod = Expression.Call(null, propertyInfo!.GetMethod!);

                        // ToString method of the DateTime class.
                        MethodInfo toStringMethod = typeof(DateTime).GetMethod(nameof(DateTime.ToString), new Type[] { typeof(string) });

                        // Get the current date and time and then call its ToString method.
                        return Expression.Call(getExpressionDateMethod, toStringMethod!, Expression.Constant(options));

                    // Message:
                    case "%m":

                        mAppendedMessage = true;

                        // Get the value of the message content field.
                        return Expression.Field(messageParameter, typeof(Message), nameof(Message.Content));

                    // Severity:
                    case "%s":

                        // Constant expression representing the 'severity' field of the message class.
                        ConstantExpression messageSeverityExpressionField = Expression.Constant(typeof(Message).GetField(nameof(Message.Severity)));

                        // Expression that calls the GetValue method of the Reflection.FieldInfo class using the field we previously obtained.
                        MethodCallExpression infoGetValueExpressionField = Expression.Call(
                            messageSeverityExpressionField,
                            typeof(FieldInfo).GetMethod(nameof(FieldInfo.GetValue), new[] { typeof(object) }),
                            // Message parameters.
                            messageParameter);

                        MethodInfo getNameMethod = typeof(Enum).GetMethod(nameof(Enum.GetName), new[] { typeof(Type), typeof(object) });

                        // Expression that calls the GetName method of the enum class.
                        // This way we can obtain the string containing the name of the severity.
                        MethodCallExpression getNameExpression = Expression.Call(
                            null,
                            getNameMethod,
                            // Parameters.
                            Expression.Constant(typeof(ESeverity)), infoGetValueExpressionField);

                        // If no arguments were provided, we simply return the name of the expression.
                        if (string.IsNullOrWhiteSpace(options)) return getNameExpression;

                        // If there is an option argument:
                        switch (options)
                        {
                            // Change capitalization to uppercase.
                            case "u":
                                {
                                    MethodInfo toUpperMethodInfo = typeof(string).GetMethod(nameof(string.ToUpper), new Type[] { });

                                    Expression toUpperExpression = Expression.Call(getNameExpression, toUpperMethodInfo);

                                    return toUpperExpression;
                                }
                            // Change capitalization to lowercase.
                            case "l":
                                {
                                    MethodInfo toLowerMethodInfo = typeof(string).GetMethod(nameof(string.ToLower), new Type[] { });

                                    Expression toLowerExpression = Expression.Call(getNameExpression, toLowerMethodInfo);

                                    return toLowerExpression;
                                }
                            // If the given option argument is not valid, we return the default name of the expression.
                            default:
                                {
                                    return getNameExpression;
                                }
                        }

                    // Vertical line:
                    case "%v":
                    {
                        // If the argument is empty or contains characters that are not accepted, return a default line.
                        if (string.IsNullOrWhiteSpace(options) || Regex.IsMatch(options, "[^0-9.]"))
                        {
                            return Expression.Constant($"{Environment.NewLine}----------------------------------------{Environment.NewLine}");
                        }
                        else
                        {
                            int prelude = 0;
                            int finale = 0;

                            // If there is a single argument:
                            if (!options.Contains("."))
                            {
                                // We assign the same value to the prelude and the finale.
                                prelude = Math.Clamp(int.Parse(options), 1, int.MaxValue);
                                finale = prelude;
                            }
                            // If there are two arguments:
                            else
                            {
                                // Split into parts:
                                string[] optionParts = options.Split(".");

                                // If the string is empty we initialize the prelude to 1.
                                if (string.IsNullOrWhiteSpace(optionParts[0]))
                                {
                                    prelude = 1;
                                }
                                // If the string is not empty, parse its value:
                                else
                                {
                                    prelude = Math.Clamp(int.Parse(optionParts[0]), 1, int.MaxValue);
                                }

                                // Repeat the same process for the second variable in the option parts:

                                if (string.IsNullOrWhiteSpace(optionParts[1]))
                                {
                                    finale = 1;
                                }
                                else
                                {
                                    finale = Math.Clamp(int.Parse(optionParts[1]), 1, int.MaxValue);
                                }
                            }

                            StringBuilder stringBuilder = new StringBuilder(40 + (prelude + finale) * Environment.NewLine.Length);

                            for (; prelude != 0; --prelude)
                            {
                                stringBuilder.Append(Environment.NewLine);
                            }

                            stringBuilder.Append("----------------------------------------");

                            for (; finale != 0; --finale)
                            {
                                stringBuilder.Append(Environment.NewLine);
                            }

                            // Return an expression containing the string we just built.
                            return Expression.Constant(stringBuilder.ToString(), typeof(string));
                        }
                    }
                    // File name:
                    case "%a":
                    {
                        return Expression.Property(messageParameter, nameof(Message.FileName));
                    }
                    // Function name:
                    case "%f":
                    {
                        return Expression.Field(messageParameter, nameof(Message.FunctionName));
                    }
                    // Line number:
                    case "%l":
                    {
                        MethodInfo intToStringMethod = typeof(int).GetMethod(nameof(int.ToString), new Type[] { });

                        return Expression.Call(Expression.Field(messageParameter, nameof(Message.LineNumber)), intToStringMethod);
                    }
                    // Logger name:
                    case "%n":
                    {
                        return Expression.Property(Expression.Field(messageParameter, nameof(Message.Origin)), nameof(Logger.Nombre));
                    }
                    // Line jump:
                    case "%b":
                    {
                        return Expression.Constant(Environment.NewLine, typeof(string));
                    }
                    case "%h":
                    {
                        if (string.IsNullOrWhiteSpace(options) || string.Equals(options, "n", StringComparison.OrdinalIgnoreCase))
                        {
                            return Expression.Property(Expression.Property(null, typeof(Thread), nameof(Thread.CurrentThread)), nameof(Thread.Name));
                        }
                        else if (string.Equals(options, "i", StringComparison.OrdinalIgnoreCase))
                        {
                            return Expression.Call(
                                Expression.Call(null, typeof(Thread).GetMethod(nameof(Thread.GetCurrentProcessorId))),
                                typeof(int).GetMethod(nameof(int.ToString), new Type[] { })
                            );
                        }

                        return Expression.Constant("");
                    }
                    // If the argument is not valid, we return an empty string.
                    default:
                    {
                        return Expression.Constant(string.Empty);
                    }
                }
            }
            // If an exception occurs, we don't crash the program.
            // We simply capture the exception and display it in the console.
            catch (Exception exception)
            {
                //TODO: Might want to log the exception through an specialized logger.
                Console.WriteLine(exception.Message);
            }

            return Expression.Constant(string.Empty);
        }

        #endregion
    }
}
