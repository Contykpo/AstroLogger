namespace AstroLogger
{
    /// <summary>
	/// Class that allows multiple <see cref="Logger"/>s to synchronously write on the same file.
	/// </summary>
	public class FileAccessController : IDisposable
    {
        #region Fields and Properties

        /// <summary>
        /// File where <see cref="Logger"/>s will be writing.
        /// </summary>
        private StreamWriter mFile;

        /// <summary>
        /// List of weak references to the loggers using this file.
        /// We keep a weak reference so that memory gets released when an instantiated logger is no longer used.
        /// </summary>
        private List<WeakReference<Logger>> mLinkedLoggers = new List<WeakReference<Logger>>();

        /// <summary>
        /// File access synchronization lock.
        /// </summary>
        private object mFileLock = new object();

        /// <summary>
        /// Full path to the file.
        /// </summary>
        private string mFullFilePath;

        /// <summary>
        /// File prefix shared between multiple files.
        /// </summary>
        private string mFileNamePrefix;


        // --- Properties ---


        /// <summary>
        /// List of <see cref="WeakReference"/> to the <see cref="FileLogger"/>s using this file.
        /// </summary>
        public List<WeakReference<Logger>> AssociatedLoggers => mLinkedLoggers;

        /// <summary>
        /// Full path to the file.
        /// </summary>
        public string FullFilePath => mFullFilePath;

        /// <summary>
        /// File name.
        /// </summary>
        public string FileName => Path.GetFileName(mFullFilePath);

        /// <summary>
        /// File prefix need to identify from which logger a certain log comes from.
        /// </summary>
        public string FileNamePrefix => mFileNamePrefix;

        #endregion

        #region Constructor and Destructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fullFilePath">Absolute path to the file where logs will be written.</param>
        /// <param name="filePrefix">File prefix</param>
        public FileAccessController(string fullFilePath, string filePrefix)
        {
            mFileNamePrefix = filePrefix;

            try
            {
                // We open the file.
                mFile = new StreamWriter(File.Open(fullFilePath, FileMode.OpenOrCreate, FileAccess.Write));

                // We allow the buffer's contents to be automatically poured inside the file after performing a write operation.
                mFile.AutoFlush = true;

                mFullFilePath = fullFilePath;
            }
            catch (Exception ex)
            {
                string.Format($"An error occurred while opening the file {Path.GetFileName(fullFilePath)}: {ex.Message}").AttemptLog(ESeverity.Error);
            }
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~FileAccessController()
        {
            if (mFile != null)
            {
                mFile.Close();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Links a logger to this controller.
        /// </summary>
        /// <param name="logger">Logger to be linked</param>
        public void LinkLogger(Logger logger)
        {
            mLinkedLoggers.Add(new WeakReference<Logger>(logger));

            // We remove the excess memory occupied by the list.
            mLinkedLoggers.TrimExcess();
        }

        /// <summary>
        /// Removes the link between a logger and this controller.
        /// If the amount of linked loggers reaches 0, the file will be closed.
        /// </summary>
        /// <param name="logger">Logger to be linked</param>
        public void UnlinkLogger(Logger logger)
        {
            mLinkedLoggers.RemoveAll(item =>
            {
                item.TryGetTarget(out Logger currentLogger);

                // If the referenced logger is null, we remove it to avoid keeping a reference to a null object.
                return currentLogger == null || currentLogger == logger;
            });

            // We dispose of this instance if there are no more linked loggers.
            if (mLinkedLoggers.Count <= 0)
            {
                Dispose();
            }
            // If there are still some loggers, we only remove the excess memory occupied by the list.
            else
            {
                mLinkedLoggers.TrimExcess();
            }
        }

        /// <summary>
        /// Removes all the linked loggers from this instance. 
        /// This instance becomes unusable after calling this method.
        /// </summary>
        public void UnlinkAllLoggers()
        {
            foreach (var logger in mLinkedLoggers)
            {
                mLinkedLoggers.Remove(logger);
            }
        }

        /// <summary>
        /// Synchronously writes on file, that means that the caller thread will have to wait in the locks.
        /// </summary>
        /// <param name="contentString"></param>
        /// <param name="timeout"></param>
        public void Write(string contentString, int timeout = 1000)
        {
            // Return if the file is null or the contentString is empty.
            if (mFile == null || string.IsNullOrWhiteSpace(contentString))
                return;

            // There is no need to synchronize the access if there is a single logger.
            if (mLinkedLoggers.Count == 1)
            {
                WriteWithoutSyncronization(contentString);
            }
            else
            {
                bool retrievedLock = false;

                try
                {
                    // We attempt to get the lock.
                    Monitor.TryEnter(mFileLock, timeout, ref retrievedLock);

                    // If the lock is successfully retrieved, we write the string content in the file.
                    if (retrievedLock)
                    {
                        mFile.WriteLine(contentString);
                    }
                }
                finally
                {
                    // We free the lock if it was retrieved.
                    if (retrievedLock)
                    {
                        Monitor.PulseAll(mFileLock);
                        Monitor.Exit(mFileLock);
                    }
                }
            }
        }

        public async Task WriteAsynchronously(string cadena, int timeout = Int32.MaxValue)
        {
            await Task.Run(() => Write(cadena, timeout));
        }

        /// <summary>
        /// Asynchronously moves the file to a new path.
        /// </summary>
        /// <param name="newPath">The new path where the file will be moved, the path must be absolute.</param>
        /// <param name="deletePreviousFile">Whether the previous file in the old path must be deleted or not.</param>
        /// <returns></returns>
        public async Task<bool> MoveFileAsynchronously(string newPath, bool deletePreviousFile = true)
        {
            return await Task.Run(() => MoveFile(newPath, deletePreviousFile));
        }

        /// <summary>
        /// Moves the file to a new path.
        /// </summary>
        /// <param name="newPath">The new path where the file will be moved, the path must be absolute.</param>
        /// <param name="deletePreviousFile">Whether the current file in the old path must be deleted or not.</param>
        /// <returns></returns>
        public bool MoveFile(string newPath, bool deletePreviousFile = true)
        {
            // Verify that the path is absolute.
            if (!Path.IsPathFullyQualified(newPath))
                return false;

            bool retrievedLock = false;

            try
            {
                Monitor.TryEnter(mFileLock, Int32.MaxValue, ref retrievedLock);

                if (retrievedLock)
                {
                    mFile.Close();

                    // Move the file to the new path.
                    File.Move(mFullFilePath, newPath, true);

                    // Delete the previous file in the old path if true.
                    if (deletePreviousFile)
                    {
                        File.Delete(mFullFilePath);
                    }

                    // Remove the key linked to old path.
                    FileLogger.CurrentlyOpenedFiles.Remove(mFullFilePath);

                    mFullFilePath = newPath;

                    mFile = new StreamWriter(File.Open(mFullFilePath, FileMode.OpenOrCreate, FileAccess.Write));

                    // Create a new dictionary entry with the new path.
                    FileLogger.CurrentlyOpenedFiles.Add(mFullFilePath, this);

                    return true;
                }
            }
            catch (Exception ex)
            {
                string.Format($"An error ocurred while moving file {FileName} to {newPath}: {ex.Message}").AttemptLog(ESeverity.Error);
            }
            finally
            {
                if (retrievedLock)
                {
                    Monitor.Pulse(mFileLock);
                    Monitor.Exit(mFileLock);
                }
            }

            return false;
        }

        /// <summary>
        /// Write content directly in the file without performing any syncronization.
        /// </summary>
        /// <param name="contentString">The content that will be written in the file</param>
        private void WriteWithoutSyncronization(string contentString)
        {
            mFile.WriteLine(contentString);
        }

        #endregion

        #region IDispose Implementation

        /// <summary>
        /// Closes the file and removes the entry related to its path inside the dictionary.
        /// </summary>
        public void Dispose()
        {
            if (mFile != null)
            {
                mFile.Close();

                mFile = null!;

                // Remove the entry related to the file.
                FileLogger.CurrentlyOpenedFiles.Remove(mFullFilePath);
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
