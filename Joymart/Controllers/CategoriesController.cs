using Joymart.Filters;
using Joymart.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    [AdminAuthorize]
    public class CategoriesController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // GET: Categories
        public ActionResult Index()
        {
            return View(db.Categories.ToList());
        }

        // GET: Categories/Details/5
        public ActionResult Details(int? id)
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

        // GET: Categories/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
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
                if (db.Categories.Any(c => c.CategoryName.ToLower() == name.ToLower()))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại.");
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

        // GET: Categories/Edit/5
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

        // POST: Categories/Edit/5
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
                if (db.Categories.Any(c => c.CategoryID != category.CategoryID && c.CategoryName.ToLower() == name.ToLower()))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại.");
                }
            }

            if (ModelState.IsValid)
            {
                category.CategoryName = category.CategoryName.Trim();
                db.Entry(category).State = EntityState.Modified;
                db.SaveChanges();
                TempData["CategorySuccess"] = "Đã cập nhật danh mục thành công.";
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // GET: Categories/Delete/5
        public ActionResult Delete(int? id)
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
            ViewBag.HasProducts = db.Products.Any(p => p.CategoryID == id);
            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra xem có sản phẩm thuộc danh mục này không
            if (db.Products.Any(p => p.CategoryID == id))
            {
                TempData["CategoryError"] = "Không thể xóa danh mục \"" + category.CategoryName + "\" vì đang có sản phẩm thuộc danh mục này.";
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
