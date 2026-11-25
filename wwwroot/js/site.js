// The one moment of motion: forecast water levels rise when the page loads.
document.addEventListener("DOMContentLoaded", function () {
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    var fills = document.querySelectorAll(".level-fill");
    fills.forEach(function (f) { f.style.height = "0"; });
    requestAnimationFrame(function () {
        requestAnimationFrame(function () { fills.forEach(function (f) { f.style.height = ""; }); });
    });
});
