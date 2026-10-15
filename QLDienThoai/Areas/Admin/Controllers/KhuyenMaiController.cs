using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Services;

namespace QLDienThoai.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class KhuyenMaiController : Controller
{
    private readonly AppDbContext _db;
    private readonly UploadService _upload;
    private readonly HtmlCleaner _cleaner;

    public KhuyenMaiController(AppDbContext db, UploadService upload, HtmlCleaner cleaner)
    {
        _db = db;
        _upload = upload;
        _cleaner = cleaner;
    }

    public async Task<IActionResult> Index(int page = 1) =>
        View(await _db.KhuyenMais.AsNoTracking().OrderByDescending(k => k.NgayBatDau).ToPagedAsync(page, 10));

    public async Task<IActionResult> Details(int id)
    {
        var km = await _db.KhuyenMais.FindAsync(id);
        return km == null ? NotFound() : View(km);
    }

    public IActionResult Create() => View(new KhuyenMai());

    [HttpPost]
    public async Task<IActionResult> Create(KhuyenMai model, IFormFile? fileAnh)
    {
        if (fileAnh != null && _upload.KiemTra(fileAnh) is { } loi) ModelState.AddModelError("fileAnh", loi);
        if (!ModelState.IsValid) return View(model);

        model.NoiDung = _cleaner.Lam(model.NoiDung);
        if (fileAnh != null) model.AnhBia = await _upload.LuuAsync(fileAnh, "khuyenmai");
        _db.KhuyenMais.Add(model);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã đăng tin khuyến mãi";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var km = await _db.KhuyenMais.FindAsync(id);
        return km == null ? NotFound() : View(km);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, KhuyenMai model, IFormFile? fileAnh)
    {
        if (id != model.MaKM) return BadRequest();
        var km = await _db.KhuyenMais.FindAsync(id);
        if (km == null) return NotFound();
        if (fileAnh != null && _upload.KiemTra(fileAnh) is { } loi) ModelState.AddModelError("fileAnh", loi);
        if (!ModelState.IsValid)
        {
            model.AnhBia = km.AnhBia;
            return View(model);
        }

        km.TieuDe = model.TieuDe;
        km.NoiDung = _cleaner.Lam(model.NoiDung);
        km.NgayBatDau = model.NgayBatDau;
        km.NgayKetThuc = model.NgayKetThuc;
        km.HienThi = model.HienThi;
        if (fileAnh != null)
        {
            _upload.Xoa(km.AnhBia);
            km.AnhBia = await _upload.LuuAsync(fileAnh, "khuyenmai");
        }
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã cập nhật tin khuyến mãi";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var km = await _db.KhuyenMais.FindAsync(id);
        return km == null ? NotFound() : View(km);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var km = await _db.KhuyenMais.FindAsync(id);
        if (km == null) return NotFound();
        _upload.Xoa(km.AnhBia);
        _db.KhuyenMais.Remove(km);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã xóa tin khuyến mãi";
        return RedirectToAction(nameof(Index));
    }
}
