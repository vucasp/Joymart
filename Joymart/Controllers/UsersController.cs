using Joymart.Filters;
using Joymart.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    [AdminAuthorize]
    public class UsersController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // GET: /Users (Danh sách tài khoản & Phân quyền Admin)
        public ActionResult Index(string keyword, string roleFilter)
        {
            int currentUserId = Convert.ToInt32(Session["UserID"]);

            var query = db.UserAccounts
                .Include(u => u.Role)
                .Include(u => u.Customers)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                query = query.Where(u => u.Role.RoleName == roleFilter);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(u =>
                    u.UserName.Contains(keyword) ||
                    u.Customers.Any(c =>
                        c.FullName.Contains(keyword) ||
                        c.Email.Contains(keyword) ||
                        c.Phone.Contains(keyword)));
            }

            var users = query
                .OrderBy(u => u.RoleID)
                .ThenBy(u => u.UserID)
                .ToList()
                .Select(u =>
                {
                    var profile = u.Customers.FirstOrDefault();
                    return new UserListItemViewModel
                    {
                        UserID = u.UserID,
                        UserName = u.UserName,
                        RoleName = u.Role != null ? u.Role.RoleName : "Customer",
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        FullName = profile != null ? profile.FullName : null,
                        Phone = profile != null ? profile.Phone : null,
                        Email = profile != null ? profile.Email : null
                    };
                })
                .ToList();

            var model = new UserManagementViewModel
            {
                Keyword = keyword,
                RoleFilter = roleFilter,
                CurrentUserId = currentUserId,
                Users = users
            };

            return View(model);
        }

        // POST: /Users/ChangeRole (Cấp quyền Admin hoặc hạ về Customer)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeRole(int userId, string targetRole)
        {
            int currentUserId = Convert.ToInt32(Session["UserID"]);

            if (userId == currentUserId)
            {
                TempData["UserError"] = "Bạn không thể tự thay đổi quyền của tài khoản đang đăng nhập.";
                return RedirectToAction("Index");
            }

            if (targetRole != "Admin" && targetRole != "Customer")
            {
                TempData["UserError"] = "Quyền được chọn không hợp lệ.";
                return RedirectToAction("Index");
            }

            var account = db.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserID == userId);

            if (account == null)
            {
                TempData["UserError"] = "Không tìm thấy tài khoản cần phân quyền.";
                return RedirectToAction("Index");
            }

            var role = db.Roles.FirstOrDefault(r => r.RoleName == targetRole);
            if (role == null)
            {
                TempData["UserError"] = "Không tìm thấy quyền \"" + targetRole + "\" trong CSDL.";
                return RedirectToAction("Index");
            }

            if (account.Role.RoleName == "Admin" && targetRole != "Admin")
            {
                int remainingActiveAdmins = db.UserAccounts.Count(u =>
                    u.UserID != userId && u.IsActive && u.Role.RoleName == "Admin");

                if (remainingActiveAdmins == 0)
                {
                    TempData["UserError"] = "Hệ thống bắt buộc phải còn ít nhất 1 tài khoản Admin đang hoạt động.";
                    return RedirectToAction("Index");
                }
            }

            account.RoleID = role.RoleID;
            db.SaveChanges();

            TempData["UserSuccess"] = targetRole == "Admin"
                ? "Đã cấp quyền Quản trị viên (Admin) cho tài khoản \"" + account.UserName + "\"."
                : "Đã chuyển tài khoản \"" + account.UserName + "\" về quyền Khách hàng (Customer).";

            return RedirectToAction("Index");
        }

        // POST: /Users/ToggleActive (Khóa / Mở khóa tài khoản)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleActive(int userId)
        {
            int currentUserId = Convert.ToInt32(Session["UserID"]);

            if (userId == currentUserId)
            {
                TempData["UserError"] = "Bạn không thể tự khóa tài khoản đang đăng nhập.";
                return RedirectToAction("Index");
            }

            var account = db.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserID == userId);

            if (account == null)
            {
                TempData["UserError"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Index");
            }

            if (account.IsActive && account.Role.RoleName == "Admin")
            {
                int remainingActiveAdmins = db.UserAccounts.Count(u =>
                    u.UserID != userId && u.IsActive && u.Role.RoleName == "Admin");

                if (remainingActiveAdmins == 0)
                {
                    TempData["UserError"] = "Không thể khóa tài khoản Admin hoạt động cuối cùng của hệ thống.";
                    return RedirectToAction("Index");
                }
            }

            account.IsActive = !account.IsActive;
            db.SaveChanges();

            TempData["UserSuccess"] = account.IsActive
                ? "Đã mở khóa tài khoản \"" + account.UserName + "\"."
                : "Đã khóa tài khoản \"" + account.UserName + "\".";

            return RedirectToAction("Index");
        }

        // GET: /Users/Create (Tạo tài khoản mới trực tiếp từ trang Admin)
        [HttpGet]
        public ActionResult Create()
        {
            return View(new AdminCreateUserViewModel { RoleName = "Admin" });
        }

        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AdminCreateUserViewModel model)
        {
            if (model.RoleName != "Admin" && model.RoleName != "Customer")
            {
                ModelState.AddModelError("RoleName", "Quyền tài khoản không hợp lệ.");
            }

            // Nếu tạo tài khoản Khách hàng thì bắt buộc có Họ tên, SĐT, Email
            if (model.RoleName == "Customer")
            {
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    ModelState.AddModelError("FullName", "Vui lòng nhập họ tên khách hàng.");
                }
                if (string.IsNullOrWhiteSpace(model.Phone))
                {
                    ModelState.AddModelError("Phone", "Vui lòng nhập số điện thoại.");
                }
                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError("Email", "Vui lòng nhập email.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.UserName))
            {
                model.UserName = model.UserName.Trim();
                if (db.UserAccounts.Any(u => u.UserName == model.UserName))
                {
                    ModelState.AddModelError("UserName", "Tên đăng nhập này đã tồn tại.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var targetRole = db.Roles.FirstOrDefault(r => r.RoleName == model.RoleName);
            if (targetRole == null)
            {
                ModelState.AddModelError("", "Không tìm thấy quyền \"" + model.RoleName + "\" trong hệ thống.");
                return View(model);
            }

            string salt = PasswordHelper.CreateSalt();
            string hash = PasswordHelper.Hash(model.Password, salt);

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var account = new UserAccount
                    {
                        UserName = model.UserName,
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        RoleID = targetRole.RoleID,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    db.UserAccounts.Add(account);
                    db.SaveChanges();

                    if (model.RoleName == "Customer" || !string.IsNullOrWhiteSpace(model.FullName))
                    {
                        var customer = new Customer
                        {
                            UserID = account.UserID,
                            FullName = string.IsNullOrWhiteSpace(model.FullName) ? model.UserName : model.FullName.Trim(),
                            Phone = model.Phone,
                            Email = model.Email,
                            Address = ""
                        };
                        db.Customers.Add(customer);
                        db.SaveChanges();
                    }

                    transaction.Commit();
                    TempData["UserSuccess"] = "Đã tạo tài khoản \"" + account.UserName + "\" với quyền " + model.RoleName + " thành công.";
                    return RedirectToAction("Index");
                }
                catch
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu tài khoản mới.");
                    return View(model);
                }
            }
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
