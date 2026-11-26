using Joymart.Filters;
using Joymart.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    // =====================================================================
    // CONTROLLER QUẢN LÝ DANH MỤC SẢN PHẨM (CategoriesController)
    // Bao gồm:
    //   - Index: Hiển thị thống kê danh mục, tìm kiếm theo tên và đếm số sản phẩm
    //   - Create / Edit: Kiểm tra trùng tên danh mục không phân biệt hoa thường
    //   - Details: Xem thông tin danh mục kèm danh sách sản phẩm trực thuộc
    //   - Delete: Ngăn chặn xóa danh mục đang chứa sản phẩm để bảo toàn khóa ngoại
    // =====================================================================
    [AdminAuthorize]
    public class CategoriesController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // GET: /Categories
        public ActionResult Index(string keyword)
        {
            var allCategories = db.Categories.Include(c => c.Products).ToList();

            ViewBag.TotalCategories = allCategories.Count;
            ViewBag.TotalAssignedProducts = allCategories.Sum(c => c.Products.Count);
            ViewBag.EmptyCategories = allCategories.Count(c => !c.Products.Any());
            ViewBag.Keyword = keyword;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                allCategories = allCategories
                    .Where(c => c.CategoryName.ToLower().Contains(kw))
                    .ToList();
            }

            return View(allCategories.OrderBy(c => c.CategoryID).ToList());
        }

        // GET: /Categories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Category category = db.Categories
                .Include(c => c.Products)
                .FirstOrDefault(c => c.CategoryID == id);

            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }

        // GET: /Categories/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CategoryID,CategoryName")] Category category)
        {
            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                ModelState.AddModelError("CategoryName", "Vui lòng nhập tên danh mục.");
            }
            else
            {
                string name = category.CategoryName.Trim();
                if (name.Length > 100)
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục không được vượt quá 100 ký tự.");
                }
                else if (db.Categories.Any(c => c.CategoryName.ToLower() == name.ToLower()))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại trên hệ thống.");
                }
            }

            if (ModelState.IsValid)
            {
                category.CategoryName = category.CategoryName.Trim();
                db.Categories.Add(category);
                db.SaveChanges();
                TempData["CategorySuccess"] = "Đã thêm danh mục \"" + category.CategoryName + "\" thành công.";
                return RedirectToAction("Index");
            }

            return View(category);
        }

        // GET: /Categories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }

        // POST: /Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CategoryID,CategoryName")] Category category)
        {
            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                ModelState.AddModelError("CategoryName", "Vui lòng nhập tên danh mục.");
            }
            else
            {
                string name = category.CategoryName.Trim();
                if (name.Length > 100)
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục không được vượt quá 100 ký tự.");
                }
                else if (db.Categories.Any(c => c.CategoryID != category.CategoryID && c.CategoryName.ToLower() == name.ToLower()))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại trên hệ thống.");
                }
            }

            if (ModelState.IsValid)
            {
                category.CategoryName = category.CategoryName.Trim();
                db.Entry(category).State = EntityState.Modified;
                db.SaveChanges();
                TempData["CategorySuccess"] = "Đã cập nhật tên danh mục thành \"" + category.CategoryName + "\".";
                return RedirectToAction("Index");
            }

            return View(category);
        }

        // GET: /Categories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Category category = db.Categories
                .Include(c => c.Products)
                .FirstOrDefault(c => c.CategoryID == id);

            if (category == null)
            {
                return HttpNotFound();
            }

            ViewBag.HasProducts = category.Products.Any();
            return View(category);
        }

        // POST: /Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }

            if (db.Products.Any(p => p.CategoryID == id))
            {
                TempData["CategoryError"] = "Không thể xóa danh mục \"" + category.CategoryName + "\" vì vẫn còn sản phẩm thuộc danh mục này.";
                return RedirectToAction("Index");
            }

            db.Categories.Remove(category);
            db.SaveChanges();
            TempData["CategorySuccess"] = "Đã xóa danh mục \"" + category.CategoryName + "\" thành công.";
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
