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

            var filePath = Server.MapPath($"~/Content/Thumbnail/{source.SourceThumbnailName}");

            if (System.IO.File.Exists(filePath))
            {
                string thumbnailPath = Path.Combine(Server.MapPath("~/Content/Thumbnail"), $"{Guid.NewGuid()}.jpg");

                // Tạo ảnh snapshot từ video
                //FFMpeg.Snapshot(filePath, thumbnailPath, null, TimeSpan.FromSeconds(1));

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
    }
}