using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;

namespace QLDienThoai.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = VaiTro.QuanLy)]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var dauThang = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var vm = new DashboardVM
        {
            SoDonChoXacNhan = await _db.DonHangs.CountAsync(d => d.TrangThai == TrangThaiDon.ChoXacNhan),
            SoDonHomNay = await _db.DonHangs.CountAsync(d => d.NgayDat >= DateTime.Today),
            DoanhThuThangNay = await _db.DonHangs
                .Where(d => d.TrangThai == TrangThaiDon.HoanThanh && d.NgayDat >= dauThang)
                .SumAsync(d => (decimal?)d.TongTien) ?? 0,
            SoSanPham = await _db.SanPhams.CountAsync(),
            SoKhachHang = await _db.TaiKhoans.CountAsync(t => t.VaiTro == VaiTro.KhachHang),
            SapHetHang = await _db.PhienBans.Include(p => p.SanPham)
                .Where(p => p.SoLuongTon <= 3).OrderBy(p => p.SoLuongTon).Take(8).ToListAsync(),
            DonMoi = await _db.DonHangs.Include(d => d.KhachHang)
                .OrderByDescending(d => d.NgayDat).Take(8).ToListAsync()
        };
        return View(vm);
    }
}
