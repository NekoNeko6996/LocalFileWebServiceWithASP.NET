using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace LocalFileWebService.Models
{
    public class Folder
    {
        [Key]
        public int FolderId { get; set; }
        public string FolderName { get; set; }

    }
}