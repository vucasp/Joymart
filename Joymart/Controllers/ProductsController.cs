using Joymart.Filters;
using Joymart.Models;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System;

namespace Joymart.Controllers
{
    public class ProductsController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // Hàm lưu ảnh vào Content/images, tự thêm -1, -2 nếu trùng tên file
        private string SaveProductImage(HttpPostedFileBase imageFile, out string errorMessage)
        {
            errorMessage = null;

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            string ext = Path.GetExtension(imageFile.FileName)?.ToLower();

            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                errorMessage = "Chỉ chấp nhận file ảnh định dạng .jpg, .jpeg, .png";
                return null;
            }

            // Kiểm tra dung lượng tối đa 2MB
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

            // Đặt tên file duy nhất để tránh ghi đè ảnh trùng tên
            string fileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ext;
            string fullPath = Path.Combine(folderPath, fileName);
            try
            {
                imageFile.SaveAs(fullPath);
                // Trả về đường dẫn tương đối tính từ ~/Content/images/
                return "products/" + fileName;
            }
            catch (Exception ex)
            {
                errorMessage = "Có lỗi xảy ra khi lưu file ảnh: " + ex.Message;
                return null;
            }
        }

        // GET: Products
        [AdminAuthorize]
        public ActionResult Index()
        {
            var product = db.Products.Include(p => p.Category);
            return View(product.ToList());
        }

        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Product product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        // GET: Products/Create
        [AdminAuthorize]
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult Create([Bind(Include = "ProductID,ProductName,CategoryID,Price,StockQuantity,Description,IsActive,ImageUrl")] Product product, HttpPostedFileBase ImageFile)
        {
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
                TempData["ProductMsg"] = "Đã thêm sản phẩm \"" + product.ProductName + "\" thành công.";
                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: Products/Edit
        [AdminAuthorize]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Product product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult Edit([Bind(Include = "ProductID,ProductName,CategoryID,Price,StockQuantity,Description,IsActive,ImageUrl")] Product product, HttpPostedFileBase ImageFile)
        {
            // Nếu admin chọn ảnh mới thì lưu ảnh mới và cập nhật ImageUrl
            // Nếu không chọn ảnh mới, @Html.HiddenFor(m => m.ImageUrl) trên View sẽ tự giữ nguyên ảnh cũ
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

        // GET: Products/Delete/5
        [AdminAuthorize]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Product product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id); // Lưu ý: đổi thành db.Product.Find(id) nếu DbSet của bạn đặt tên số ít
            if (product == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra sản phẩm đã từng phát sinh trong đơn hàng chưa
            bool hasOrderHistory = db.OrderDetails.Any(d => d.ProductID == id);
            if (hasOrderHistory)
            {
                // Chuyển sang ngừng kinh doanh thay vì xóa cứng
                product.IsActive = false;
                db.SaveChanges();
                TempData["ProductMsg"] = "Sản phẩm \"" + product.ProductName + "\" đã có trong lịch sử đơn hàng nên hệ thống tự động chuyển sang trạng thái Ngừng bán.";
            }
            else
            {
                db.Products.Remove(product); // Đổi thành db.Product.Remove(product) nếu DbSet số ít
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