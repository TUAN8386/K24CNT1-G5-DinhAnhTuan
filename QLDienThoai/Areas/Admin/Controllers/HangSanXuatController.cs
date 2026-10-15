using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Services;

namespace QLDienThoai.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class HangSanXuatController : Controller
{
    private readonly AppDbContext _db;
    private readonly UploadService _upload;

    public HangSanXuatController(AppDbContext db, UploadService upload)
    {
        _db = db;
        _upload = upload;
    }

    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewBag.Q = q;
        var query = _db.HangSanXuats.AsNoTracking().Include(h => h.SanPhams).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(h => h.TenHang.Contains(q) || (h.QuocGia != null && h.QuocGia.Contains(q)));
        return View(await query.OrderBy(h => h.TenHang).ToPagedAsync(page, 10));
    }

    public async Task<IActionResult> Details(int id)
    {
        var h = await _db.HangSanXuats.AsNoTracking().Include(x => x.SanPhams).FirstOrDefaultAsync(x => x.MaHang == id);
        return h == null ? NotFound() : View(h);
    }

    public IActionResult Create() => View(new HangSanXuat());

    [HttpPost]
    public async Task<IActionResult> Create(HangSanXuat model, IFormFile? fileLogo)
    {
        ModelState.Remove(nameof(HangSanXuat.Logo));
        if (await _db.HangSanXuats.AnyAsync(h => h.TenHang == model.TenHang))
            ModelState.AddModelError(nameof(model.TenHang), "Tên hãng đã tồn tại");
        if (fileLogo != null)
        {
            var loi = _upload.KiemTra(fileLogo);
            if (loi != null) ModelState.AddModelError("fileLogo", loi);
        }
        if (!ModelState.IsValid) return View(model);

        if (fileLogo != null) model.Logo = await _upload.LuuAsync(fileLogo, "hang");
        _db.HangSanXuats.Add(model);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã thêm hãng " + model.TenHang;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var h = await _db.HangSanXuats.FindAsync(id);
        return h == null ? NotFound() : View(h);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, HangSanXuat model, IFormFile? fileLogo)
    {
        if (id != model.MaHang) return BadRequest();
        var h = await _db.HangSanXuats.FindAsync(id);
        if (h == null) return NotFound();

        if (await _db.HangSanXuats.AnyAsync(x => x.TenHang == model.TenHang && x.MaHang != id))
            ModelState.AddModelError(nameof(model.TenHang), "Tên hãng đã tồn tại");
        if (fileLogo != null)
        {
            var loi = _upload.KiemTra(fileLogo);
            if (loi != null) ModelState.AddModelError("fileLogo", loi);
        }
        if (!ModelState.IsValid)
        {
            model.Logo = h.Logo;
            return View(model);
        }

        h.TenHang = model.TenHang;
        h.QuocGia = model.QuocGia;
        if (fileLogo != null)
        {
            _upload.Xoa(h.Logo);
            h.Logo = await _upload.LuuAsync(fileLogo, "hang");
        }
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã cập nhật hãng " + h.TenHang;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var h = await _db.HangSanXuats.AsNoTracking().Include(x => x.SanPhams).FirstOrDefaultAsync(x => x.MaHang == id);
        return h == null ? NotFound() : View(h);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var h = await _db.HangSanXuats.FindAsync(id);
        if (h == null) return NotFound();
        // Ràng buộc: không xóa hãng còn sản phẩm
        if (await _db.SanPhams.AnyAsync(s => s.MaHang == id))
        {
            TempData["Loi"] = $"Không thể xóa hãng {h.TenHang} vì vẫn còn sản phẩm thuộc hãng này";
            return RedirectToAction(nameof(Index));
        }
        _upload.Xoa(h.Logo);
        _db.HangSanXuats.Remove(h);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã xóa hãng " + h.TenHang;
        return RedirectToAction(nameof(Index));
    }
}
