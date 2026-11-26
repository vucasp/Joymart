/* =====================================================================
   FILE: Scripts/store-product.js
   MỤC ĐÍCH:
     Xử lý tương tác giao diện phía khách hàng tại trang Chi tiết sản phẩm.
   CÁC HÀM CHÍNH:
     - changeDetailQty(delta): Cộng/trừ số lượng sản phẩm muốn mua nhưng không
       vượt quá số lượng tồn kho tối đa (input.max) và không nhỏ hơn 1.
     - selectRatingPill(score, btnElement): Cập nhật giá trị điểm đánh giá (1..5)
       vào input ẩn khi người mua bấm chọn mức điểm đánh giá sản phẩm.
   ===================================================================== */

// Hàm tăng/giảm số lượng sản phẩm cần mua trên trang Products/Details
function changeDetailQty(delta) {
    var input = document.getElementById("detailQty");
    if (!input || input.readOnly) return;

    var current = parseInt(input.value, 10) || 1;
    var max = parseInt(input.max, 10) || 1;
    var next = current + delta;

    if (next < 1) next = 1;
    if (next > max) next = max;
    input.value = next;
}

// Hàm chọn mức điểm đánh giá (1 đến 5 điểm) trên form Đánh giá sản phẩm
function selectRatingPill(score, btnElement) {
    var ratingInput = document.getElementById("ratingValueInput");
    if (ratingInput) {
        ratingInput.value = score;
    }

    var pills = document.querySelectorAll(".rating-pill");
    for (var i = 0; i < pills.length; i++) {
        pills[i].classList.remove("active");
    }

    if (btnElement) {
        btnElement.classList.add("active");
    }
}
