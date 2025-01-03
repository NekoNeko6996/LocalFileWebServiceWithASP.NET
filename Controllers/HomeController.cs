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

namespace LocalFileWebService.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            string folder_path_params = Request.Params["p"];
            string folderName = "root";


            MVCDBContext db = new MVCDBContext();

            // Lấy và giải mã cookie
            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
            string userEmail = ticket.Name;

            // Tìm thư mục root của user
            var query_find_folder = db.Folders
                .FirstOrDefault(folder => folder.User.UserEmail == userEmail && folder.FolderName == folderName);

            if (query_find_folder == null)
            {
                // Thông báo người dùng nếu không tìm thấy thư mục root
                ViewBag.ErrorMessage = "Không tìm thấy thư mục gốc.";
                return View();
            }

            int parentFolderId = query_find_folder.FolderId;

            // Tìm thư mục con
            var foldersChildren = db.Folders
                .Where(folder => folder.FolderParentId == parentFolderId)
                .ToList();

            // Tìm file con
            var sources = db.FolderLinks
                .Where(folderLink => folderLink.Folder.FolderId== parentFolderId)
                .Select(folderLink => folderLink.Source)
                .ToList();

            ViewBag.Folders = foldersChildren;
            ViewBag.Sources = sources;

            return View();
        }


        [HttpGet]
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
            var filePath = Path.Combine(source.SourceUrl.Trim('\"')); // Đường dẫn tệp
            if (System.IO.File.Exists(filePath))
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath); // Đọc file
                var fileType = source.SourceType; // Kiểu file, ví dụ: "image/jpeg" hoặc "video/mp4"
                return File(fileBytes, fileType); // Trả file
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