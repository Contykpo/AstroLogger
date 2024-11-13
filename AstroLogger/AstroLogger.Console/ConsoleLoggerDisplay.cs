using AstroLogger;
using System;
using System.IO.Pipes;

namespace AstroLogger.Console
{
    /// <summary>
    /// Displays the logs of a <see cref="Logger"/> on console.
    /// </summary>
    public class ConsoleLoggerDisplay
    {
        /// <summary>
		/// Indicates whether the application should wait for the user to press a key before exiting.
		/// </summary>
		private static bool WaitUserInputBeforeClosing = false;

        /// <summary>
        /// Entry point of the console logger application.
        /// </summary>
        /// <param name="args">Arguments passed when starting the application</param>
        static void Main(string[] args)
        {
            // Check that we have been provided with enough arguments.
            if (args.Length == 3)
            {
                try
                {
                    Start(args);

                    if (WaitUserInputBeforeClosing)
                    {
                        DisplayMessage("Press any key to exit...", ConsoleColor.DarkGray);

                        System.Console.ReadKey();
                    }
                }
                catch (Exception exception)
                {
                    DisplayMessage(exception.Message, ConsoleColor.Red);
                    System.Console.ReadKey();
                }
            }
            else
            {
                DisplayMessage("Insufficient arguments.", ConsoleColor.Red);
                DisplayMessage("Press any key to exit...", ConsoleColor.Red);

                System.Console.ReadKey();
            }
        }

        /// <summary>
        /// Initializes logger to start reading the logs sent by the proxy.
        /// </summary>
        /// <param name="args">Argumentos</param>
        private static void Start(string[] args)
        {
            DisplayMessage("Initializing logger...", ConsoleColor.DarkGray);

            // Make sure we've been passed a bool as the second parameter.
            if (!bool.TryParse(args[1], out bool waitUserInputBeforeClosing))
            {
                throw new ArgumentException($"Cannot cast from {args[1]} to bool.");
            }

            DisplayMessage($"Title = {args[0]}", ConsoleColor.DarkGray);
            DisplayMessage($"Wait for user input before closing = {args[1]}", ConsoleColor.DarkGray);
            DisplayMessage($"Pipe handle = {args[2]}", ConsoleColor.DarkGray);

            // Update console title.
            System.Console.Title = args[0];

            WaitUserInputBeforeClosing = waitUserInputBeforeClosing;

            // Create the pipe and pass the server handle to it.
            using (AnonymousPipeClientStream pipe = new AnonymousPipeClientStream(PipeDirection.In, args[2]))
            {
                using (StreamReader pipeReader = new StreamReader(pipe))
                {
                    // Last line read from the stream.
                    string currentLine = string.Empty;

                    // Last message read from the stream.
                    string currentMessage = string.Empty;

                    // Severity of last read message.
                    ESeverity currentMessageSeverity = ESeverity.Info;

                    DisplayMessage("Listening...", ConsoleColor.DarkGray);

                    // Wait for the broadcast to begin.
                    do
                    {
                        currentLine = pipeReader.ReadLine() ?? string.Empty;

                    } while (!currentLine.StartsWith(Constants.Constants.BroadcastBeginFlag) && !currentLine.StartsWith(Constants.Constants.BroadcastEndFlag));

                    DisplayMessage("Begin broadcast.", ConsoleColor.Green);

                    // Loop while we do not find the end of transmission flag.
                    do
                    {
                        // Loop while we do not find the end of message flag.
                        do
                        {
                            currentLine = pipeReader.ReadLine() ?? string.Empty;

                            // If the current flag is StartMessageFlag, assign the current message to the next line.
                            if (currentLine.StartsWith(Constants.Constants.MessageFlag))
                            {
                                currentMessage = pipeReader.ReadLine();
                            }
                            // If the current flag is StartSeverityFlag, assign the severity of the current message to the next line.
                            else if (currentLine.StartsWith(Constants.Constants.SeverityFlag))
                            {
                                currentMessageSeverity = Enum.Parse<ESeverity>(pipeReader.ReadLine());
                            }

                        } while (!currentLine.StartsWith(Constants.Constants.EndMessageFlag) && !currentLine.StartsWith(Constants.Constants.BroadcastEndFlag));

                        // Display received message.
                        if (!string.IsNullOrWhiteSpace(currentMessage))
                        {
                            DisplayMessage(currentMessage, GetConsoleFontColor(currentMessageSeverity));
                        }

                        currentMessage = string.Empty;

                    } while (!currentLine.StartsWith(Constants.Constants.BroadcastEndFlag));
                }
            }

            // Report that the broadcast has come to an end.
            DisplayMessage("Broadcast ended.", ConsoleColor.DarkGray);
        }

        /// <summary>
        /// Associates a <see cref="ESeveridad"/> value to a <see cref="ConsoleColor"/>
        /// </summary>
        /// <param name="severity">Severity for which to obtain the color</param>
        /// <returns><see cref="ConsoleColor"/> corresponding to the given <paramref name="severity"/></returns>
        private static ConsoleColor GetConsoleFontColor(ESeverity severity)
        {
            switch (severity)
            {
                case ESeverity.Debug:
                    return ConsoleColor.DarkGray;
                case ESeverity.Info:
                    return ConsoleColor.Green;
                case ESeverity.Warning:
                    return ConsoleColor.Yellow;
                case ESeverity.Error:
                    return ConsoleColor.Red;
                case ESeverity.CriticalError:
                    return ConsoleColor.DarkRed;
                default:
                    return ConsoleColor.White;
            }
        }

        /// <summary>
        /// Displays a <paramref name="message"/> on the console with a specific <paramref name="color"/> 
        /// </summary>
        /// <param name="message">Message to be displayed</param>
        /// <param name="color">Color given to the message text</param>
        private static void DisplayMessage(string message, ConsoleColor color)
        {
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(message);
        }
    }
}
