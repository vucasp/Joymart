using Joymart.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    // =====================================================================
    // CONTROLLER GIỎ HÀNG, ĐẶT HÀNG & TRA CỨU ĐƠN HÀNG (ShoppingCartController)
    // Bao gồm:
    //   - ShowCart / AddToCart / UpdateQuantity / Remove / ClearCart / BagCart
    //   - Checkout: Tạo đơn hàng có tính phí vận chuyển tự động (Freeship >= 360.067đ)
    //               và trừ trực tiếp tồn kho Product.StockQuantity trong Transaction.
    //   - OrderSuccess: Hiển thị xác nhận đơn hàng vừa đặt (có chống xem trộm IDOR).
    //   - TrackOrder: Cho phép Khách vãng lai tra cứu tiến độ đơn bằng Mã đơn + SĐT.
    // =====================================================================
    public class ShoppingCartController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // Lấy đối tượng Giỏ hàng từ Session (nếu chưa có thì khởi tạo mới)
        private Cart GetCart()
        {
            var cart = Session["Cart"] as Cart;
            if (cart == null)
            {
                cart = new Cart();
                Session["Cart"] = cart;
            }
            return cart;
        }

        // Đồng bộ lại giá bán, tồn kho và trạng thái kinh doanh mới nhất từ CSDL cho giỏ hàng
        private void SyncCartWithDatabase(Cart cart)
        {
            var productIds = cart.Items.Select(i => i.ProductID).ToList();
            if (!productIds.Any()) return;

            var dbProducts = db.Products
                .Where(p => productIds.Contains(p.ProductID))
                .ToDictionary(p => p.ProductID);

            foreach (int pid in productIds)
            {
                Product dbProd;
                if (dbProducts.TryGetValue(pid, out dbProd))
                {
                    cart.SyncItem(dbProd);
                }
                else
                {
                    cart.Remove(pid);
                }
            }
        }

        // GET: /ShoppingCart/ShowCart (Xem chi tiết giỏ hàng & thanh tiến trình Freeship)
        public ActionResult ShowCart()
        {
            var cart = GetCart();
            SyncCartWithDatabase(cart);
            return View(cart);
        }

        // GET: /ShoppingCart/AddToCart/5 (Thêm sản phẩm vào giỏ hoặc Mua ngay)
        public ActionResult AddToCart(int id, int quantity = 1, string mode = "cart")
        {
            var product = db.Products.FirstOrDefault(p => p.ProductID == id && p.IsActive);
            if (product == null)
            {
                TempData["CartError"] = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.";
                return RedirectToAction("Index", "Store");
            }

            if (quantity < 1) quantity = 1;

            var cart = GetCart();
            int addedCount = 0;
            for (int i = 0; i < quantity; i++)
            {
                if (cart.Add(product))
                {
                    addedCount++;
                }
                else
                {
                    break;
                }
            }

            if (addedCount == 0)
            {
                TempData["CartError"] = "Không thể thêm sản phẩm. Số lượng đã đạt giới hạn tồn kho hoặc đã hết hàng.";
                return RedirectToAction("Details", "Products", new { id = id });
            }

            if (mode == "buy")
            {
                return RedirectToAction("Checkout");
            }

            return RedirectToAction("ShowCart");
        }

        // POST: /ShoppingCart/UpdateQuantity (Cập nhật số lượng từng dòng trong giỏ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            var product = db.Products.Find(productId);
            if (product == null)
            {
                TempData["CartError"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("ShowCart");
            }

            bool updated = GetCart().Update(productId, quantity, product.StockQuantity);
            if (!updated)
            {
                TempData["CartError"] = "Số lượng phải lớn hơn 0 và không vượt quá tồn kho hiện tại (" + product.StockQuantity + ").";
            }

            return RedirectToAction("ShowCart");
        }

        // POST: /ShoppingCart/Remove (Xóa 1 sản phẩm khỏi giỏ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(int productId)
        {
            GetCart().Remove(productId);
            return RedirectToAction("ShowCart");
        }

        // POST: /ShoppingCart/ClearCart (Xóa sạch toàn bộ giỏ hàng)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            GetCart().Clear();
            return RedirectToAction("ShowCart");
        }

        // Partial View hiển thị tóm tắt số lượng & tiền hàng trên thanh Navbar
        [ChildActionOnly]
        public PartialViewResult BagCart()
        {
            var cart = Session["Cart"] as Cart;
            ViewBag.CartQty = cart == null ? 0 : cart.TotalQuantity();
            ViewBag.CartTotal = cart == null ? 0m : cart.TotalMoney();
            return PartialView();
        }

        // GET: /ShoppingCart/Checkout (Trang điền thông tin nhận hàng)
        public ActionResult Checkout()
        {
            var cart = GetCart();
            SyncCartWithDatabase(cart);
            if (!cart.Items.Any())
            {
                return RedirectToAction("ShowCart");
            }

            var model = new CheckoutViewModel
            {
                Cart = cart
            };

            // Tự động điền thông tin nếu khách đã đăng nhập tài khoản thành viên
            if (Session["CustomerID"] != null)
            {
                int customerId = (int)Session["CustomerID"];
                var customer = db.Customers.Find(customerId);
                if (customer != null)
                {
                    model.FullName = customer.FullName;
                    model.Phone = customer.Phone;
                    model.Email = customer.Email;
                    model.ShippingAddress = customer.Address;
                }
            }

            return View(model);
        }

        // POST: /ShoppingCart/Checkout (Xác nhận đặt hàng & Trừ tồn kho trong Transaction)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();
            model.Cart = cart;

            if (!cart.Items.Any())
            {
                return RedirectToAction("ShowCart");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra lại tồn kho thực tế và đồng bộ giá bán mới nhất trước khi ghi đơn
            foreach (var item in cart.Items.ToList())
            {
                var product = db.Products.Find(item.ProductID);
                if (product == null || !product.IsActive || product.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError("", "Sản phẩm \"" + item.ProductName + "\" không còn đủ số lượng tồn kho.");
                    return View(model);
                }

                item.UnitPrice = product.Price;
                item.ProductName = product.ProductName;
                item.ImageUrl = product.ImageUrl;
                item.StockQuantity = product.StockQuantity;
            }

            decimal shippingFee = cart.ShippingFee();
            decimal grandTotal = cart.GrandTotal();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Customer customer = null;

                    if (Session["CustomerID"] != null)
                    {
                        int customerId = (int)Session["CustomerID"];
                        customer = db.Customers.Find(customerId);
                        if (customer == null)
                        {
                            ModelState.AddModelError("", "Không tìm thấy hồ sơ khách hàng.");
                            return View(model);
                        }

                        if (string.IsNullOrWhiteSpace(customer.Address) && !string.IsNullOrWhiteSpace(model.ShippingAddress))
                        {
                            customer.Address = model.ShippingAddress;
                        }
                    }
                    else
                    {
                        customer = new Customer
                        {
                            UserID = null,
                            FullName = model.FullName,
                            Phone = model.Phone,
                            Email = model.Email,
                            Address = model.ShippingAddress
                        };
                        db.Customers.Add(customer);
                        db.SaveChanges();
                    }

                    var order = new OrderPro
                    {
                        OrderDate = DateTime.Now,
                        CustomerID = customer.CustomerID,
                        RecipientName = model.FullName,
                        ShippingPhone = model.Phone,
                        ShippingAddress = model.ShippingAddress,
                        ShippingFee = shippingFee,
                        TotalAmount = grandTotal,
                        Status = "Chờ xử lý"
                    };
                    db.OrderProes.Add(order);
                    db.SaveChanges();

                    foreach (var item in cart.Items)
                    {
                        var product = db.Products.Find(item.ProductID);
                        var detail = new OrderDetail
                        {
                            OrderID = order.OrderID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        };
                        db.OrderDetails.Add(detail);

                        // Trừ trực tiếp số lượng tồn kho (tự động đồng bộ sang Quản lý kho)
                        product.StockQuantity -= item.Quantity;
                    }
                    db.SaveChanges();

                    transaction.Commit();
                    cart.Clear();
                    Session["LatestOrderId"] = order.OrderID;

                    return RedirectToAction("OrderSuccess", new { id = order.OrderID });
                }
                catch
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Có lỗi khi lưu đơn hàng. Vui lòng thử lại.");
                    return View(model);
                }
            }
        }

        // GET: /ShoppingCart/OrderSuccess/5 (Trang thông báo đặt hàng thành công)
        public ActionResult OrderSuccess(int id)
        {
            OrderWorkflowHelper.SyncAutoCompletedOrders(db);

            var order = db.OrderProes
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Bảo mật chống IDOR: Chỉ chủ đơn, phiên vừa đặt xong hoặc Admin mới xem được
            int? latestOrderId = Session["LatestOrderId"] as int?;
            int? currentCustomerId = Session["CustomerID"] as int?;
            string currentRole = Convert.ToString(Session["Role"]);

            bool isOwner = (currentCustomerId.HasValue && currentCustomerId.Value == order.CustomerID);
            bool isRecentOrder = (latestOrderId.HasValue && latestOrderId.Value == id);
            bool isAdmin = (currentRole == "Admin");

            if (!isOwner && !isRecentOrder && !isAdmin)
            {
                TempData["MemberError"] = "Bạn không có quyền xem thông tin đơn hàng này.";
                return RedirectToAction("Index", "Store");
            }

            return View(order);
        }

        // GET: /ShoppingCart/TrackOrder (Tra cứu đơn hàng dành cho Khách vãng lai & Thành viên)
        [HttpGet]
        public ActionResult TrackOrder(int? orderId, string phone)
        {
            OrderWorkflowHelper.SyncAutoCompletedOrders(db);

            var model = new TrackOrderViewModel
            {
                OrderID = orderId,
                Phone = phone,
                Searched = false
            };

            if (!orderId.HasValue && string.IsNullOrWhiteSpace(phone))
            {
                return View(model);
            }

            model.Searched = true;

            if (!orderId.HasValue || orderId.Value <= 0 || string.IsNullOrWhiteSpace(phone))
            {
                model.ErrorMessage = "Vui lòng nhập đầy đủ cả Mã đơn hàng và Số điện thoại nhận hàng.";
                return View(model);
            }

            string cleanPhone = phone.Trim();
            var order = db.OrderProes
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o =>
                    o.OrderID == orderId.Value &&
                    (o.ShippingPhone == cleanPhone || o.Customer.Phone == cleanPhone));

            if (order == null)
            {
                model.ErrorMessage = "Không tìm thấy đơn hàng khớp với Mã đơn #" + orderId.Value + " và số điện thoại " + cleanPhone + ".";
                return View(model);
            }

            model.FoundOrder = order;
            return View(model);
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
