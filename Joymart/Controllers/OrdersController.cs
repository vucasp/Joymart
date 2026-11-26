using Joymart.Filters;
using Joymart.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    // =====================================================================
    // CONTROLLER QUẢN LÝ ĐƠN HÀNG & XUẤT KHO GIAO VẬN (OrdersController)
    // Quy tắc nghiệp vụ:
    //   - Đơn "Chờ xử lý": Hiển thị nút "Xác nhận" -> sang trang Xuất hàng / Giao hàng.
    //   - Đơn "Đã xác nhận": Hiển thị nút "Giao hàng" -> sang trang Xuất hàng / Giao hàng.
    //   - Đơn "Đang giao": Không cho phép hủy đơn; Admin chỉ theo dõi & xem chi tiết
    //     tại /Orders/Delivering (không tự bấm hoàn thành; khách hàng tự xác nhận
    //     hoặc sau 7 ngày kể từ ShippedDate hệ thống tự động chuyển sang "Hoàn thành").
    // =====================================================================
    [AdminAuthorize]
    public class OrdersController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // GET: /Orders (Danh sách đơn đặt hàng kèm bộ lọc)
        public ActionResult Index(string keyword, string statusFilter, DateTime? fromDate, DateTime? toDate)
        {
            int autoCount = OrderWorkflowHelper.SyncAutoCompletedOrders(db);
            if (autoCount > 0 && TempData["OrderSuccessMsg"] == null)
            {
                TempData["OrderSuccessMsg"] = "Hệ thống vừa tự động chuyển " + autoCount + " đơn hàng giao quá 7 ngày sang trạng thái \"Hoàn thành\".";
            }

            var query = db.OrderProes
                .Include(o => o.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                int parsedOrderId;
                bool isNumeric = int.TryParse(keyword.TrimStart('#'), out parsedOrderId);

                query = query.Where(o =>
                    (isNumeric && o.OrderID == parsedOrderId) ||
                    o.RecipientName.Contains(keyword) ||
                    o.ShippingPhone.Contains(keyword) ||
                    o.Customer.FullName.Contains(keyword) ||
                    o.Customer.Phone.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            if (fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                query = query.Where(o => o.OrderDate >= start);
            }
            if (toDate.HasValue)
            {
                DateTime endExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < endExclusive);
            }

            var model = new AdminOrderFilterViewModel
            {
                Keyword = keyword,
                StatusFilter = statusFilter,
                FromDate = fromDate,
                ToDate = toDate,
                DeliveringCount = db.OrderProes.Count(o => o.Status == "Đang giao"),
                Orders = query.OrderByDescending(o => o.OrderID).ToList()
            };

            return View(model);
        }

        // GET: /Orders/Details/5 (Xem chi tiết đơn hàng)
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            OrderWorkflowHelper.SyncAutoCompletedOrders(db);

            var order = db.OrderProes
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: /Orders/Dispatch/5 (Trang Xuất hàng / Giao hàng)
        [HttpGet]
        public ActionResult Dispatch(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            OrderWorkflowHelper.SyncAutoCompletedOrders(db);

            var order = db.OrderProes
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            if (order.Status != "Chờ xử lý" && order.Status != "Đã xác nhận")
            {
                TempData["OrderErrorMsg"] = "Đơn hàng #" + order.OrderID + " đang ở trạng thái \"" + order.Status + "\", không thể thực hiện xuất hàng.";
                return RedirectToAction("Index");
            }

            decimal subTotal = order.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);

            var model = new OrderDispatchViewModel
            {
                Order = order,
                ItemsSubTotal = subTotal,
                SelectedShippedDate = order.ShippedDate ?? DateTime.Now
            };

            return View(model);
        }

        // POST: /Orders/Dispatch (Xác nhận xuất hàng -> "Đang giao" hoặc Hoãn xuất hàng -> "Đã xác nhận")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Dispatch(int orderId, DateTime? shippedDate, string submitAction)
        {
            var order = db.OrderProes.FirstOrDefault(o => o.OrderID == orderId);
            if (order == null)
            {
                return HttpNotFound();
            }

            if (order.Status != "Chờ xử lý" && order.Status != "Đã xác nhận")
            {
                TempData["OrderErrorMsg"] = "Đơn hàng #" + orderId + " không còn ở trạng thái chờ xuất hàng.";
                return RedirectToAction("Index");
            }

            if (submitAction == "defer")
            {
                order.Status = "Đã xác nhận";
                db.SaveChanges();

                TempData["OrderSuccessMsg"] = "Đã hoãn xuất hàng và cập nhật đơn #" + orderId + " sang trạng thái \"Đã xác nhận\".";
                return RedirectToAction("Index");
            }

            DateTime chosenDate = shippedDate ?? DateTime.Now;
            if (chosenDate.Date == DateTime.Now.Date)
            {
                order.ShippedDate = DateTime.Now;
            }
            else
            {
                order.ShippedDate = chosenDate.Date.AddHours(8);
            }

            order.Status = "Đang giao";
            db.SaveChanges();

            OrderWorkflowHelper.SyncAutoCompletedOrders(db);

            TempData["OrderSuccessMsg"] = "Đã xác nhận xuất hàng cho đơn #" + orderId + " (Ngày xuất: " + order.ShippedDate.Value.ToString("dd/MM/yyyy") + "). Trạng thái hiện tại: \"" + order.Status + "\".";
            return RedirectToAction("Delivering");
        }

        // GET: /Orders/Delivering (Chỉ theo dõi các đơn đang giao & xem chi tiết; tự hoàn thành sau 7 ngày)
        public ActionResult Delivering()
        {
            int autoCount = OrderWorkflowHelper.SyncAutoCompletedOrders(db);
            if (autoCount > 0 && TempData["OrderSuccessMsg"] == null)
            {
                TempData["OrderSuccessMsg"] = "Hệ thống đã tự động xác nhận \"Hoàn thành\" cho " + autoCount + " đơn hàng giao quá 7 ngày.";
            }

            var deliveringOrders = db.OrderProes
                .Include(o => o.Customer)
                .Where(o => o.Status == "Đang giao")
                .OrderBy(o => o.ShippedDate)
                .ThenByDescending(o => o.OrderID)
                .ToList();

            return View(deliveringOrders);
        }

        // POST: /Orders/CancelOrder (Chỉ cho phép hủy khi đơn ở "Chờ xử lý" hoặc "Đã xác nhận"; đơn "Đang giao" không được hủy)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelOrder(int orderId)
        {
            var order = db.OrderProes
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
            {
                return HttpNotFound();
            }

            if (order.Status == "Đang giao")
            {
                TempData["OrderErrorMsg"] = "Đơn hàng #" + orderId + " đang được giao nên không thể hủy đơn.";
                return RedirectToAction("Index");
            }

            if (order.Status == "Hoàn thành" || order.Status == "Đã hủy")
            {
                TempData["OrderErrorMsg"] = "Không thể hủy đơn hàng #" + orderId + " đang ở trạng thái \"" + order.Status + "\".";
                return RedirectToAction("Index");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = db.Products.Find(detail.ProductID);
                        if (product != null)
                        {
                            product.StockQuantity += detail.Quantity;
                        }
                    }

                    order.Status = "Đã hủy";
                    db.SaveChanges();
                    transaction.Commit();

                    TempData["OrderSuccessMsg"] = "Đã hủy đơn hàng #" + orderId + " và hoàn trả đầy đủ số lượng sản phẩm vào kho.";
                }
                catch
                {
                    transaction.Rollback();
                    TempData["OrderErrorMsg"] = "Có lỗi xảy ra khi hủy đơn hàng.";
                }
            }

            return RedirectToAction("Index");
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
