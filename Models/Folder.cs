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

        [Required]  
        public int UserId { get; set; }

        [Required]
        public string FolderName { get; set; }

        public string FolderIconUrl { get; set; }

        public int FolderParentId { get; set; }

        public DateTime FolderCreateAt { get; set; } = DateTime.Now;

        //
        public virtual User User {  get; set; } 
    }
}