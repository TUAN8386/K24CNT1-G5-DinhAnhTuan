using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;

namespace QLDienThoai.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = VaiTro.QuanLy)]
public class PhienBanController : Controller
{
    private readonly AppDbContext _db;

    public PhienBanController(AppDbContext db) => _db = db;

    private async Task NapSanPhamAsync(int? chon = null) =>
        ViewBag.MaSP = new SelectList(await _db.SanPhams.OrderBy(s => s.TenSP).ToListAsync(), "MaSP", "TenSP", chon);

    /// <summary>Nhân viên cũng được xem tồn kho; thêm/sửa/xóa/nhập hàng chỉ dành cho Admin.</summary>
    public async Task<IActionResult> Index(int? maSP, string? q, bool sapHet = false, int page = 1)
    {
        ViewBag.Q = q;
        ViewBag.SapHet = sapHet;
        ViewBag.MaSPChon = maSP;
        await NapSanPhamAsync(maSP);
        var query = _db.PhienBans.AsNoTracking().Include(p => p.SanPham).AsQueryable();
        if (maSP.HasValue) query = query.Where(p => p.MaSP == maSP);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.SanPham!.TenSP.Contains(q) || p.MauSac.Contains(q) || p.DungLuong.Contains(q));
        if (sapHet) query = query.Where(p => p.SoLuongTon <= 3);
        return View(await query.OrderBy(p => p.SanPham!.TenSP).ThenBy(p => p.GiaBan).ToPagedAsync(page, 15));
    }

    public async Task<IActionResult> Details(int id)
    {
        var pb = await _db.PhienBans.AsNoTracking().Include(p => p.SanPham).FirstOrDefaultAsync(p => p.MaPB == id);
        if (pb == null) return NotFound();
        ViewBag.DaBan = await _db.ChiTietDonHangs.CountAsync(c => c.MaPB == id && c.DonHang!.TrangThai != TrangThaiDon.DaHuy);
        return View(pb);
    }

    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Create(int? maSP)
    {
        await NapSanPhamAsync(maSP);
        return View(new PhienBan { MaSP = maSP ?? 0 });
    }

    [HttpPost]
    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Create(PhienBan model)
    {
        await KiemTraTrungAsync(model);
        if (!ModelState.IsValid)
        {
            await NapSanPhamAsync(model.MaSP);
            return View(model);
        }
        _db.PhienBans.Add(model);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã thêm phiên bản";
        return RedirectToAction(nameof(Index), new { maSP = model.MaSP });
    }

    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var pb = await _db.PhienBans.FindAsync(id);
        if (pb == null) return NotFound();
        await NapSanPhamAsync(pb.MaSP);
        return View(pb);
    }

    [HttpPost]
    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Edit(int id, PhienBan model)
    {
        if (id != model.MaPB) return BadRequest();
        await KiemTraTrungAsync(model);
        if (!ModelState.IsValid)
        {
            await NapSanPhamAsync(model.MaSP);
            return View(model);
        }
        var pb = await _db.PhienBans.FindAsync(id);
        if (pb == null) return NotFound();
        pb.MaSP = model.MaSP;
        pb.DungLuong = model.DungLuong;
        pb.MauSac = model.MauSac;
        pb.GiaBan = model.GiaBan;
        pb.SoLuongTon = model.SoLuongTon;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã cập nhật phiên bản";
        return RedirectToAction(nameof(Index), new { maSP = pb.MaSP });
    }

    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var pb = await _db.PhienBans.AsNoTracking().Include(p => p.SanPham).FirstOrDefaultAsync(p => p.MaPB == id);
        if (pb == null) return NotFound();
        ViewBag.DaBan = await _db.ChiTietDonHangs.AnyAsync(c => c.MaPB == id);
        return View(pb);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var pb = await _db.PhienBans.FindAsync(id);
        if (pb == null) return NotFound();
        if (await _db.ChiTietDonHangs.AnyAsync(c => c.MaPB == id))
        {
            TempData["Loi"] = "Không thể xóa phiên bản đã có trong đơn hàng";
            return RedirectToAction(nameof(Index), new { maSP = pb.MaSP });
        }
        _db.PhienBans.Remove(pb);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã xóa phiên bản";
        return RedirectToAction(nameof(Index), new { maSP = pb.MaSP });
    }

    // ---------- Nhập hàng theo phiên bản ----------
    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> NhapHang(int id)
    {
        var pb = await _db.PhienBans.Include(p => p.SanPham).FirstOrDefaultAsync(p => p.MaPB == id);
        if (pb == null) return NotFound();
        return View(new NhapHangVM { MaPB = id, TenPhienBan = pb.TenDayDu, TonHienTai = pb.SoLuongTon });
    }

    [HttpPost]
    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> NhapHang(NhapHangVM vm)
    {
        var pb = await _db.PhienBans.Include(p => p.SanPham).FirstOrDefaultAsync(p => p.MaPB == vm.MaPB);
        if (pb == null) return NotFound();
        if (!ModelState.IsValid)
        {
            vm.TenPhienBan = pb.TenDayDu;
            vm.TonHienTai = pb.SoLuongTon;
            return View(vm);
        }
        pb.SoLuongTon += vm.SoLuongNhap;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = $"Đã nhập thêm {vm.SoLuongNhap} máy {pb.TenDayDu}. Tồn mới: {pb.SoLuongTon}";
        return RedirectToAction(nameof(Index), new { maSP = pb.MaSP });
    }

    private async Task KiemTraTrungAsync(PhienBan m)
    {
        if (!await _db.SanPhams.AnyAsync(s => s.MaSP == m.MaSP))
            ModelState.AddModelError(nameof(m.MaSP), "Sản phẩm không tồn tại");
        if (await _db.PhienBans.AnyAsync(p => p.MaSP == m.MaSP && p.DungLuong == m.DungLuong && p.MauSac == m.MauSac && p.MaPB != m.MaPB))
            ModelState.AddModelError(nameof(m.MauSac), "Sản phẩm đã có phiên bản cùng dung lượng và màu này");
    }
}
