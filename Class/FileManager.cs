using LocalFileWebService.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace LocalFileWebService.Class
{
    public class SaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class FileManager
    {
        private readonly MVCDBContext _dbContext;
        private List<FormUploadFilesInfo> _filesInfo;
        private List<HttpPostedFileBase> _files;
        private string _path;
        private string _absolutePath;
        private User _user;

        public FileManager(
            List<FormUploadFilesInfo> filesInfo,
            List<HttpPostedFileBase> files,
            string path,
            string absolutePath,
            User user,
            MVCDBContext dbContext
        )
        {
            _filesInfo = filesInfo;
            _files = files;
            _path = path;
            _absolutePath = absolutePath;
            _user = user;
            _dbContext = dbContext;
        }


        public SaveResult save()
        {
            // map đường đẫn để lưu
            List<string> folder_paths = new List<string> { "root" };

            if (!string.IsNullOrEmpty(_path))
            {
                folder_paths.AddRange(_path.Split('/').ToList());
            }

            // xác định folder đích
            var folderQuery = _dbContext.Folders
                            .Where(folder => folder.User.UserId.Equals(_user.UserId) && folder_paths.Contains(folder.FolderName))
                            .ToList();

            if (folderQuery.Count != folder_paths.Count)
            {
                return new SaveResult { 
                    Success = false, 
                    Message = "One or more folders in the path are missing." 
                };
            }

            var lastFolderID = folderQuery.Last().FolderId;
            string userFolder = $"[{_user.UserId}]/root/{_path}";
            string uploadPath = Path.Combine(_absolutePath, userFolder);

            // Create directory if not exists
            try
            {
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }
            }
            catch (Exception ex)
            {
                return new SaveResult
                { 
                    Success = false, 
                    Message = $"Error creating upload directory: {ex.Message}" 
                };
            }

            //
            var allowedExtensions = new[] { ".jpg", ".png", ".pdf", ".docx", ".mp4" };
            List<string> errorMessages = new List<string>();

            // kiểm tra số lượng thông tin và số lượng file
            if (_files.Count != _filesInfo.Count)
            {
                return new SaveResult
                { 
                    Success = false, 
                    Message = "Files and file info count mismatch." 
                };
            }

            string thumbnailFolder = Path.Combine(_absolutePath, $"/[{_user.UserId}]/thumbnail/");

            // gọi func để lưu
            for (int count = 0; count < _files.Count; count++)
            {
                HttpPostedFileBase file = _files[count];
                if (file != null)
                {
                    try
                    {
                        ProcessFile(file, allowedExtensions, uploadPath, _user, lastFolderID, errorMessages, _filesInfo[count], thumbnailFolder);
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add($"Unexpected error processing file '{file.FileName}': {ex.Message}");
                    }
                }
            }

            if(errorMessages.Count > 0)
            {
                return new SaveResult
                {
                    Success = false,
                    Message = string.Join(";", errorMessages)
                };
            }

            return new SaveResult
            {
                Success = true,
                Message = "Save file successfull"
            };
        }


        private void ProcessFile(HttpPostedFileBase file, string[] allowedExtensions, string uploadPath, User user, int lastFolderID, List<string> errorMessages, FormUploadFilesInfo _fileInfo, string thumbnailFolder)
        {
            try
            {
                string fileExtension = Path.GetExtension(file.FileName);
                string fileNameWithoutEx = _fileInfo.FileName ?? Path.GetFileNameWithoutExtension(file.FileName);
                string fileName = $"{fileNameWithoutEx}{fileExtension}";
                string path = Path.Combine(uploadPath, fileName);
                string mimeType = file.ContentType;

                if (!allowedExtensions.Contains(fileExtension.ToLower()) || !mimeType.StartsWith("image/") && !mimeType.StartsWith("video/"))
                {
                    errorMessages.Add($"File type '{fileExtension}' is not allowed.");
                    return;
                }

                // Save file
                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    file.InputStream.CopyTo(fileStream);
                }

                string thumbnailPath = null;
                if (mimeType.Contains("video"))
                {
                    if (!Directory.Exists(thumbnailFolder))
                    {
                        Directory.CreateDirectory(thumbnailFolder);
                    }

                    string thumbnailName = $"{Guid.NewGuid()}.png";
                    Media.GetThumbnail(path, thumbnailFolder, thumbnailName);
                    thumbnailPath = Path.Combine(thumbnailFolder, thumbnailName);
                }

                int mediaDuration = Media.GetMediaInfo(path).Duration;

                Source fileInfo = new Source
                {
                    UserId = user.UserId,
                    SourceName = fileNameWithoutEx,
                    SourceType = string.IsNullOrEmpty(mimeType) ? "application/octet-stream" : mimeType,
                    ArtistId = 6,
                    UploadTime = DateTime.Now,
                    SourceUrl = path,
                    SourceThumbnailPath = string.IsNullOrEmpty(thumbnailPath) ? null : thumbnailPath,
                    SourceLength = mimeType.Contains("mp4") || mimeType.Contains("audio") ? mediaDuration : 0
                };

                if (_fileInfo.Tags != null)
                {
                    fileInfo.SourceTag = _fileInfo.Tags != null ? string.Join(",", _fileInfo.Tags) : null;
                }

                _dbContext.Sources.Add(fileInfo);

                FolderLink folderLink = new FolderLink
                {
                    FolderId = lastFolderID,
                    SourceId = fileInfo.SourceId,
                    FolderLinkCreateAt = DateTime.Now
                };

                _dbContext.FolderLinks.Add(folderLink);
                _dbContext.SaveChanges();
            }
            catch (IOException ioEx)
            {
                errorMessages.Add($"I/O error processing file '{file.FileName}': {ioEx.Message}");
            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.Add($"DB validation error: Property: {validationError.PropertyName}, Error: {validationError.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessages.Add($"Unexpected error processing file '{file.FileName}': {ex.Message}");
            }
        }
    }
}