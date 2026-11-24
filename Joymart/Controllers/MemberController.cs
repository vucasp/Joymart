using Joymart.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    public class MemberController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // Kiểm tra đã đăng nhập hay chưa
        private bool IsLoggedIn()
        {
            return Session["UserID"] != null;
        }

        // =====================================================
        // 1. LỊCH SỬ ĐƠN HÀNG CỦA TÔI (/Member/MyOrders)
        // =====================================================
        public ActionResult MyOrders()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            if (Session["CustomerID"] == null)
            {
                TempData["MemberError"] = "Tài khoản quản trị không có hồ sơ mua hàng cá nhân.";
                return RedirectToAction("Index", "Store");
            }

            int customerId = (int)Session["CustomerID"];
            var orders = db.OrderProes
                .Where(o => o.CustomerID == customerId)
                .OrderByDescending(o => o.OrderID)
                .ToList();

            return View(orders);
        }

        // =====================================================
        // 2. CHI TIẾT ĐƠN HÀNG CỦA TÔI (/Member/OrderDetails/5)
        // =====================================================
        public ActionResult OrderDetails(int? id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null || Session["CustomerID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int customerId = (int)Session["CustomerID"];

            // Chỉ lấy đúng đơn hàng thuộc về khách hàng đang đăng nhập
            var order = db.OrderProes
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id && o.CustomerID == customerId);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // =====================================================
        // 3. KHÁCH TỰ HỦY ĐƠN KHI CÒN "CHỜ XỬ LÝ" + HOÀN TỒN KHO
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelOrder(int orderId)
        {
            if (!IsLoggedIn() || Session["CustomerID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["CustomerID"];
            var order = db.OrderProes
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.OrderID == orderId && o.CustomerID == customerId);

            if (order == null)
            {
                return HttpNotFound();
            }

            if (order.Status != "Chờ xử lý")
            {
                TempData["MemberError"] = "Chỉ có thể hủy đơn hàng khi đang ở trạng thái \"Chờ xử lý\".";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    order.Status = "Đã hủy";

                    // Hoàn trả số lượng tồn kho cho từng sản phẩm
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = db.Products.Find(detail.ProductID);
                        if (product != null)
                        {
                            product.StockQuantity += detail.Quantity;
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["MemberSuccess"] = "Đã hủy đơn hàng #" + orderId + " thành công.";
                }
                catch
                {
                    transaction.Rollback();
                    TempData["MemberError"] = "Có lỗi xảy ra khi hủy đơn hàng. Vui lòng thử lại.";
                }
            }

            return RedirectToAction("OrderDetails", new { id = orderId });
        }

        // =====================================================
        // 4. CẬP NHẬT HỒ SƠ CÁ NHÂN (/Member/Profile)
        // =====================================================
        [HttpGet]
        public ActionResult Profile()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("ChangePassword");
            }

            int customerId = (int)Session["CustomerID"];
            var customer = db.Customers.Find(customerId);
            if (customer == null)
            {
                return HttpNotFound();
            }

            var model = new ProfileViewModel
            {
                UserName = Session["UserName"] != null ? Session["UserName"].ToString() : "",
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileViewModel model)
        {
            if (!IsLoggedIn() || Session["CustomerID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.UserName = Session["UserName"] != null ? Session["UserName"].ToString() : "";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int customerId = (int)Session["CustomerID"];
            var customer = db.Customers.Find(customerId);
            if (customer == null)
            {
                return HttpNotFound();
            }

            customer.FullName = model.FullName;
            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.Address = model.Address;
            db.SaveChanges();

            // Cập nhật lại tên hiển thị trên thanh Navbar
            Session["FullName"] = customer.FullName;

            TempData["MemberSuccess"] = "Cập nhật thông tin hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        // =====================================================
        // 5. ĐỔI MẬT KHẨU (/Member/ChangePassword)
        // =====================================================
        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int userId = (int)Session["UserID"];
            var account = db.UserAccounts.Find(userId);
            if (account == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra mật khẩu cũ
            if (!PasswordHelper.Verify(model.OldPassword, account.PasswordSalt, account.PasswordHash))
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu hiện tại không chính xác.");
                return View(model);
            }

            // Tạo Salt & Hash mới
            string newSalt = PasswordHelper.CreateSalt();
            account.PasswordSalt = newSalt;
            account.PasswordHash = PasswordHelper.Hash(model.NewPassword, newSalt);
            db.SaveChanges();

            TempData["MemberSuccess"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("ChangePassword");
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