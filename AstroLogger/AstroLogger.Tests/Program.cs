using AstroLogger;

namespace AstroLogger.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Globals.Initialize<DefaultLoggerFactory>(ESeverity.ALL, "[%s]:", "GlobalLogger", "logs");

            Globals.InitializeConsoleLoggerProxy("Test - GlobalLogger", @"D:\Projects\DotNet\AstroLogger\AstroLogger\AstroLogger.Console\bin\Release\net8.0\AstroLogger.Console.exe", true, true, true);

            Globals.GlobalLogger.Log("Log 1", ESeverity.Info, EDestination.NONE);

            Globals.GlobalLogger.Log("Log 2", ESeverity.Info, EDestination.NONE);

            Globals.GlobalLogger.Log("Log 3", ESeverity.Info, EDestination.NONE);

            Globals.GlobalLogger.Log("Log 4", ESeverity.Info, EDestination.NONE);

            Globals.End();
        }
    }
}