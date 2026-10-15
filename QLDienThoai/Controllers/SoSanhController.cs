using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Services;

namespace QLDienThoai.Controllers;

public class SoSanhController : Controller
{
    private readonly AppDbContext _db;
    private readonly SoSanhService _soSanh;

    public SoSanhController(AppDbContext db, SoSanhService soSanh)
    {
        _db = db;
        _soSanh = soSanh;
    }

    public async Task<IActionResult> Index()
    {
        var ids = _soSanh.LayDanhSach();
        var sps = await _db.SanPhams.AsNoTracking()
            .Include(s => s.HangSanXuat)
            .Include(s => s.PhienBans)
            .Include(s => s.DanhGias)
            .Where(s => ids.Contains(s.MaSP)).ToListAsync();
        // giữ đúng thứ tự người dùng thêm
        return View(ids.Select(id => sps.FirstOrDefault(s => s.MaSP == id)).Where(s => s != null).Select(s => s!).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Them(int id, string? returnUrl)
    {
        if (!await _db.SanPhams.AnyAsync(s => s.MaSP == id)) return NotFound();
        var loi = _soSanh.Them(id);
        TempData[loi == null ? "ThongBao" : "Loi"] = loi ?? "Đã thêm vào danh sách so sánh";
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Xoa(int id)
    {
        _soSanh.Xoa(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult XoaHet()
    {
        _soSanh.XoaHet();
        return RedirectToAction(nameof(Index));
    }
}
