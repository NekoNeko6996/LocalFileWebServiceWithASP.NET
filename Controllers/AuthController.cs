using LocalFileWebService.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using BCrypt.Net;

using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

using LocalFileWebService.Class;
using System.Net;
using System.Web.Security;

namespace LocalFileWebService.Controllers
{
    public class AuthController : Controller
    {

        public class SignUpFormData 
        {
            [Required]
            public string UserEmail { get; set; }
            [Required]
            public string Password { get; set; }
        }

        // GET: Authenticaton
        [HttpGet]
        public ActionResult Login()
        {
            string token = (string) Session["token"];
            ViewBag.currentBody = "login";
            return View();
        }

        public ActionResult SignUp()
        {
            ViewBag.currentBody = "signup";
            return View();
        }

        [HttpPost]
        public ActionResult SignUp(SignUpFormData data)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Form không hợp lệ!");
                return View(data);
            }

            try
            {
                using (MVCDBContext db = new MVCDBContext())
                {
                    // Kiểm tra email đã tồn tại
                    if (db.Users.Any(u => u.UserEmail.ToLower() == data.UserEmail.ToLower()))
                    {
                        ModelState.AddModelError("", "Email đã tồn tại. Vui lòng chọn email khác.");
                        return View(data);
                    }

                    // Hash mật khẩu
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(data.Password);

                    // Lưu thông tin người dùng
                    User user = new User
                    {
                        UserEmail = data.UserEmail,
                        UserHash = hashedPassword
                    };
                    db.Users.Add(user);
                    db.SaveChanges();

                    // Đăng nhập người dùng sau khi đăng ký
                    FormsAuthentication.SetAuthCookie(data.UserEmail, false);

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xử lý dữ liệu. Vui lòng thử lại.");
                return View(data);
            }
        }


        [HttpPost]
        public ActionResult Login(FormDataUser user)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "From không hợp lệ!");
                return View();
            }

            MVCDBContext db = new MVCDBContext();
            User __user = db.Users.FirstOrDefault(u => u.UserEmail.ToLower().Equals(user.UserEmail.ToLower()));

            if (__user == null)
            {
                ModelState.AddModelError("", "Người dùng không tồn tại");
                return View();
            }

            if (BCrypt.Net.BCrypt.Verify(user.Password, __user.UserHash))
            {
                //Session["token"] = JWT.GenerateToken(db.Users.FirstOrDefault(u => u.UserEmail.ToLower() == user.UserEmail.ToLower()).UserId);

                // tạo cookie đăng nhập
                FormsAuthentication.SetAuthCookie(__user.UserEmail.ToString(), false);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác!");
            return View();
        }
    }
}