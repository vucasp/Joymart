/* =====================================================================
   FILE: Scripts/chiikawa-slider.js
   MỤC ĐÍCH:
     Điều khiển hiệu ứng chuyển ảnh Slideshow Banner trên Trang chủ (Store/Index).
   CÁC HÀM CHÍNH:
     - plusSlides(n): Chuyển tiến (+1) hoặc lùi (-1) banner khi bấm nút điều hướng.
     - currentSlide(n): Nhảy trực tiếp đến banner thứ n khi bấm chấm tròn.
     - showSlides(n): Bật class 'active-slide' cho ảnh hiện tại và ẩn các ảnh còn lại.
     - resetAutoSlide(): Đặt lại bộ đếm tự động chuyển ảnh sau mỗi 3.5 giây.
   ===================================================================== */

let slideIndex = 1;
let slideTimer = null;

// Hàm chuyển sang slide kế tiếp hoặc trước đó
function plusSlides(n) {
    showSlides(slideIndex += n);
    resetAutoSlide();
}

// Hàm chọn trực tiếp slide thứ n từ chấm tròn bên dưới
function currentSlide(n) {
    showSlides(slideIndex = n);
    resetAutoSlide();
}

// Hàm hiển thị đúng slide theo chỉ số slideIndex
function showSlides(n) {
    const slides = document.getElementsByClassName("mySlides");
    const dots = document.getElementsByClassName("dot");

    if (slides.length === 0) return;

    if (n > slides.length) {
        slideIndex = 1;
    }
    if (n < 1) {
        slideIndex = slides.length;
    }

    for (let i = 0; i < slides.length; i++) {
        slides[i].classList.remove("active-slide");
    }

    for (let i = 0; i < dots.length; i++) {
        dots[i].classList.remove("active");
    }

    slides[slideIndex - 1].classList.add("active-slide");
    if (dots.length >= slideIndex) {
        dots[slideIndex - 1].classList.add("active");
    }
}

// Hàm khởi động lại chu kỳ tự động chuyển slide sau 3500ms (3.5 giây)
function resetAutoSlide() {
    clearInterval(slideTimer);
    slideTimer = setInterval(function () {
        slideIndex++;
        showSlides(slideIndex);
    }, 3500);
}

// Tự động chạy ngay khi cây DOM tải xong
document.addEventListener("DOMContentLoaded", function () {
    showSlides(slideIndex);
    resetAutoSlide();
});
