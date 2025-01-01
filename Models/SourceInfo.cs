using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.ComponentModel;

namespace LocalFileWebService.Models
{
    public class Sources
    {
        [Key]
        public int SourceId { get; set; }

        [MaxLength(255), Required]
        public string SourceName { get; set; }
        [MaxLength(1000)]
        public string SourceDescription { get; set; }

        [Required]
        [RegularExpression(@"^(video|image|application)/[a-zA-Z0-9.-]+$", ErrorMessage = "Invalid MIME type.")]
        public string SourceType { get; set; }

        public DateTime UploadTime { get; set; } = DateTime.Now;

        [Url, Required]
        public string SourceUrl { get; set; }
    }
}