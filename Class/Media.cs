using FFMpegCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LocalFileWebService.Class
{
    public class Media
    {
        public static byte[] GetThumbnail(string videoPath)
        {
            string thumbnailPath = "C:\\Users\\tranp\\Desktop\\ASP NET";

            // Lấy frame đầu tiên
            FFMpeg.Snapshot("C:\\Users\\tranp\\Downloads\\【AMV】 Hololive × Tấm Lòng Son Remix.mp4", thumbnailPath, null, TimeSpan.FromSeconds(0));

            // Trả về ảnh
            byte[] imageBytes = System.IO.File.ReadAllBytes(thumbnailPath);
            // Dọn dẹp tệp tạm
            System.IO.File.Delete(thumbnailPath); 

            return imageBytes;
        }
    }
}