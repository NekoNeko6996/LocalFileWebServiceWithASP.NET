using LocalFileWebService.Class;
using LocalFileWebService.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;

namespace LocalFileWebService.Controllers
{
    public class AuthController : Controller
    {
        // GET: Authenticaton
        [HttpGet]
        public ActionResult Login()
        {
            ViewBag.currentBody = "login";
            return View();
        }

        public ActionResult SignUp()
        {
            ViewBag.currentBody = "signup";
            return View();
        }

        [HttpPost]
        public ActionResult SignUp(AuthFormData data)
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
                        UserEmail = data.UserEmail.ToLower(),
                        UserHash = hashedPassword
                    };
                    db.Users.Add(user);
                    db.SaveChanges();

                    // tạo folder root
                    int userId = db.Users.FirstOrDefault(u => u.UserEmail.Equals(data.UserEmail.ToLower())).UserId;
                    FolderPath.CreateRootFolder(userId, db);

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
        public ActionResult Login(AuthFormData user)
        {
            ModelState.Remove("RePassword");

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

        public ActionResult SignOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}