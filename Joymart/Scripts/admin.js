/* =====================================================================
   FILE: Scripts/admin.js
   MỤC ĐÍCH:
     Các hàm JavaScript dùng chung cho giao diện Portal Quản trị viên (Admin).
   CÁC HÀM CHÍNH:
     - toggleAdminSidebar(): Đóng/mở thanh menu dọc bên trái khi xem trên màn hình nhỏ.
     - previewProductImage(input, imgElementId): Đọc file ảnh từ máy tính bằng FileReader
       để hiển thị ảnh xem trước ngay lập tức trước khi bấm Lưu sản phẩm.
   ===================================================================== */

// Hàm bật/tắt Sidebar quản trị trên điện thoại & máy tính bảng
function toggleAdminSidebar() {
    var sb = document.getElementById("adminSidebar");
    var bd = document.getElementById("adminBackdrop");
    if (sb && bd) {
        sb.classList.toggle("open");
        bd.classList.toggle("open");
    }
}

// Hàm xem trước ảnh sản phẩm khi Admin chọn file từ máy tính (dùng ở Create & Edit Product)
function previewProductImage(input, previewElementId) {
    var targetId = previewElementId || "previewImg";
    var preview = document.getElementById(targetId);
    if (!preview) return;

    if (input && input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            preview.src = e.target.result;
            preview.style.display = "block";
        };
        reader.readAsDataURL(input.files[0]);
    }
}
