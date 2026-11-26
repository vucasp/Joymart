using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Joymart.Models
{
    // =====================================================================
    // TẬP HỢP CÁC VIEWMODEL PHỤC VỤ KHU VỰC QUẢN TRỊ VIÊN (ADMIN PORTAL)
    // =====================================================================

    // Dòng thống kê Top sản phẩm bán chạy trên Dashboard
    public class TopSellingProductItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string ImageUrl { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int StockQuantity { get; set; }
    }

    // Dòng thống kê tỷ lệ đơn hàng theo trạng thái (vẽ biểu đồ cột CSS)
    public class OrderStatusStatItem
    {
        public string StatusName { get; set; }
        public int OrderCount { get; set; }
        public int Percentage { get; set; }
    }

    // ViewModel tổng quan cho trang Dashboard Admin (/Admin/Index)
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalStockUnits { get; set; }
        public int LowStockCount { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveringOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalUsers { get; set; }
        public int AdminCount { get; set; }
        public int CustomerCount { get; set; }

        public List<OrderStatusStatItem> StatusBreakdown { get; set; }
        public List<TopSellingProductItem> TopSellingProducts { get; set; }
        public List<Product> LowStockProducts { get; set; }
        public List<OrderPro> RecentOrders { get; set; }
    }

    // ViewModel bộ lọc danh sách sản phẩm trong trang Admin (/Products/Index)
    public class AdminProductListViewModel
    {
        public string Keyword { get; set; }
        public int? CategoryId { get; set; }
        public string StatusFilter { get; set; }
        public List<Category> Categories { get; set; }
        public List<Product> Products { get; set; }
    }

    // ViewModel cho trang Quản lý kho hàng (/Inventory/Index)
    public class InventoryManagementViewModel
    {
        public string Keyword { get; set; }
        public int? CategoryId { get; set; }
        public string StockStatus { get; set; }
        public int TotalSku { get; set; }
        public int TotalUnitsInStock { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public List<Category> Categories { get; set; }
        public List<Product> Products { get; set; }
    }

    // ViewModel bộ lọc cho trang Quản lý đơn hàng (/Orders/Index)
    public class AdminOrderFilterViewModel
    {
        public string Keyword { get; set; }
        public string StatusFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int DeliveringCount { get; set; }
        public List<OrderPro> Orders { get; set; }
    }

    // ViewModel cho trang Xuất hàng / Giao hàng (/Orders/Dispatch)
    public class OrderDispatchViewModel
    {
        public OrderPro Order { get; set; }
        public decimal ItemsSubTotal { get; set; }
        public DateTime SelectedShippedDate { get; set; }
    }

    // Dòng hiển thị tài khoản trong trang Quản lý tài khoản (/Users/Index)
    public class UserListItemViewModel
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }

    // ViewModel danh sách & bộ lọc tài khoản
    public class UserManagementViewModel
    {
        public string Keyword { get; set; }
        public string RoleFilter { get; set; }
        public int CurrentUserId { get; set; }
        public List<UserListItemViewModel> Users { get; set; }
    }

    // ViewModel tạo tài khoản mới từ trang Quản trị (/Users/Create)
    public class AdminCreateUserViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(20, ErrorMessage = "Tên đăng nhập không được vượt quá 20 ký tự.")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn quyền tài khoản.")]
        [Display(Name = "Phân quyền")]
        public string RoleName { get; set; }

        [StringLength(20, ErrorMessage = "Họ tên không được vượt quá 20 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Email phải có đuôi @gmail.com.")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
