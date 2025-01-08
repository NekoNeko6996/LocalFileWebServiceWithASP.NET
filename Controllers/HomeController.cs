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

namespace LocalFileWebService.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
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
        public ActionResult UploadFiles()
        {
            MVCDBContext db = new MVCDBContext();

            // check user
            HttpCookie cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null)
            {
                return Json(new { success = false, message = "user not alower" });
            }

            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie.Value);
            string userEmail = ticket.Name;
            User user = db.Users.Where(u => u.UserEmail.ToLower().Equals(userEmail.ToLower())).FirstOrDefault();

            if (user == null)
            {
                return Json(new { success = false, message = "user not found" });
            }

            var re = Request.Params;

            // get params
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

            // uploaded folder
            var lastFolderID = folderQuery.Last().FolderId;
            string userFolder = $"[{user.UserId}]/root/{folder_path_params}";
            string uploadPath = Path.Combine(Server.MapPath("~/UploadFiles"), userFolder);

            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var uploadedFiles = Request.Files;
            var allowedExtensions = new[] { ".jpg", ".png", ".pdf", ".docx", ".mp4" };

            List<string> errorMessages = new List<string>();
            List<string> successfullyUploadedFiles = new List<string>();     // Lưu trữ danh sách file đã upload thành công
            List<string> successfullyUploadedThumbnail = new List<string>(); // Lưu trữ danh sách thumbnail upload thành công (nếu có)

            for (int count = 0; count < uploadedFiles.Count; count++)
            {
                try
                {
                    HttpPostedFileBase file = uploadedFiles[count];
                    if (file != null)
                    {
                        string fileExtension = Path.GetExtension(file.FileName);

                        // Kiểm tra loại file hợp lệ
                        if (!allowedExtensions.Contains(fileExtension.ToLower()))
                        {
                            return Json(new { success = false, message = $"File type '{fileExtension}' is not allowed." });
                        }

                        string fileNameWithoutEx = Path.GetFileNameWithoutExtension(file.FileName);
                        string fileName = $"{fileNameWithoutEx}{fileExtension}";
                        string path = Path.Combine(uploadPath, fileName);
                        string mimeType = file.ContentType;

                        // Lưu file
                        file.SaveAs(path);
                        successfullyUploadedFiles.Add(path); // Thêm file vào danh sách đã upload

                        // get userId
                        int userId = db.Users.FirstOrDefault(x => x.UserEmail == userEmail).UserId;

                        string thumbnailPath = null;
                        if (mimeType.Contains("video"))
                        {
                            // tạo thumbnail cho video
                            thumbnailPath = Path.Combine(Server.MapPath($"~/UploadFiles/[{userId}]/thumbnail/"));
                            if (!Directory.Exists(thumbnailPath))
                            {
                                Directory.CreateDirectory(thumbnailPath);
                            }

                            string thumbnailName = $"{Guid.NewGuid().ToString()}.png";

                            // lấy thumbnail và save 
                            Media.GetThumbnail(path, thumbnailPath, thumbnailName);

                            thumbnailPath = Path.Combine(thumbnailPath, thumbnailName);
                            successfullyUploadedThumbnail.Add(thumbnailPath);
                        }

                        int mediaDuration = Media.GetMediaInfo(path).Duration;

                        Source fileInfo = new Source
                        {
                            UserId = userId,
                            SourceName = fileNameWithoutEx,
                            SourceType = string.IsNullOrEmpty(mimeType) ? "application/octet-stream" : mimeType,
                            ArtistId = 6,
                            UploadTime = DateTime.Now,
                            SourceUrl = path,
                            SourceThumbnailPath = string.IsNullOrEmpty(thumbnailPath) ? null : thumbnailPath,
                            SourceLength = mimeType.Contains("mp4") || mimeType.Contains("audio") ? mediaDuration : 0
                        };

                        // save new source
                        db.Sources.Add(fileInfo);

                        // link source to folder
                        FolderLink folderLink = new FolderLink
                        {
                            FolderId = lastFolderID,
                            SourceId = fileInfo.SourceId,
                            FolderLinkCreateAt = DateTime.Now
                        };

                        db.FolderLinks.Add(folderLink);

                        // save info
                        db.SaveChanges();
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    // Xử lý lỗi khi lưu vào cơ sở dữ liệu
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                        }
                    }

                    foreach (var error in ex.EntityValidationErrors)
                    {
                        errorMessages.Add(error.ValidationErrors.ToString());
                    }

                    // Nếu lỗi, xóa các file đã upload
                    foreach (var filePath in successfullyUploadedFiles)
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                     
                    // xóa thumbnail
                    foreach (var _thumbnailPath  in successfullyUploadedThumbnail)
                    {
                        if (System.IO.File.Exists(_thumbnailPath))
                        {
                            System.IO.File.Delete(_thumbnailPath);
                        }
                    }

                    return Json(new { success = false, message = string.Join(", ", errorMessages) });
                }
            }

            if (errorMessages.Count > 0)
            {
                // Xóa các file đã upload nếu có lỗi
                foreach (var filePath in successfullyUploadedFiles)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                return Json(new { success = false, message = string.Join(", ", errorMessages) });
            }

            return Json(new { success = true, message = "Files uploaded successfully." });
        }

    }
}