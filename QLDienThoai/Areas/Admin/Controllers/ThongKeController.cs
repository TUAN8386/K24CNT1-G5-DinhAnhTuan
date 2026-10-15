using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;

namespace QLDienThoai.Areas.Admin.Controllers;

/// <summary>Thống kê doanh thu (chỉ tính đơn "Hoàn thành").</summary>
[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class ThongKeController : Controller
{
    private readonly AppDbContext _db;

    public ThongKeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? nam)
    {
        var cacNam = await _db.DonHangs.Select(d => d.NgayDat.Year).Distinct().OrderByDescending(y => y).ToListAsync();
        if (!cacNam.Contains(DateTime.Today.Year)) cacNam.Insert(0, DateTime.Today.Year);
        var namChon = nam ?? DateTime.Today.Year;

        var donHoanThanh = _db.DonHangs.Where(d => d.TrangThai == TrangThaiDon.HoanThanh && d.NgayDat.Year == namChon);

        var theoThang = await donHoanThanh
            .GroupBy(d => d.NgayDat.Month)
            .Select(g => new { Thang = g.Key, DoanhThu = g.Sum(d => d.TongTien) })
            .ToListAsync();

        var chiTiet = _db.ChiTietDonHangs.Where(c => c.DonHang!.TrangThai == TrangThaiDon.HoanThanh && c.DonHang.NgayDat.Year == namChon);

        var theoHang = await chiTiet
            .GroupBy(c => c.PhienBan!.SanPham!.HangSanXuat!.TenHang)
            .Select(g => new { TenHang = g.Key, DoanhThu = g.Sum(c => c.DonGia * c.SoLuong), SoMay = g.Sum(c => c.SoLuong) })
            .OrderByDescending(x => x.DoanhThu).ToListAsync();

        var top = await chiTiet
            .GroupBy(c => c.PhienBan!.SanPham!.TenSP)
            .Select(g => new { TenSP = g.Key, SoMay = g.Sum(c => c.SoLuong), DoanhThu = g.Sum(c => c.DonGia * c.SoLuong) })
            .OrderByDescending(x => x.SoMay).ThenByDescending(x => x.DoanhThu).Take(5).ToListAsync();

        var trangThai = await _db.DonHangs.Where(d => d.NgayDat.Year == namChon)
            .GroupBy(d => d.TrangThai).Select(g => new { g.Key, SoDon = g.Count() }).ToListAsync();

        var vm = new ThongKeVM
        {
            Nam = namChon,
            CacNam = cacNam,
            TheoHang = theoHang.Select(x => (x.TenHang, x.DoanhThu, x.SoMay)).ToList(),
            TopSanPham = top.Select(x => (x.TenSP, x.SoMay, x.DoanhThu)).ToList(),
            DonTheoTrangThai = trangThai.ToDictionary(x => x.Key, x => x.SoDon)
        };
        foreach (var t in theoThang) vm.DoanhThuTheoThang[t.Thang - 1] = t.DoanhThu;
        return View(vm);
    }
}
