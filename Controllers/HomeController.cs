using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using LocalFileWebService.Models;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using LocalFileWebService.Class;
using Microsoft.Owin.Security;

namespace LocalFileWebService.Controllers
{
    [System.Web.Mvc.Authorize]
    public class HomeController : Controller
    {
        private readonly string DefaultArtistAvatarUrls = "/Content/Resources/Default/Img/_z5862484494155_3d8ec334ae9dfa5829f4e47d070d8dfd.jpg";

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

        public ActionResult Artists()
        {
            MVCDBContext db = new MVCDBContext();
            List<Artist> artists = db.Artists.ToList();

            ViewBag.DefaultArtistAvatarUrls = DefaultArtistAvatarUrls;
            ViewBag.Artists = artists;
            return View();
        }

        public ActionResult Artist(int id = -1)
        {


            if (id == -1)
            {
                ViewBag.Status = "error";
                ViewBag.Message = "Invalid Artist Id";
                return View();
            }

            MVCDBContext db = new MVCDBContext();
            Artist artists = db.Artists.FirstOrDefault(a => a.ArtistId.Equals(id));
            List<Source> sources = db.Sources.Where(s => s.Artist.ArtistId.Equals(id)).ToList();

            if (artists == null)
            {
                ViewBag.Status = "error";
                ViewBag.Message = "Artists Not Found";
                return View();
            }

            ViewBag.DefaultArtistAvatarUrls = DefaultArtistAvatarUrls;
            ViewBag.Artists = artists;
            ViewBag.Sources = sources;
            return View();
        }

        public ActionResult UploadFiles()
        {
            HttpCookie cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie.Value);

            string userEmail = ticket.Name;

            MVCDBContext db = new MVCDBContext();
            List<Artist> artists = db.Artists.ToList() ?? new List<Artist>();
            List<Tag> tags = db.Tags.ToList() ?? new List<Tag>();
            List<Folder> folders = db.Folders.Where(f => f.User.UserEmail.Equals(userEmail)).ToList() ?? new List<Folder>();
            Folder folder = db.Folders.FirstOrDefault(f => f.FolderName.Equals("root") && f.User.UserEmail.Equals(userEmail));

            ViewBag.RootFolderId = folder.FolderId;
            ViewBag.RootFolder = folder;
            ViewBag.Folders = folders;
            ViewBag.DefaultArtist = 6;
            

            ViewBag.Tags = tags;
            ViewBag.Artists = artists;
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
        public ActionResult _UploadFiles()
        {
            string uploadTo = Request.Form["path"] ?? string.Empty;

            // Convert HttpFileCollectionBase to List<HttpPostedFileBase>
            var uploadedFiles = new List<HttpPostedFileBase>();
            for (int i = 0; i < Request.Files.Count; i++)
            {
                uploadedFiles.Add(Request.Files[i]);
            }

            string filesInfoJson = Request.Form["filesInfo"];

            if (uploadTo.Contains(".."))
            {
                return Json(new { success = false, message = "Invalid upload path." });
            }

            HttpCookie cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null)
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            FormsAuthenticationTicket ticket;
            try
            {
                ticket = FormsAuthentication.Decrypt(cookie.Value);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Authentication error: {ex.Message}" });
            }

            string userEmail = ticket.Name;
            using (var db = new MVCDBContext())
            {
                var user = db.Users.FirstOrDefault(u => u.UserEmail.ToLower() == userEmail.ToLower());
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                if (string.IsNullOrEmpty(filesInfoJson))
                {
                    return Json(new { success = false, message = "No file information provided." });
                }

                List<FormUploadFilesInfo> fileInfo;
                try
                {
                    fileInfo = JsonConvert.DeserializeObject<List<FormUploadFilesInfo>>(filesInfoJson);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Invalid JSON format: {ex.Message}" });
                }

                string absolutePath = Server.MapPath("~/UploadFiles/");
                var manager = new FileManager(fileInfo, uploadedFiles, uploadTo, absolutePath, user, db);

                var result = manager.save();
                return Json(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
        }


        [HttpPost]
        public ActionResult CreateFolder()
        {
            string folderName = Request.Form.GetValues( "folder-name" )[0];
            string folderPath = Request.Form.GetValues("folder-path")[0];

            // Check user authentication
            HttpCookie cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null)
            {
                return Json(new { success = false, message = "forbiden" });
            }

            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie.Value);
            string userEmail = ticket.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "invalid cookie" });
            }

            MVCDBContext db = new MVCDBContext();
            User user = db.Users.FirstOrDefault(u => u.UserEmail.Equals(userEmail));

            if (user == null)
            {
                return Json(new { success = false, message = "user not found" });
            }

            if(folderName.ToLower().Equals("root"))
            {
                return Json(new { success = false, message = "invalid folder name" });
            }

            List<string> list_path_folder = new List<string>();
            list_path_folder.Add("root");

            if(!string.IsNullOrEmpty(folderPath))
            {
                list_path_folder.AddRange(folderPath.Split('/'));
            }


            // check and get parent folder id
            int folderParentId = FolderPath.CheckFolderExists(list_path_folder, user.UserId, db);

            if (folderParentId == -1)
            {
                TempData["ErrorMessage"] = "Can't create new folder, One or more folders in the path are missing.";
                return Redirect(Request.UrlReferrer.ToString());
            }

            // create new folder
            Folder folder = new Folder
            {
                UserId = user.UserId,
                FolderName = folderName,
                FolderParentId = folderParentId,
                FolderCreateAt = DateTime.Now,
            };

            db.Folders.Add(folder);
            db.SaveChanges();

            TempData["SuccessMessage"]= $"Create new folder [{folderName}] successfully.";
            return Redirect(Request.UrlReferrer.ToString());
        }

        [HttpDelete]
        public ActionResult DeleteFolder(int id, string name, string type, string folder_path)
        {
            List<string> validType = new List<string>();
            validType.Add("folder");
            validType.Add("source");

            List<string> folderPath = new List<string>();
            folderPath.Add("root");

            if(!string.IsNullOrEmpty(folder_path))
            {
                folderPath.AddRange(folder_path.Split('/'));
            }

            HttpCookie cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null)
            {
                return Json(new { success = false, message = "forbiden" });
            }

            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie.Value);
            string userEmail = ticket.Name;

            if (userEmail == null)
            {
                return Json(new { success = false, message = "invalid cookie" });
            }

            MVCDBContext db = new MVCDBContext();
            User user = db.Users.FirstOrDefault(u => u.UserEmail.Equals(userEmail));

            if (user == null) 
            {
                return Json(new { success = false, message = "user not found" });
            }

            if(!validType.Any(t => t.Equals(type)))
            {
                return Json(new { success = false, message = "invalid type of source" });
            }

            switch (type)
            {
                case "folder":
                    break;
            }

            return Json(new { success = false, message = "." });
        }
    }
}