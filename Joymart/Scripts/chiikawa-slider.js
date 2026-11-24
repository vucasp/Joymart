// =========================================================
// ĐIỀU KHIỂN SLIDESHOW BANNER CHIIKAWA (CHUYỂN ẢNH TRỰC TIẾP)
// =========================================================
let slideIndex = 1;
let slideTimer = null;

function plusSlides(n) {
    showSlides(slideIndex += n);
    resetAutoSlide();
}

function currentSlide(n) {
    showSlides(slideIndex = n);
    resetAutoSlide();
}

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

function resetAutoSlide() {
    clearInterval(slideTimer);
    slideTimer = setInterval(function () {
        slideIndex++;
        showSlides(slideIndex);
    }, 3500);
}

document.addEventListener("DOMContentLoaded", function () {
    showSlides(slideIndex);
    resetAutoSlide();
});