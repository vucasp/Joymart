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

            // 1. Kiểm tra trùng tên đăng nhập
            bool userNameExists = db.UserAccounts.Any(u => u.UserName == model.UserName);
            if (userNameExists)
            {
                ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            // 2. Lấy Role Customer mặc định từ CSDL
            var customerRole = db.Roles.FirstOrDefault(r => r.RoleName == "Customer");
            if (customerRole == null)
            {
                ModelState.AddModelError("", "Không tìm thấy Role Customer trong hệ thống.");
                return View(model);
            }

            // 3. Tạo Salt và Hash mật khẩu
            string salt = PasswordHelper.CreateSalt();
            string passwordHash = PasswordHelper.Hash(model.Password, salt);

            // 4. Lưu đồng thời UserAccount và Customer trong Transaction
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

            // Nếu trước đó người dùng đang bấm Đặt hàng thì chuyển thẳng lại trang Checkout
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
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

        // Action tạo tài khoản Admin mẫu cho bài thực hành (/Account/CreateAdminForLab)
        // Sau khi chạy 1 lần để tạo admin01 / Admin123, bạn nên comment hoặc xóa action này
        public ActionResult CreateAdminForLab()
        {
            if (db.UserAccounts.Any(u => u.UserName == "admin01"))
            {
                return Content("Tài khoản admin01 đã tồn tại (Mật khẩu: Admin123).");
            }

            var adminRole = db.Roles.FirstOrDefault(r => r.RoleName == "Admin");
            if (adminRole == null)
            {
                return Content("Không tìm thấy Role Admin trong bảng Role.");
            }

            string salt = PasswordHelper.CreateSalt();
            var admin = new UserAccount
            {
                UserName = "admin01",
                PasswordSalt = salt,
                PasswordHash = PasswordHelper.Hash("Admin123", salt),
                RoleID = adminRole.RoleID,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            db.UserAccounts.Add(admin);
            db.SaveChanges();

            return Content("Đã tạo thành công tài khoản Admin: admin01 / Admin123. Hãy quay lại /Account/Login để đăng nhập.");
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