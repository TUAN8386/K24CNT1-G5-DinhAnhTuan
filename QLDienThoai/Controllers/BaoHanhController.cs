using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;

namespace QLDienThoai.Controllers;

/// <summary>Tra cứu bảo hành theo IMEI (bảo hành 12 tháng kể từ ngày đặt đơn hoàn thành).</summary>
public class BaoHanhController : Controller
{
    public const int SoThangBaoHanh = 12;
    private readonly AppDbContext _db;

    public BaoHanhController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(string? imei)
    {
        var vm = new TraCuuBaoHanhVM { IMEI = imei?.Trim() };
        ModelState.Clear();
        if (string.IsNullOrEmpty(vm.IMEI)) return View(vm);
        if (!TryValidateModel(vm)) return View(vm);

        vm.DaTraCuu = true;
        vm.KetQua = await _db.ChiTietDonHangs.AsNoTracking()
            .Include(c => c.DonHang)
            .Include(c => c.PhienBan).ThenInclude(p => p!.SanPham)
            .FirstOrDefaultAsync(c => c.IMEI == vm.IMEI && c.DonHang!.TrangThai != TrangThaiDon.DaHuy);
        if (vm.KetQua != null)
            vm.HanBaoHanh = vm.KetQua.DonHang!.NgayDat.AddMonths(SoThangBaoHanh);
        return View(vm);
    }
}
