function closeBeveraToast() {
    const toast = document.getElementById('bevera-toast');
    const container = document.getElementById('toast-container');
    if (!toast) return;
    toast.classList.add('bevera-toast--hide');
    window.setTimeout(() => {
        if (container) container.remove();
    }, 300);
}
document.addEventListener('DOMContentLoaded', function () {
    const toast = document.getElementById('bevera-toast');
    if (!toast) return;
    window.setTimeout(closeBeveraToast, 4500);
});
