using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using LocalFileWebService.Models;

namespace LocalFileWebService.Class
{
    public class FolderPath
    {
        public static int CheckFolderExists(List<string> path, int userId, MVCDBContext context)
        {
            // load folder of user
            int currentParentId = -1;
            
            foreach (var folderName in path)
            {   
                var folder = context.Folders
                    .Where(f => f.UserId == userId && f.FolderParentId == currentParentId && f.FolderName == folderName)
                    .Select(f => new { f.FolderId })
                    .FirstOrDefault();
                if (folder == null)
                {
                    return -1;
                }

                currentParentId = folder.FolderId;
            }

            return currentParentId;
        }

        public static bool CreateRootFolder(int userId, MVCDBContext context)
        {
            try
            {
                Folder rootFolder = new Folder
                {
                    UserId = userId,
                    FolderCreateAt = DateTime.Now,
                    FolderIconUrl = null,
                    FolderName = "root",
                    FolderParentId = -1,

                };

                context.Folders.Add(rootFolder);
                context.SaveChanges();

                return true;
            }
            catch (Exception ex) 
            {
                throw new Exception(ex.Message);
            }
        }

        public static void RemoveFolder(int userId, int folderId, string absoluteFolderPath, MVCDBContext context)
        {
            try
            {
                // Lấy danh sách thư mục con
                List<Folder> folders = context.Folders.Where(f => f.FolderParentId == folderId).ToList();

                // Lấy danh sách file trong folder
                List<Source> sources = (from source in context.Sources
                                        join folderLink in context.FolderLinks
                                        on source.SourceId equals folderLink.SourceId
                                        where folderLink.FolderId == folderId
                                        select source).ToList();

                // Xóa file vật lý và xóa record trong DB
                List<FolderLink> folderLinksToRemove = new List<FolderLink>();
                foreach (Source source in sources)
                {
                    string filePath = Path.Combine(absoluteFolderPath, source.SourceName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Console.WriteLine($"Đã xóa file: {filePath}");
                    }

                    var folderLink = context.FolderLinks.FirstOrDefault(f => f.SourceId == source.SourceId);
                    if (folderLink != null)
                    {
                        folderLinksToRemove.Add(folderLink);
                    }

                    context.Sources.Remove(source);
                }

                // Xóa tất cả folderLink cùng lúc (tối ưu hơn so với từng cái một)
                context.FolderLinks.RemoveRange(folderLinksToRemove);

                // Đệ quy xóa thư mục con trước
                foreach (var folder in folders)
                {
                    string childFolderPath = Path.Combine(absoluteFolderPath, folder.FolderName);
                    RemoveFolder(userId, folder.FolderId, childFolderPath, context);

                    // Xóa thư mục con nếu rỗng
                    if (Directory.Exists(childFolderPath) && Directory.GetFileSystemEntries(childFolderPath).Length == 0)
                    {
                        context.Folders.Remove(folder);
                        Directory.Delete(childFolderPath);
                        Console.WriteLine($"Đã xóa thư mục: {childFolderPath}");
                    }
                }

                // Xóa thư mục gốc nếu rỗng
                if (Directory.Exists(absoluteFolderPath) && Directory.GetFileSystemEntries(absoluteFolderPath).Length == 0)
                {
                    var folderToDelete = context.Folders.FirstOrDefault(f => f.FolderId == folderId);
                    if (folderToDelete != null)
                    {
                        context.Folders.Remove(folderToDelete);
                    }
                    Directory.Delete(absoluteFolderPath);
                    Console.WriteLine($"Đã xóa thư mục gốc: {absoluteFolderPath}");
                }

                // Lưu thay đổi vào DB sau khi xóa tất cả
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xóa thư mục: {ex.Message}");
                throw;
            }
        }
    }
}