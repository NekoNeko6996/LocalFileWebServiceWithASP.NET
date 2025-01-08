using FFMpegCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MediaInfo;
using FFMpegCore.Arguments;

namespace LocalFileWebService.Class
{
    public class Media
    {
        public class MediaInfomation
        {
            public int Duration { get; set; }
            public long Size { get; set; }
            public double FrameRate { get; set; }
        }

        public static byte[] GetThumbnail(string videoPath, string exportThumbnailPath, string thumbnailName)
        {
            string thumbnailFilePath = Path.Combine(exportThumbnailPath, thumbnailName);
            // Lấy frame đầu tiên
            FFMpeg.Snapshot(videoPath, thumbnailFilePath, null, TimeSpan.FromSeconds(0));

            // Trả về ảnh
            byte[] imageBytes = System.IO.File.ReadAllBytes(thumbnailFilePath);
            return imageBytes;
        }

        public static MediaInfomation GetMediaInfo(string filePath)
        {
            var mediaInfo = new MediaInfo.DotNetWrapper.MediaInfoWrapper(filePath);
            
            // Lấy các thông tin cơ bản
            return new MediaInfomation
            {
                Duration = mediaInfo.Duration,
                Size = mediaInfo.Size,
                FrameRate = mediaInfo.Framerate,
            };
        }
    }
}