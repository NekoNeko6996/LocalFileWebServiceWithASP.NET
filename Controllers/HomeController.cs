using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LocalFileWebService.Models;

namespace LocalFileWebService.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            MVCDBContext db = new MVCDBContext();
            List<Sources> sources = db.Sources.ToList();
            ViewBag.Sources = sources;
            return View();
        }

        [HttpGet]
        public ActionResult GetFile()
        {
            var db = new MVCDBContext();
            Sources source = db.Sources.Where(s => s.SourceId.Equals(1)).FirstOrDefault();
            string _ = source.SourceUrl.ToString();
            var filePath = Path.Combine("C:\\Users\\tranp\\Downloads\\【AMV】 Hololive × Tấm Lòng Son Remix.mp4"); // Đường dẫn tệp
            if (System.IO.File.Exists(filePath))
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath); // Đọc file
                var fileType = "video/mp4"; // Kiểu file, ví dụ: "image/jpeg" hoặc "video/mp4"
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