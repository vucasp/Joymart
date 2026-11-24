using Joymart.Filters;
using Joymart.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    [AdminAuthorize]
    public class OrdersController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // GET: /Orders (Danh sách tất cả đơn hàng)
        public ActionResult Index()
        {
            var orders = db.OrderProes
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderID)
                .ToList();

            return View(orders);
        }

        // GET: /Orders/Details/5 (Chi tiết đơn hàng)
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

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

        // POST: /Orders/UpdateStatus (Cập nhật trạng thái đơn hàng)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int orderId, string status)
        {
            string[] validStatuses = { "Chờ xử lý", "Đã xác nhận", "Đang giao", "Hoàn thành", "Đã hủy" };

            var order = db.OrderProes
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
            {
                return HttpNotFound();
            }

            if (!validStatuses.Contains(status))
            {
                return RedirectToAction("Details", new { id = orderId });
            }

            // Không cho phép mở lại đơn đã hủy để đảm bảo tính chính xác tồn kho
            if (order.Status == "Đã hủy" && status != "Đã hủy")
            {
                TempData["OrderSuccessMsg"] = "Đơn hàng #" + orderId + " đã ở trạng thái Hủy và đã hoàn tồn kho, không thể chuyển sang trạng thái khác.";
                return RedirectToAction("Details", new { id = orderId });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Nếu chuyển từ trạng thái khác sang "Đã hủy" -> hoàn lại tồn kho
                    if (status == "Đã hủy" && order.Status != "Đã hủy")
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            var product = db.Products.Find(detail.ProductID);
                            if (product != null)
                            {
                                product.StockQuantity += detail.Quantity;
                            }
                        }
                    }

                    order.Status = status;
                    db.SaveChanges();
                    transaction.Commit();

                    TempData["OrderSuccessMsg"] = status == "Đã hủy"
                        ? "Đã hủy đơn hàng #" + orderId + " và hoàn trả số lượng sản phẩm vào kho."
                        : "Đã cập nhật trạng thái đơn hàng #" + orderId + " thành \"" + status + "\".";
                }
                catch
                {
                    transaction.Rollback();
                    TempData["OrderSuccessMsg"] = "Có lỗi khi cập nhật trạng thái đơn hàng.";
                }
            }

            return RedirectToAction("Details", new { id = orderId });
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