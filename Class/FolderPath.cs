using System;
using System.Collections.Generic;
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

    }
}