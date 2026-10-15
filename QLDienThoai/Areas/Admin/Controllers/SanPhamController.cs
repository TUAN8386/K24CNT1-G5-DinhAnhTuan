using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Services;

namespace QLDienThoai.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class SanPhamController : Controller
{
    /// <summary>Số ảnh phụ tối đa của một sản phẩm.</summary>
    public const int SoAnhPhuToiDa = 10;

    private readonly AppDbContext _db;
    private readonly UploadService _upload;
    private readonly HtmlCleaner _cleaner;

    public SanPhamController(AppDbContext db, UploadService upload, HtmlCleaner cleaner)
    {
        _db = db;
        _upload = upload;
        _cleaner = cleaner;
    }

    private async Task NapHangAsync(int? chon = null) =>
        ViewBag.MaHang = new SelectList(await _db.HangSanXuats.OrderBy(h => h.TenHang).ToListAsync(), "MaHang", "TenHang", chon);

    /// <summary>Kiểm tra ảnh đại diện và các ảnh phụ, ghi lỗi vào ModelState.</summary>
    private void KiemTraAnh(IFormFile? fileAnh, List<IFormFile>? fileAnhPhu, int soAnhPhuHienCo)
    {
        if (fileAnh != null)
        {
            var loi = _upload.KiemTra(fileAnh);
            if (loi != null) ModelState.AddModelError("fileAnh", loi);
        }
        if (fileAnhPhu == null || fileAnhPhu.Count == 0) return;
        if (soAnhPhuHienCo + fileAnhPhu.Count > SoAnhPhuToiDa)
            ModelState.AddModelError("fileAnhPhu", $"Mỗi sản phẩm tối đa {SoAnhPhuToiDa} ảnh phụ (hiện có {soAnhPhuHienCo})");
        foreach (var f in fileAnhPhu)
        {
            var loi = _upload.KiemTra(f);
            if (loi != null) { ModelState.AddModelError("fileAnhPhu", $"{f.FileName}: {loi}"); break; }
        }
    }

    private async Task LuuAnhPhuAsync(SanPham sp, List<IFormFile>? fileAnhPhu)
    {
        if (fileAnhPhu == null) return;
        var thuTu = sp.HinhAnhs.Count == 0 ? 0 : sp.HinhAnhs.Max(h => h.ThuTu);
        foreach (var f in fileAnhPhu)
            sp.HinhAnhs.Add(new HinhAnhSanPham { DuongDan = await _upload.LuuAsync(f, "sanpham"), ThuTu = ++thuTu });
    }

    public async Task<IActionResult> Index(string? q, int? hang, int page = 1)
    {
        ViewBag.Q = q;
        ViewBag.Hang = hang;
        await NapHangAsync(hang);
        var query = _db.SanPhams.AsNoTracking().Include(s => s.HangSanXuat).Include(s => s.PhienBans).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(s => s.TenSP.Contains(q));
        if (hang.HasValue) query = query.Where(s => s.MaHang == hang);
        return View(await query.OrderByDescending(s => s.NgayTao).ToPagedAsync(page, 10));
    }

    public async Task<IActionResult> Details(int id)
    {
        var sp = await _db.SanPhams.AsNoTracking().Include(s => s.HangSanXuat).Include(s => s.PhienBans)
            .Include(s => s.HinhAnhs)
            .FirstOrDefaultAsync(s => s.MaSP == id);
        return sp == null ? NotFound() : View(sp);
    }

    public async Task<IActionResult> Create()
    {
        await NapHangAsync();
        return View(new SanPham());
    }

    [HttpPost]
    public async Task<IActionResult> Create(SanPham model, IFormFile? fileAnh, List<IFormFile>? fileAnhPhu)
    {
        KiemTraAnh(fileAnh, fileAnhPhu, 0);
        if (!await _db.HangSanXuats.AnyAsync(h => h.MaHang == model.MaHang))
            ModelState.AddModelError(nameof(model.MaHang), "Hãng không tồn tại");
        if (!ModelState.IsValid)
        {
            await NapHangAsync(model.MaHang);
            return View(model);
        }

        model.MoTa = _cleaner.Lam(model.MoTa);
        model.NgayTao = DateTime.Now;
        model.AnhDaiDien = fileAnh != null ? await _upload.LuuAsync(fileAnh, "sanpham") : null;
        model.HinhAnhs = new List<HinhAnhSanPham>();
        await LuuAnhPhuAsync(model, fileAnhPhu);
        _db.SanPhams.Add(model);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã thêm sản phẩm. Hãy thêm phiên bản (dung lượng/màu) cho sản phẩm.";
        return RedirectToAction("Create", "PhienBan", new { maSP = model.MaSP });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var sp = await _db.SanPhams.Include(s => s.HinhAnhs).FirstOrDefaultAsync(s => s.MaSP == id);
        if (sp == null) return NotFound();
        await NapHangAsync(sp.MaHang);
        return View(sp);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, SanPham model, IFormFile? fileAnh, List<IFormFile>? fileAnhPhu, bool xoaAnhDaiDien = false)
    {
        if (id != model.MaSP) return BadRequest();
        var sp = await _db.SanPhams.Include(s => s.HinhAnhs).FirstOrDefaultAsync(s => s.MaSP == id);
        if (sp == null) return NotFound();

        KiemTraAnh(fileAnh, fileAnhPhu, sp.HinhAnhs.Count);
        if (!ModelState.IsValid)
        {
            model.AnhDaiDien = sp.AnhDaiDien;
            model.HinhAnhs = sp.HinhAnhs;
            await NapHangAsync(model.MaHang);
            return View(model);
        }

        sp.TenSP = model.TenSP;
        sp.MaHang = model.MaHang;
        sp.ManHinh = model.ManHinh;
        sp.Chip = model.Chip;
        sp.Camera = model.Camera;
        sp.Pin = model.Pin;
        sp.HeDieuHanh = model.HeDieuHanh;
        sp.MoTa = _cleaner.Lam(model.MoTa);

        string? anhCanXoa = null;
        if (fileAnh != null)
        {
            anhCanXoa = sp.AnhDaiDien;                       // thay ảnh mới
            sp.AnhDaiDien = await _upload.LuuAsync(fileAnh, "sanpham");
        }
        else if (xoaAnhDaiDien)
        {
            anhCanXoa = sp.AnhDaiDien;                       // bỏ ảnh, hiển thị ảnh mặc định
            sp.AnhDaiDien = null;
        }
        await LuuAnhPhuAsync(sp, fileAnhPhu);
        await _db.SaveChangesAsync();
        _upload.Xoa(anhCanXoa);                              // chỉ xóa file trong /uploads/

        TempData["ThongBao"] = "Đã cập nhật sản phẩm " + sp.TenSP;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Nhận ảnh chèn vào phần mô tả từ CKEditor 5 (nút "Ảnh").
    /// Kiểm tra đuôi/kích thước/chữ ký file như ảnh đại diện; token CSRF gửi qua header RequestVerificationToken.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(UploadService.KichThuocToiDa + 64 * 1024)]
    public async Task<IActionResult> UploadAnhMoTa(IFormFile? upload)
    {
        var loi = _upload.KiemTra(upload);
        if (loi != null) return BadRequest(new { error = new { message = loi } });
        var url = await _upload.LuuAsync(upload!, "mota");
        return Json(new { url });
    }

    /// <summary>Xóa một ảnh trong thư viện ảnh.</summary>
    [HttpPost]
    public async Task<IActionResult> XoaAnhPhu(int id)
    {
        var ha = await _db.HinhAnhSanPhams.FindAsync(id);
        if (ha == null) return NotFound();
        _db.HinhAnhSanPhams.Remove(ha);
        await _db.SaveChangesAsync();
        _upload.Xoa(ha.DuongDan);
        TempData["ThongBao"] = "Đã xóa ảnh khỏi thư viện";
        return RedirectToAction(nameof(Edit), new { id = ha.MaSP });
    }

    /// <summary>Đổi một ảnh phụ thành ảnh đại diện; ảnh đại diện cũ chuyển vào thư viện.</summary>
    [HttpPost]
    public async Task<IActionResult> DatAnhChinh(int id)
    {
        var ha = await _db.HinhAnhSanPhams.Include(h => h.SanPham).FirstOrDefaultAsync(h => h.MaHA == id);
        if (ha?.SanPham == null) return NotFound();
        var sp = ha.SanPham;
        var cu = sp.AnhDaiDien;
        sp.AnhDaiDien = ha.DuongDan;
        if (string.IsNullOrEmpty(cu)) _db.HinhAnhSanPhams.Remove(ha);
        else ha.DuongDan = cu;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã đổi ảnh đại diện";
        return RedirectToAction(nameof(Edit), new { id = sp.MaSP });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var sp = await _db.SanPhams.AsNoTracking().Include(s => s.HangSanXuat).Include(s => s.PhienBans)
            .FirstOrDefaultAsync(s => s.MaSP == id);
        if (sp == null) return NotFound();
        ViewBag.DaBan = await _db.ChiTietDonHangs.AnyAsync(c => c.PhienBan!.MaSP == id);
        return View(sp);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sp = await _db.SanPhams.Include(s => s.PhienBans).Include(s => s.HinhAnhs)
            .FirstOrDefaultAsync(s => s.MaSP == id);
        if (sp == null) return NotFound();
        // Ràng buộc: không xóa sản phẩm đã phát sinh đơn hàng
        if (await _db.ChiTietDonHangs.AnyAsync(c => c.PhienBan!.MaSP == id))
        {
            TempData["Loi"] = $"Không thể xóa {sp.TenSP} vì đã có trong đơn hàng";
            return RedirectToAction(nameof(Index));
        }
        var files = sp.HinhAnhs.Select(h => h.DuongDan).Append(sp.AnhDaiDien).ToList();
        _db.PhienBans.RemoveRange(sp.PhienBans);
        _db.SanPhams.Remove(sp);                             // ảnh phụ xóa theo (cascade)
        await _db.SaveChangesAsync();
        foreach (var f in files) _upload.Xoa(f);
        TempData["ThongBao"] = "Đã xóa sản phẩm " + sp.TenSP;
        return RedirectToAction(nameof(Index));
    }
}
