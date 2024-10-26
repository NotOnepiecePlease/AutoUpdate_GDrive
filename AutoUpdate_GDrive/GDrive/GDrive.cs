using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;

namespace AutoUpdate_GDrive.GDrive
{
    /// <summary>
    /// Class that manages all uploads, downloads, and file verifications on Google Drive
    /// </summary>
    public class GDrive
    {
        #region Properties

        // Name of your Google API project
        public const string APP_NAME = "";

        // Your API credentials key
        public const string APIKEY_GOOGLEDRIVE = "";

        // Updated FILE ID from Google Drive Folder
        public const string FILE_UPDATED_ID = "";

        // Version TEXT FILE ID from Google Drive Folder
        public const string TEXT_VERSION_UPDATED_ID = "";

        // Updated FILE FOLDER ID from Google Drive Folder
        public const string FILE_FOLDER_UPDATED_ID = "";

        public DriveService Service;

        #endregion

        public GDrive()
        {
            Service = GetGDriveConnection_ApiKey();
        }

        #region Drive Connection
        /// <summary>
        /// Connection made using the Google Drive API Key
        /// </summary>
        /// <returns></returns>
        private DriveService GetGDriveConnection_ApiKey()
        {
            return new DriveService(new BaseClientService.Initializer()
            {
                ApiKey = APIKEY_GOOGLEDRIVE,
                ApplicationName = APP_NAME,
            });
        }

        #endregion

        #region Manage System AutoUpdate

        /// <summary>
        /// Method to retrieve the version of the updated system from Google Drive
        /// </summary>
        /// <returns></returns>
        public async Task<string> SearchVersionUpdated()
        {
            try
            {
                // ID of the .txt file you want to read.
                string fileId = TEXT_VERSION_UPDATED_ID;

                // Read the content of the .txt file.
                var fileRequest = Service.Files.Get(fileId);

                // Create a stream to save the file to the selected directory.
                using (var fileStream = new MemoryStream())
                {
                    fileRequest.MediaDownloader.ProgressChanged += (IDownloadProgress downloadProgress) =>
                    {
                        switch (downloadProgress.Status)
                        {
                            case DownloadStatus.Downloading:
                                {
                                    // Update the download progress.
                                    break;
                                }
                            case DownloadStatus.Completed:
                                {
                                    // Preparation to update the system.
                                    break;
                                }
                            case DownloadStatus.Failed:
                                {
                                    // Handle download failure.
                                    MessageBox.Show($"Version file could not be found, contact an administrator!\n\n{downloadProgress.Exception?.Message}");
                                    break;
                                }
                        }
                    };

                    // Start the download.
                    await fileRequest.DownloadAsync(fileStream);
                    fileStream.Seek(0, SeekOrigin.Begin);
                    StreamReader reader = new StreamReader(fileStream);
                    string onlineVersion = reader.ReadToEnd();

                    return onlineVersion;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("VERSION ERROR: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Method to retrieve the files in the updated folder for the update.
        /// This method is mainly used to get the file size in SBAutoUpdate.
        /// </summary>
        /// <returns></returns>
        public FileList GetUpdatedFileList()
        {
            string gDriveFolderId = FILE_FOLDER_UPDATED_ID;
            var fileListRequest = Service.Files.List();
            fileListRequest.Q = $"'{gDriveFolderId}' in parents";
            fileListRequest.Fields = "*";

            var preparedFileList = fileListRequest.Execute();
            return preparedFileList;
        }

        /// <summary>
        /// Method to retrieve the updated main system file from Google Drive
        /// </summary>
        /// <returns></returns>
        public FilesResource.GetRequest GetFileUpdated()
        {
            string gDriveFileId = FILE_UPDATED_ID;
            var gDriveFileRequest = Service.Files.Get(gDriveFileId);

            return gDriveFileRequest;
        }

        #endregion
    }
}
