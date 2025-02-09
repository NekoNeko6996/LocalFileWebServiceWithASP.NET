using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalFileWebService.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(255), EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string UserEmail { get; set; }

        [Required, MaxLength(64)]
        public string UserHash { get; set; }

        public DateTime UserCreateAt { get; set; } = DateTime.Now;

        // list Source
        public ICollection<Source> Sources { get; set; }
    }
}