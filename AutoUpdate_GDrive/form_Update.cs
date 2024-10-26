using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Download;

namespace AutoUpdate_GDrive
{
    public partial class form_Update : Form
    {
        private const string ACTUAL_SOFTWARE_VERSION = "1.0.0";
        private static string FILE_UPDATED_NAME = "UpdateNEW.zip";

        public form_Update()
        {
            InitializeComponent();
        }

        private enum UpdateStatus
        {
            Updated,
            UpdatePending,
            Error
        }

        /// <summary>
        /// Checks the version of the system to determine if an update is required.
        /// </summary>
        /// <returns>An <see cref="UpdateStatus"/> indicating whether the system is up to date, needs an update, or encountered an error.</returns>
        private static async Task<UpdateStatus> CheckStatusVersion()
        {
            try
            {
                GDrive.GDrive GDrive = new GDrive.GDrive();
                string onlineVersion = await GDrive.SearchVersionUpdated();

                if (!string.IsNullOrWhiteSpace(onlineVersion))
                {
                    if (onlineVersion.Replace(" ", "").Trim().Equals(ACTUAL_SOFTWARE_VERSION))
                    {
                        MessageBox.Show("The system is already up to date!");
                        return UpdateStatus.Updated;
                    }
                    MessageBox.Show("There is a new version available!");
                    return UpdateStatus.UpdatePending;
                }

                MessageBox.Show("Version file cannot be found, call an administrator!",
                                @"Version download failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return UpdateStatus.Error;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to check system version, don't worry, everything will work normally.\n\n\nError: " + ex,
                                @"Verification Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return UpdateStatus.Error;
            }
        }

        /// <summary>
        /// Retrieves the total size of the update from Google Drive.
        /// </summary>
        /// <returns>The size of the update file, or 0 if not found.</returns>
        private static long? GetUpdateSize()
        {
            GDrive.GDrive GDrive = new GDrive.GDrive();
            long? totalFileSize = 0;

            // Workaround to get the update size
            var updatedFileList = GDrive.GetUpdatedFileList();
            foreach (var file in updatedFileList.Files)
            {
                if (file.Name == FILE_UPDATED_NAME)
                {
                    totalFileSize = file.Size;
                    return totalFileSize;
                }
            }
            return totalFileSize;
        }

        /// <summary>
        /// Processes the update by downloading the latest version from Google Drive.
        /// </summary>
        /// <param name="_progressBar">The progress bar to update with the download progress.</param>
        /// <param name="_cancellationToken">A cancellation token to manage the download cancellation.</param>
        public static async Task ProcessUpdate(ProgressBar _progressBar, CancellationToken _cancellationToken)
        {
            try
            {
                var updateStatus = await CheckStatusVersion();
                if (updateStatus == UpdateStatus.UpdatePending)
                {
                    GDrive.GDrive GDrive = new GDrive.GDrive();
                    string rootPath = AppDomain.CurrentDomain.BaseDirectory;

                    Console.WriteLine("Searching for updated file...");
                    var updatedFile = GDrive.GetFileUpdated();

                    long? totalUpdatedFileSize = GetUpdateSize();
                    long bytesAlreadyDownloaded = 0;

                    // Initialize the progress bar
                    _progressBar.Invoke(new Action(() =>
                    {
                        _progressBar.Value = 0;
                    }));

                    // Create a stream to save the file to the selected directory.
                    using (var fileStream = new FileStream(Path.Combine(rootPath, FILE_UPDATED_NAME), FileMode.Create, FileAccess.Write))
                    {
                        updatedFile.MediaDownloader.ProgressChanged += (IDownloadProgress downloadProgress) =>
                        {
                            switch (downloadProgress.Status)
                            {
                                case DownloadStatus.Downloading:
                                    {
                                        bytesAlreadyDownloaded = downloadProgress.BytesDownloaded;
                                        Console.WriteLine($"Bytes downloaded: {bytesAlreadyDownloaded}...");
                                        double progressInPercent = ((double)bytesAlreadyDownloaded / Convert.ToDouble(totalUpdatedFileSize)) * 100;

                                        _progressBar.Invoke(new Action(() =>
                                        {
                                            _progressBar.Value = (int)progressInPercent / 2;
                                        }));
                                        break;
                                    }
                                case DownloadStatus.Completed:
                                    {
                                        _progressBar.Invoke(new Action(() =>
                                        {
                                            _progressBar.Value = 100;
                                        }));
                                        MessageBox.Show("Download complete!");
                                        break;
                                    }
                                case DownloadStatus.Failed:
                                    {
                                        Console.WriteLine("Download failed!");
                                        break;
                                    }
                            }
                        };

                        try
                        {
                            Console.WriteLine("Downloading update files...");
                            await updatedFile.DownloadAsync(fileStream, _cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            MessageBox.Show("The download was interrupted. This may be due to a problem with your internet connection. " +
                                            "Please try again later. If the problem persists, " +
                                            "contact an administrator for assistance.");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred during the update: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error downloading update file: " + ex.Message);
            }
        }

        /// <summary>
        /// Event handler for the update button click. Initiates the update process.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        private async void btnUpdateProcess_Click(object sender, EventArgs e)
        {
            await ProcessUpdate(progressUpdate, new CancellationToken());
        }
    }
}
