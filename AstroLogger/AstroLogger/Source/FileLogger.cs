using System.Text.RegularExpressions;

namespace AstroLogger
{
    /// <summary>
    /// Interface that contains the methods and properties for a <see cref="Logger"/> that saves its logs inside a file.
    /// </summary>
    public abstract class FileLogger : Logger
    {
        #region Fields and Properties

        private static Dictionary<string, FileAccessController> mCurrentlyOpenedFiles = new Dictionary<string, FileAccessController>();

        protected FileAccessController mFileAccessController;

        /// <summary>
        /// Diccionario que relaciona rutas con un <see cref="FileAccessController"/>
        /// </summary>
        public static Dictionary<string, FileAccessController> CurrentlyOpenedFiles => mCurrentlyOpenedFiles;

        #endregion

        #region Constructor

        public FileLogger(LoggerFactory factory, string name, string fileName) : base(factory, name)
        {
            // Verify file name is not empty and does not include forbidden characters.
            if (string.IsNullOrWhiteSpace(fileName) || Regex.IsMatch(fileName, @"[/\:?*<>|]")) fileName = "Log";

            try
            {
                // Get the path to the file where we will be logging.
                string pathArchivoLogs = Path.GetFullPath(Path.Combine(Factory.LogsPath, $"{fileName} {DateTime.Now.ToString("dd-mm (hh-mm-ss)")}.log"));

                if (mCurrentlyOpenedFiles.ContainsKey(pathArchivoLogs))
                {
                    mCurrentlyOpenedFiles[pathArchivoLogs].LinkLogger(this);
                }
                else
                {
                    mCurrentlyOpenedFiles.Add(pathArchivoLogs, new FileAccessController(pathArchivoLogs, fileName));
                }

                mFileAccessController = mCurrentlyOpenedFiles[pathArchivoLogs];

                ClearLogSurplus(ELogType.General);
            }
            catch (Exception exception)
            {
                string.Format($"En error occurred while opening log file: {exception.Message}").AttemptLog(ESeverity.Error);
            }
        }

        #endregion

        #region Metodos

        public override void End()
        {
            base.End();

            if (mFileAccessController == null) return;

            mFileAccessController.UnlinkLogger(this);

            mFileAccessController = null!;
        }

        /// <summary>
        /// Deletes de log surplus so that the amount left is equal to the maximum specified by the <see cref="LoggerFactory"/> instance minus one.
        /// </summary>
        /// <param name="logType">The type of the logs we want to delete.</param>
        protected virtual void ClearLogSurplus(ELogType logType)
        {
            if (logType == ELogType.All)
            {
                ClearLogSurplus(ELogType.General);
                ClearLogSurplus(ELogType.Crash);

                return;
            }
            else if (logType == ELogType.NONE) return;

            // Retrieve existing logs or crashes.
            List<string> existingLogFiles =
                logType == ELogType.General ?
                new List<string>(Directory.GetFiles(Factory.LogsPath, $"*{mFileAccessController.FileNamePrefix}*.*"))
                : new List<string>(Directory.GetFiles(Factory.CrashesPath, "*Crash*.*"));

            // Get the maximum amount of logs or crashes for the given log type.
            int maximumAmountOfFiles =
                logType == ELogType.General
                ? Factory.MaximumAmountOfLogs
                : Factory.MaximumAmountOfCrashes;

            // Loop while the amount of files is greater or equal than the maximum amount of files.
            while (existingLogFiles.Count >= maximumAmountOfFiles)
            {
                int oldestFileIndex = 0;
                
                DateTime mostRecentDate = File.GetLastWriteTime(existingLogFiles[0]);

                // Search for the oldest files.
                for (int i = 1; i < existingLogFiles.Count; ++i)
                {
                    DateTime currentFileDate = File.GetLastWriteTime(existingLogFiles[i]);

                    // If the last modification date is even more recent than the one we label as the most recent date:
                    if (currentFileDate < mostRecentDate)
                    {
                        mostRecentDate = currentFileDate;

                        // We save the position index where the file path is located.
                        oldestFileIndex = i;
                    }
                }

                // We delete the file.
                File.Delete(existingLogFiles[oldestFileIndex]);

                // Remove the file path from the existing log files list.
                existingLogFiles.RemoveAt(oldestFileIndex);
            }
        }

        #endregion
    }
}
