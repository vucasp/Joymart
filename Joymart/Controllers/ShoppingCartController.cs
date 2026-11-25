using Joymart.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    public class ShoppingCartController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // Lấy Cart hiện tại từ Session hoặc khởi tạo mới
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

        private void SyncCartWithDatabase(Cart cart)
        {
            var productIds = cart.Items.Select(i => i.ProductID).ToList();
            if (!productIds.Any()) return;

            var dbProducts = db.Products.Where(p => productIds.Contains(p.ProductID)).ToDictionary(p => p.ProductID);
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

        // GET: /ShoppingCart/ShowCart
        public ActionResult ShowCart()
        {
            var cart = GetCart();
            SyncCartWithDatabase(cart);
            return View(cart);
        }

        // GET: /ShoppingCart/AddToCart/5
        public ActionResult AddToCart(int id, int quantity = 1, string mode = "cart")
        {
            var product = db.Products.FirstOrDefault(p => p.ProductID == id && p.IsActive);
            if (product == null)
            {
                TempData["CartError"] = "Sản phẩm không tồn tại hoặc đã ngừng bán.";
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

            // Nếu bấm "Mua ngay" -> chuyển thẳng tới trang Checkout
            if (mode == "buy")
            {
                return RedirectToAction("Checkout");
            }

            // Nếu bấm "Thêm vào giỏ hàng" -> chuyển sang trang Giỏ hàng
            return RedirectToAction("ShowCart");
        }

        // POST: /ShoppingCart/UpdateQuantity
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

        // POST: /ShoppingCart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(int productId)
        {
            GetCart().Remove(productId);
            return RedirectToAction("ShowCart");
        }

        // POST: /ShoppingCart/ClearCart (Bài tập 1: Xóa toàn bộ giỏ hàng)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            GetCart().Clear();
            return RedirectToAction("ShowCart");
        }

        // Partial View hiển thị số lượng & tổng tiền giỏ hàng trên Navbar
        [ChildActionOnly]
        public PartialViewResult BagCart()
        {
            var cart = Session["Cart"] as Cart;
            ViewBag.CartQty = cart == null ? 0 : cart.TotalQuantity();
            ViewBag.CartTotal = cart == null ? 0 : cart.TotalMoney();
            return PartialView();
        }

        // GET: /ShoppingCart/Checkout
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

            // Nếu Customer đã đăng nhập -> tự động điền thông tin hồ sơ
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

        // POST: /ShoppingCart/Checkout
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

            // Kiểm tra lại tồn kho & đồng bộ giá bán thực tế trước khi tạo đơn
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

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Customer customer = null;

                    // Trường hợp 1: Customer đã đăng nhập -> dùng CustomerID hiện tại
                    if (Session["CustomerID"] != null)
                    {
                        int customerId = (int)Session["CustomerID"];
                        customer = db.Customers.Find(customerId);
                        if (customer == null)
                        {
                            ModelState.AddModelError("", "Không tìm thấy hồ sơ khách hàng.");
                            return View(model);
                        }

                        // Nếu khách hàng chưa có địa chỉ thì tự động cập nhật từ đơn hàng
                        if (string.IsNullOrWhiteSpace(customer.Address) && !string.IsNullOrWhiteSpace(model.ShippingAddress))
                        {
                            customer.Address = model.ShippingAddress;
                        }
                    }
                    // Trường hợp 2: Guest -> tạo bản ghi Customer mới (UserID = null)
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

                    // Tạo OrderPro với tổng tiền bằng đúng tổng tiền giỏ hàng
                    var order = new OrderPro
                    {
                        OrderDate = DateTime.Now,
                        CustomerID = customer.CustomerID,
                        RecipientName = model.FullName,
                        ShippingPhone = model.Phone,
                        ShippingAddress = model.ShippingAddress,
                        TotalAmount = cart.TotalMoney(),
                        Status = "Chờ xử lý"
                    };
                    db.OrderProes.Add(order);
                    db.SaveChanges();

                    // Tạo OrderDetail và trừ tồn kho Product
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

        // GET: /ShoppingCart/OrderSuccess/5
        public ActionResult OrderSuccess(int id)
        {
            var order = db.OrderProes
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra bảo mật (chống IDOR - xem trộm đơn hàng người khác):
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