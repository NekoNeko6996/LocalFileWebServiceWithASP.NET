using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LocalFileWebService.Models
{
    public class AuthFormData
    {
        [Required(ErrorMessage = "Email is require"), EmailAddress(ErrorMessage = "Invalid email format")]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "Password is require")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords doesnot match")]
        public string RePassword { get; set; }
    }
}