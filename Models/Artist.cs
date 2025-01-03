using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace LocalFileWebService.Models
{
    public class Artist
    {
        [Key]
        public int ArtistId { get; set; }
        [Required]
        public string ArtistName { get; set; }
        public string ArtisPageLink { get; set; }
        public string ArtisAvatarUrl { get; set; }
        public DateTime ArtistCreateAt { get; set; } = DateTime.Now;
    }
}