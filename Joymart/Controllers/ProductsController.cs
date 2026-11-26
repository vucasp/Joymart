using Joymart.Filters;
using Joymart.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    // =====================================================================
    // CONTROLLER SẢN PHẨM & ĐÁNH GIÁ SẢN PHẨM (ProductsController)
    // Bao gồm:
    //   - Quản trị Admin: Index (kèm bộ lọc), Create, Edit, Delete
    //   - Xử lý ảnh SaveProductImage: Đối chiếu SHA-256 tránh tạo ảnh trùng lặp
    //   - Khách hàng: Details (hiển thị chi tiết, 4 sản phẩm liên quan cùng danh mục,
    //                 danh sách đánh giá & điểm trung bình)
    //   - AddReview: Chỉ cho phép thành viên đã mua sản phẩm ở đơn "Hoàn thành" đánh giá
    // =====================================================================
    public class ProductsController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // Lưu ảnh sản phẩm vào ~/Content/images/products:
        //   - Nếu trùng nội dung SHA-256 với file có sẵn -> tái sử dụng luôn đường dẫn cũ
        //   - Nếu là ảnh mới -> giữ tên gốc, chỉ thêm -1, -2 nếu trùng tên nhưng khác nội dung
        private string SaveProductImage(HttpPostedFileBase imageFile, out string errorMessage)
        {
            errorMessage = null;

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            string originalFileName = Path.GetFileName(imageFile.FileName);
            string ext = Path.GetExtension(originalFileName)?.ToLower();

            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                errorMessage = "Chỉ chấp nhận file ảnh định dạng .jpg, .jpeg, .png";
                return null;
            }

            if (imageFile.ContentLength > 2 * 1024 * 1024)
            {
                errorMessage = "Dung lượng ảnh tối đa cho phép là 2MB.";
                return null;
            }

            string folderPath = Server.MapPath("~/Content/images/products");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            try
            {
                byte[] uploadedBytes;
                using (var ms = new MemoryStream())
                {
                    if (imageFile.InputStream.CanSeek)
                    {
                        imageFile.InputStream.Position = 0;
                    }
                    imageFile.InputStream.CopyTo(ms);
                    uploadedBytes = ms.ToArray();
                }

                using (var sha256 = SHA256.Create())
                {
                    byte[] uploadedHash = sha256.ComputeHash(uploadedBytes);

                    foreach (string existingPath in Directory.GetFiles(folderPath))
                    {
                        var fileInfo = new FileInfo(existingPath);
                        if (fileInfo.Length != uploadedBytes.Length)
                        {
                            continue;
                        }

                        using (var fs = new FileStream(existingPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            byte[] existingHash = sha256.ComputeHash(fs);
                            if (uploadedHash.SequenceEqual(existingHash))
                            {
                                return "products/" + fileInfo.Name;
                            }
                        }
                    }
                }

                string baseName = Path.GetFileNameWithoutExtension(originalFileName);
                foreach (char invalidChar in Path.GetInvalidFileNameChars())
                {
                    baseName = baseName.Replace(invalidChar.ToString(), "");
                }
                if (string.IsNullOrWhiteSpace(baseName))
                {
                    baseName = "product";
                }

                string fileName = baseName + ext;
                string fullPath = Path.Combine(folderPath, fileName);
                int counter = 1;

                while (System.IO.File.Exists(fullPath))
                {
                    fileName = baseName + "-" + counter + ext;
                    fullPath = Path.Combine(folderPath, fileName);
                    counter++;
                }

                System.IO.File.WriteAllBytes(fullPath, uploadedBytes);
                return "products/" + fileName;
            }
            catch (Exception ex)
            {
                errorMessage = "Có lỗi xảy ra khi lưu file ảnh: " + ex.Message;
                return null;
            }
        }

        // Kiểm tra tính hợp lệ dữ liệu sản phẩm ở phía Server
        private void ValidateProduct(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError("ProductName", "Vui lòng nhập tên sản phẩm.");
            }
            else
            {
                product.ProductName = product.ProductName.Trim();
                if (product.ProductName.Length > 150)
                {
                    ModelState.AddModelError("ProductName", "Tên sản phẩm không được vượt quá 150 ký tự.");
                }
            }

            if (!string.IsNullOrEmpty(product.Description) && product.Description.Length > 500)
            {
                ModelState.AddModelError("Description", "Mô tả sản phẩm không được vượt quá 500 ký tự.");
            }

            if (!db.Categories.Any(c => c.CategoryID == product.CategoryID))
            {
                ModelState.AddModelError("CategoryID", "Danh mục sản phẩm không hợp lệ.");
            }

            if (product.Price < 0)
            {
                ModelState.AddModelError("Price", "Đơn giá không được nhỏ hơn 0.");
            }

            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError("StockQuantity", "Số lượng tồn kho không được nhỏ hơn 0.");
            }
        }

        // GET: /Products (Trang Quản lý sản phẩm cho Admin, hỗ trợ tìm kiếm & lọc)
        [AdminAuthorize]
        public ActionResult Index(string keyword, int? categoryId, string statusFilter)
        {
            var query = db.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p => p.ProductName.Contains(keyword) || p.Description.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            switch (statusFilter)
            {
                case "active":
                    query = query.Where(p => p.IsActive);
                    break;
                case "inactive":
                    query = query.Where(p => !p.IsActive);
                    break;
                case "lowstock":
                    query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= 5);
                    break;
                case "outofstock":
                    query = query.Where(p => p.StockQuantity <= 0);
                    break;
            }

            var model = new AdminProductListViewModel
            {
                Keyword = keyword,
                CategoryId = categoryId,
                StatusFilter = statusFilter,
                Categories = db.Categories.OrderBy(c => c.CategoryName).ToList(),
                Products = query.OrderByDescending(p => p.ProductID).ToList()
            };

            return View(model);
        }

        // GET: /Products/Details/5 (Trang Chi tiết sản phẩm cho khách hàng + Sản phẩm liên quan + Đánh giá)
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            OrderWorkflowHelper.SyncAutoCompletedOrders(db);

            Product product = db.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null) return HttpNotFound();

            // Lấy tối đa 4 sản phẩm đang kinh doanh cùng danh mục (trừ sản phẩm hiện tại)
            var relatedProducts = db.Products
                .Where(p => p.IsActive && p.CategoryID == product.CategoryID && p.ProductID != product.ProductID)
                .OrderByDescending(p => p.ProductID)
                .Take(4)
                .ToList();

            // Lấy danh sách đánh giá của sản phẩm này
            var reviews = db.ProductReviews
                .Include(r => r.Customer)
                .Where(r => r.ProductID == product.ProductID)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            double avgRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0.0;

            // Kiểm tra khách đang đăng nhập có đơn "Hoàn thành" chứa sản phẩm này hay không
            bool canReview = false;
            ProductReview existingReview = null;
            if (Session["CustomerID"] != null)
            {
                int customerId = (int)Session["CustomerID"];
                canReview = db.OrderDetails.Any(d =>
                    d.ProductID == product.ProductID &&
                    d.OrderPro.CustomerID == customerId &&
                    d.OrderPro.Status == "Hoàn thành");

                existingReview = reviews.FirstOrDefault(r => r.CustomerID == customerId);
            }

            var model = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts,
                Reviews = reviews,
                AverageRating = avgRating,
                CanReview = canReview,
                ExistingUserReview = existingReview
            };

            return View(model);
        }

        // POST: /Products/AddReview (Gửi hoặc cập nhật đánh giá sản phẩm từ khách đã mua hoàn thành)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(int productId, int rating, string comment)
        {
            if (Session["CustomerID"] == null)
            {
                TempData["CartError"] = "Vui lòng đăng nhập tài khoản khách hàng để gửi đánh giá.";
                return RedirectToAction("Details", new { id = productId });
            }

            int customerId = (int)Session["CustomerID"];
            OrderWorkflowHelper.SyncAutoCompletedOrders(db);

            bool hasCompletedPurchase = db.OrderDetails.Any(d =>
                d.ProductID == productId &&
                d.OrderPro.CustomerID == customerId &&
                d.OrderPro.Status == "Hoàn thành");

            if (!hasCompletedPurchase)
            {
                TempData["CartError"] = "Chỉ khách hàng đã mua sản phẩm này trong đơn hàng Hoàn thành mới có thể gửi đánh giá.";
                return RedirectToAction("Details", new { id = productId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["CartError"] = "Điểm đánh giá phải từ 1 đến 5.";
                return RedirectToAction("Details", new { id = productId });
            }

            if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length > 500)
            {
                TempData["CartError"] = "Nội dung nhận xét không được để trống và tối đa 500 ký tự.";
                return RedirectToAction("Details", new { id = productId });
            }

            var existing = db.ProductReviews.FirstOrDefault(r => r.ProductID == productId && r.CustomerID == customerId);
            if (existing != null)
            {
                existing.Rating = rating;
                existing.Comment = comment.Trim();
                existing.CreatedAt = DateTime.Now;
                TempData["ReviewSuccess"] = "Đã cập nhật đánh giá sản phẩm của bạn thành công.";
            }
            else
            {
                db.ProductReviews.Add(new ProductReview
                {
                    ProductID = productId,
                    CustomerID = customerId,
                    Rating = rating,
                    Comment = comment.Trim(),
                    CreatedAt = DateTime.Now
                });
                TempData["ReviewSuccess"] = "Cảm ơn bạn đã gửi đánh giá cho sản phẩm!";
            }

            db.SaveChanges();
            return RedirectToAction("Details", new { id = productId });
        }

        // GET: /Products/Create
        [AdminAuthorize]
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            return View(new Product { IsActive = true, StockQuantity = 1 });
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult Create([Bind(Include = "ProductID,ProductName,CategoryID,Price,StockQuantity,Description,IsActive,ImageUrl")] Product product, HttpPostedFileBase ImageFile)
        {
            ValidateProduct(product);

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                string uploadError;
                string savedPath = SaveProductImage(ImageFile, out uploadError);
                if (uploadError != null)
                {
                    ModelState.AddModelError("", uploadError);
                }
                else
                {
                    product.ImageUrl = savedPath;
                }
            }

            if (ModelState.IsValid)
            {
                db.Products.Add(product);
                db.SaveChanges();
                TempData["ProductMsg"] = "Đã thêm sản phẩm \"" + product.ProductName + "\" thành công (tự động đồng bộ vào Kho hàng).";
                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: /Products/Edit/5
        [AdminAuthorize]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Product product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult Edit([Bind(Include = "ProductID,ProductName,CategoryID,Price,StockQuantity,Description,IsActive,ImageUrl")] Product product, HttpPostedFileBase ImageFile)
        {
            ValidateProduct(product);

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                string uploadError;
                string savedPath = SaveProductImage(ImageFile, out uploadError);
                if (uploadError != null)
                {
                    ModelState.AddModelError("", uploadError);
                }
                else
                {
                    product.ImageUrl = savedPath;
                }
            }

            if (ModelState.IsValid)
            {
                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
                TempData["ProductMsg"] = "Đã cập nhật sản phẩm \"" + product.ProductName + "\" thành công.";
                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: /Products/Delete/5
        [AdminAuthorize]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Product product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        // POST: /Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            bool hasOrderHistory = db.OrderDetails.Any(d => d.ProductID == id);
            bool hasReviews = db.ProductReviews.Any(r => r.ProductID == id);

            if (hasOrderHistory || hasReviews)
            {
                product.IsActive = false;
                db.SaveChanges();
                TempData["ProductMsg"] = "Sản phẩm \"" + product.ProductName + "\" đã phát sinh đơn hàng/đánh giá nên hệ thống tự động chuyển sang trạng thái Ngừng bán.";
            }
            else
            {
                db.Products.Remove(product);
                db.SaveChanges();
                TempData["ProductMsg"] = "Đã xóa vĩnh viễn sản phẩm \"" + product.ProductName + "\".";
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
