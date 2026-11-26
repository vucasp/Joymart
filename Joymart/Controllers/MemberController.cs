using Joymart.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    // =====================================================================
    // CONTROLLER TÀI KHOẢN THÀNH VIÊN (MemberController)
    // Bao gồm:
    //   - MyOrders: Lịch sử đơn mua của thành viên (hỗ trợ lọc tab trạng thái).
    //   - OrderDetails: Chi tiết đơn mua.
    //   - CancelOrder: Khách tự hủy đơn khi đang ở trạng thái "Chờ xử lý" (hoàn kho).
    //   - ConfirmReceived: Khách chủ động bấm "Đã nhận được hàng" khi đơn "Đang giao"
    //     (nếu sau 7 ngày chưa bấm, OrderWorkflowHelper sẽ tự động xác nhận Hoàn thành).
    //   - Profile & ChangePassword: Cập nhật hồ sơ và đổi mật khẩu.
    // =====================================================================
    public class MemberController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        private bool IsLoggedIn()
        {
            return Session["UserID"] != null;
        }

        // GET: /Member/MyOrders
        public ActionResult MyOrders(string status)
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
            OrderWorkflowHelper.SyncAutoCompletedOrders(db);

            var query = db.OrderProes
                .Include(o => o.OrderDetails)
                .Where(o => o.CustomerID == customerId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            ViewBag.SelectedStatus = status;
            var orders = query.OrderByDescending(o => o.OrderID).ToList();

            return View(orders);
        }

        // GET: /Member/OrderDetails/5
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
            OrderWorkflowHelper.SyncAutoCompletedOrders(db);

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

        // POST: /Member/CancelOrder (Khách tự hủy đơn khi đang "Chờ xử lý" + hoàn trả tồn kho)
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

            return RedirectToAction("MyOrders");
        }

        // POST: /Member/ConfirmReceived (Người dùng xác nhận "Đã nhận được hàng" khi đơn "Đang giao")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmReceived(int orderId)
        {
            if (!IsLoggedIn() || Session["CustomerID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["CustomerID"];
            var order = db.OrderProes
                .FirstOrDefault(o => o.OrderID == orderId && o.CustomerID == customerId);

            if (order == null)
            {
                return HttpNotFound();
            }

            if (order.Status != "Đang giao")
            {
                TempData["MemberError"] = "Chỉ có thể xác nhận nhận hàng đối với đơn đang ở trạng thái \"Đang giao\".";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }

            order.Status = "Hoàn thành";
            order.CompletedDate = DateTime.Now;
            db.SaveChanges();

            TempData["MemberSuccess"] = "Cảm ơn bạn đã xác nhận nhận hàng thành công cho đơn #" + orderId + "! Giờ bạn đã có thể đánh giá sản phẩm trong đơn.";
            return RedirectToAction("OrderDetails", new { id = orderId });
        }

        // GET: /Member/Profile
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

        // POST: /Member/Profile
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

            Session["FullName"] = customer.FullName;

            TempData["MemberSuccess"] = "Cập nhật thông tin hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        // GET: /Member/ChangePassword
        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // POST: /Member/ChangePassword
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

            if (!PasswordHelper.Verify(model.OldPassword, account.PasswordSalt, account.PasswordHash))
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu hiện tại không chính xác.");
                return View(model);
            }

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
