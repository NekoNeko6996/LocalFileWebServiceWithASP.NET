﻿using System;
using System.ComponentModel.DataAnnotations;

namespace LocalFileWebService.Models
{
    public class Source
    {
        [Key]
        public int SourceId { get; set; }

        [Required]
        public int UserId { get; set; }

        [MaxLength(255), Required]
        public string SourceName { get; set; }
        [MaxLength(1000)]
        public string SourceDescription { get; set; }

        [Required]
        [RegularExpression(@"^(video|image|application)/[a-zA-Z0-9.-]+$", ErrorMessage = "Invalid MIME type.")]
        public string SourceType { get; set; }

        // array of tags
        public string SourceTag { get; set; }

        public int ArtistId { get; set; }

        public DateTime UploadTime { get; set; } = DateTime.Now;

        [Required]
        public string SourceUrl { get; set; }

        public string SourceThumbnailPath {  get; set; }
        public int SourceLength { get; set; } = 0;

        //
        public virtual User User { get; set; }
        public virtual Artist Artist { get; set; }
    }
}