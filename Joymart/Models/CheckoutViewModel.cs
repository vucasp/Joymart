using System.ComponentModel.DataAnnotations;

namespace Joymart.Models
{
    // ViewModel thu thập thông tin người nhận khi Đặt hàng (/ShoppingCart/Checkout)
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
        [StringLength(20, ErrorMessage = "Họ tên người nhận không được vượt quá 20 ký tự.")]
        [Display(Name = "Họ tên người nhận")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại giao hàng.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Email phải có đuôi @gmail.com (ví dụ: example@gmail.com).")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(250)]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        public Cart Cart { get; set; }
    }

    // ViewModel phục vụ chức năng Tra cứu đơn hàng dành cho Khách vãng lai (/ShoppingCart/TrackOrder)
    public class TrackOrderViewModel
    {
        public int? OrderID { get; set; }
        public string Phone { get; set; }
        public bool Searched { get; set; }
        public string ErrorMessage { get; set; }
        public OrderPro FoundOrder { get; set; }
    }
}
