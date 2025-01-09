using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using LocalFileWebService.Models;
using Newtonsoft.Json;
using LocalFileWebService.Class;
using FFMpegCore;
using System.Web.WebPages;
using MediaInfo;
using MediaInfo.DotNetWrapper;
using System.Web.UI.WebControls;
using System.Data.Entity.Validation;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNet.SignalR;

namespace LocalFileWebService.Controllers
{
    [System.Web.Mvc.Authorize]
    public class HomeController : Controller
    {
        private int MAX_UPLOAD_FILE_IN_TIME = 4;

        // GET: Home
        public ActionResult Index()
        {
            // get params
            string folder_path_params = Request.Params["p"] ?? string.Empty;
            List<string> folder_paths = new List<string> { "root" };

            if (!string.IsNullOrEmpty(folder_path_params))
            {
                folder_paths.AddRange(folder_path_params.Split('/').ToList());
            }


            // get cookie
            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null)
            {
                ViewBag.ErrorMessage = "Authentication cookie is missing.";
                return View();
            }

            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
            if (ticket == null)
            {
                ViewBag.ErrorMessage = "Invalid authentication ticket.";
                return View();
            }

            string userEmail = ticket.Name;

            // query 
            MVCDBContext db = new MVCDBContext();
            var folderQuery = db.Folders
                .Where(folder => folder.User.UserEmail.Equals(userEmail) && folder_paths.Contains(folder.FolderName))
                .ToList();

            if (folderQuery.Count != folder_paths.Count)
            {
                ViewBag.ErrorMessage = "One or more folders in the path are missing.";
                return View();
            }

            var lastFolder = folderQuery.Last();
            int query_folder_id = lastFolder.FolderId;

            var foldersChildren = db.Folders
                .Where(folder => folder.FolderParentId == query_folder_id)
                .ToList();

            var sources = db.FolderLinks
                .Where(folderLink => folderLink.Folder.FolderId == query_folder_id)
                .Select(folderLink => folderLink.Source)
                .ToList();

            ViewBag.Folders = foldersChildren;
            ViewBag.Sources = sources;

            return View();
        }


        [HttpGet]
        public ActionResult GetVideoFileThumbnail(int id = -1)
        {
            if (id == -1)
            {
                return HttpNotFound("Invalid File ID");
            }
            MVCDBContext db = new MVCDBContext();
            Source source = db.Sources.Where(s => s.SourceId.Equals(id)).FirstOrDefault();
            if (source == null)
            {
                return HttpNotFound("File Not Found");
            }

            var filePath = source.SourceThumbnailPath;

            if (System.IO.File.Exists(filePath))
            {
                // Trả về file hình ảnh
                return File(System.IO.File.ReadAllBytes(filePath), "image/png"); //source.SourceType
            }
            return HttpNotFound("File Not Found");
        }

        public ActionResult GetFile(int id = -1)
        {
            if (id == -1)
            {
                return HttpNotFound("Invalid File ID");
            }
            MVCDBContext db = new MVCDBContext();
            Source source = db.Sources.Where(s => s.SourceId.Equals(id)).FirstOrDefault();
            if (source == null)
            {
                return HttpNotFound("File Not Found");
            }

            var filePath = source.SourceUrl;

            if (System.IO.File.Exists(filePath))
            {

                // Trả về file hình ảnh
                return File(System.IO.File.ReadAllBytes(filePath), source.SourceType); //source.SourceType
            }
            return HttpNotFound("File Not Found");
        }

        [HttpGet]
        public ActionResult GetAllFileOnFolder()
        {
            var filePath = Path.Combine("C:\\Users\\tranp\\Downloads\\【AMV】 Hololive × Tấm Lòng Son Remix.mp4"); // Đường dẫn tệp
            if (System.IO.File.Exists(filePath))
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath); // Đọc file
                var fileType = "video/mp4"; // Kiểu file, ví dụ: "image/jpeg" hoặc "video/mp4"
                return File(fileBytes, fileType); // Trả file
            }
            return HttpNotFound("File Not Found");
        }



        [HttpPost]
        public async Task<ActionResult> UploadFiles()
        {
            MVCDBContext db = new MVCDBContext();

            // Check user authentication
            HttpCookie cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null)
            {
                return Json(new { success = false, message = "User not allowed" });
            }

            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie.Value);
            string userEmail = ticket.Name;
            User user = db.Users.Where(u => u.UserEmail.ToLower().Equals(userEmail.ToLower())).FirstOrDefault();

            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            string folder_path_params = Request.Params["p"] ?? string.Empty;
            List<string> folder_paths = new List<string> { "root" };

            if (!string.IsNullOrEmpty(folder_path_params))
            {
                folder_paths.AddRange(folder_path_params.Split('/').ToList());
            }

            var folderQuery = db.Folders
                            .Where(folder => folder.User.UserEmail.Equals(userEmail) && folder_paths.Contains(folder.FolderName))
                            .ToList();

            if (folderQuery.Count != folder_paths.Count)
            {
                ViewBag.ErrorMessage = "One or more folders in the path are missing.";
                return View();
            }

            var lastFolderID = folderQuery.Last().FolderId;
            string userFolder = $"[{user.UserId}]/root/{folder_path_params}";
            string uploadPath = Path.Combine(Server.MapPath("~/UploadFiles"), userFolder);

            // Create directory if not exists
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var uploadedFiles = Request.Files;
            var allowedExtensions = new[] { ".jpg", ".png", ".pdf", ".docx", ".mp4" };
            List<string> errorMessages = new List<string>();

            //
            SemaphoreSlim semaphore = new SemaphoreSlim(MAX_UPLOAD_FILE_IN_TIME); // Giới hạn tối đa 4 luồng

            // Tạo danh sách các tác vụ xử lý file
            List<Task> uploadTasks = new List<Task>();

            for (int count = 0; count < uploadedFiles.Count; count++)
            {
                HttpPostedFileBase file = uploadedFiles[count];
                if (file != null)
                {
                    uploadTasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            await ProcessFile(file, allowedExtensions, uploadPath, user, db, lastFolderID, errorMessages);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
            }

            // Chờ tất cả các tác vụ hoàn thành
            await Task.WhenAll(uploadTasks);

            if (errorMessages.Count > 0)
            {
                return Json(new { success = false, message = string.Join(", ", errorMessages) });
            }

            return Json(new { success = true, message = "Files uploaded successfully." });
        }

        private async Task ProcessFile(HttpPostedFileBase file, string[] allowedExtensions, string uploadPath, User user, MVCDBContext db, int lastFolderID, List<string> errorMessages)
        {
            try
            {
                string fileExtension = Path.GetExtension(file.FileName);
                if (!allowedExtensions.Contains(fileExtension.ToLower()))
                {
                    errorMessages.Add($"File type '{fileExtension}' is not allowed.");
                    return;
                }

                string fileNameWithoutEx = Path.GetFileNameWithoutExtension(file.FileName);
                string fileName = $"{fileNameWithoutEx}{fileExtension}";
                string path = Path.Combine(uploadPath, fileName);
                string mimeType = file.ContentType;

                long totalBytes = file.ContentLength;
                long uploadedBytes = 0;

                // lấy tiến trình upload và lưu file
                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await file.InputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        uploadedBytes += bytesRead;

                        // Báo cáo tiến trình qua SignalR
                        int progress = (int)((double)uploadedBytes / totalBytes * 100);
                        //System.Diagnostics.Debug.WriteLine(progress);
                    }
                }

                string thumbnailPath = null;
                if (mimeType.Contains("video"))
                {
                    string thumbnailFolder = Path.Combine(Server.MapPath($"~/UploadFiles/[{user.UserId}]/thumbnail/"));
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

                db.Sources.Add(fileInfo);

                FolderLink folderLink = new FolderLink
                {
                    FolderId = lastFolderID,
                    SourceId = fileInfo.SourceId,
                    FolderLinkCreateAt = DateTime.Now
                };

                db.FolderLinks.Add(folderLink);
                await db.SaveChangesAsync();
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