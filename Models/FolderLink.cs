using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace LocalFileWebService.Models
{
    public class FolderLink
    {
        public int SourceId { get; set; }
        public int FolderId { get; set; }

        public DateTime FolderLinkCreateAt { get; set; } = DateTime.Now;

        //
        public virtual Source Source { get; set; }
        public virtual Folder Folder { get; set; }
    }
}