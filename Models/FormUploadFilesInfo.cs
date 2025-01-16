using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LocalFileWebService.Models
{
    public class FormUploadFilesInfo
    {
        public string FileName { get; set; }
        public string FileDescription { get; set; }
        public int ArtistId { get; set; }
        public List<string> Tags { get; set; }
    }
}