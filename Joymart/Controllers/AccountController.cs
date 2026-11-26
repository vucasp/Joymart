using Joymart.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    public class AccountController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // GET: /Account/SignUp
        [HttpGet]
        public ActionResult SignUp()
        {
            return View();
        }

        // POST: /Account/SignUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra trùng tên đăng nhập
            bool userNameExists = db.UserAccounts.Any(u => u.UserName == model.UserName);
            if (userNameExists)
            {
                ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            // Lấy Role Customer mặc định từ CSDL
            var customerRole = db.Roles.FirstOrDefault(r => r.RoleName == "Customer");
            if (customerRole == null)
            {
                ModelState.AddModelError("", "Không tìm thấy Role Customer trong hệ thống.");
                return View(model);
            }

            // Tạo Salt và Hash mật khẩu
            string salt = PasswordHelper.CreateSalt();
            string passwordHash = PasswordHelper.Hash(model.Password, salt);

            // Lưu đồng thời UserAccount và Customer trong Transaction
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var account = new UserAccount
                    {
                        UserName = model.UserName,
                        PasswordHash = passwordHash,
                        PasswordSalt = salt,
                        RoleID = customerRole.RoleID,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    db.UserAccounts.Add(account);
                    db.SaveChanges();

                    var customer = new Customer
                    {
                        UserID = account.UserID,
                        FullName = model.FullName,
                        Phone = model.Phone,
                        Email = model.Email,
                        Address = ""
                    };
                    db.Customers.Add(customer);
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["SignUpSuccess"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                catch
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo tài khoản. Vui lòng thử lại.");
                    return View(model);
                }
            }
        }

        // GET: /Account/Login
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var account = db.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserName == model.UserName && u.IsActive);

            if (account == null || !PasswordHelper.Verify(model.Password, account.PasswordSalt, account.PasswordHash))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            Session["UserID"] = account.UserID;
            Session["UserName"] = account.UserName;
            Session["Role"] = account.Role.RoleName;

            var customer = db.Customers.FirstOrDefault(c => c.UserID == account.UserID);
            if (customer != null)
            {
                Session["CustomerID"] = customer.CustomerID;
                Session["FullName"] = customer.FullName;
            }
            else
            {
                Session.Remove("CustomerID");
                Session.Remove("FullName");
            }

            // Nếu trước đó người dùng đang bấm Đặt hàng hoặc vào trang yêu cầu đăng nhập thì chuyển lại đúng trang đó
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Nếu là Admin đăng nhập trực tiếp thì chuyển thẳng vào giao diện Portal Quản trị riêng
            if (account.Role.RoleName == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Store");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            // Chỉ xóa Session tài khoản, KHÔNG gọi Session.Clear() để giữ lại giỏ hàng
            Session.Remove("UserID");
            Session.Remove("UserName");
            Session.Remove("Role");
            Session.Remove("CustomerID");
            Session.Remove("FullName");

            return RedirectToAction("Index", "Store");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}