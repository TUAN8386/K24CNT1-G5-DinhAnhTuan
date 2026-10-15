// Hỏi xác nhận trước các thao tác quan trọng: <form data-confirm="...">
document.addEventListener('submit', function (e) {
    var msg = e.target.getAttribute('data-confirm');
    if (msg && !window.confirm(msg)) e.preventDefault();
});

// Định dạng tiền VNĐ
window.formatVnd = function (n) {
    return Math.round(n).toLocaleString('vi-VN') + ' ₫';
};

// Máy tính trả góp (trang chi tiết sản phẩm và trang đặt hàng)
window.tinhTraGop = function (root) {
    if (!root) return;
    var gia = parseFloat(root.getAttribute('data-gia') || '0');
    var lai = parseFloat(root.getAttribute('data-lai') || '0');
    var pt = parseInt(root.querySelector('[data-tg="phantram"]').value || '30', 10);
    var ky = parseInt(root.querySelector('[data-tg="kyhan"]').value || '6', 10);
    var traTruoc = Math.round(gia * pt / 100);
    var vay = gia - traTruoc;
    var gop = Math.round(vay / ky + vay * lai / 100);
    root.querySelector('[data-tg="tratruoc"]').textContent = formatVnd(traTruoc);
    root.querySelector('[data-tg="gop"]').textContent = formatVnd(gop);
    root.querySelector('[data-tg="tong"]').textContent = formatVnd(traTruoc + gop * ky);
};

document.querySelectorAll('[data-tragop]').forEach(function (root) {
    root.querySelectorAll('select, input').forEach(function (el) {
        el.addEventListener('change', function () { tinhTraGop(root); });
    });
    tinhTraGop(root);
});

// Xem trước ảnh trước khi tải lên: <input type="file" data-preview="#idAnh">
document.querySelectorAll('input[type=file][data-preview]').forEach(function (input) {
    var img = document.querySelector(input.getAttribute('data-preview'));
    if (!img) return;
    var goc = img.getAttribute('src');
    input.addEventListener('change', function () {
        var f = input.files && input.files[0];
        img.src = f ? URL.createObjectURL(f) : goc;
    });
});

// Xem trước nhiều ảnh: <input type="file" multiple data-preview-list="#khung">
document.querySelectorAll('input[type=file][data-preview-list]').forEach(function (input) {
    var khung = document.querySelector(input.getAttribute('data-preview-list'));
    if (!khung) return;
    input.addEventListener('change', function () {
        khung.innerHTML = '';
        Array.prototype.forEach.call(input.files || [], function (f) {
            var img = document.createElement('img');
            img.src = URL.createObjectURL(f);
            img.className = 'thumb';
            img.style.width = '64px'; img.style.height = '64px'; img.style.border = '1px solid #dfe3ea';
            img.title = f.name;
            khung.appendChild(img);
        });
    });
});

// Thư viện ảnh trang chi tiết sản phẩm: bấm ảnh nhỏ để đổi ảnh lớn
document.querySelectorAll('.gallery-thumb').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var anh = document.getElementById('anhChinh');
        if (anh) anh.src = btn.getAttribute('data-anh');
        document.querySelectorAll('.gallery-thumb').forEach(function (b) { b.classList.remove('active'); });
        btn.classList.add('active');
    });
});

// Hiện tên file đã chọn bên cạnh nút "Chọn tệp...": <input type="file" data-file-name="#idSpan">
document.querySelectorAll('input[type=file][data-file-name]').forEach(function (input) {
    var nhan = document.querySelector(input.getAttribute('data-file-name'));
    if (!nhan) return;
    var goc = nhan.textContent;
    input.addEventListener('change', function () {
        var f = input.files && input.files[0];
        nhan.textContent = f ? f.name : goc;
    });
});

// Khung báo lỗi chung của form (ModelState): hiện khi kiểm tra phía client phát hiện trường sai
document.querySelectorAll('[data-loi-nhap-lieu]').forEach(function (box) {
    var form = box.closest('form');
    if (!form) return;
    form.addEventListener('submit', function () {
        setTimeout(function () {
            box.classList.toggle('d-none', !form.querySelector('.input-validation-error'));
        }, 0);
    }, true);
});
