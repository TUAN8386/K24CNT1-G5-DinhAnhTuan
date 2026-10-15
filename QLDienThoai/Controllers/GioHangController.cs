using Microsoft.AspNetCore.Mvc;
using QLDienThoai.Services;

namespace QLDienThoai.Controllers;

public class GioHangController : Controller
{
    private readonly GioHangService _gioHang;

    public GioHangController(GioHangService gioHang) => _gioHang = gioHang;

    public async Task<IActionResult> Index() => View(await _gioHang.LayGioHangAsync());

    [HttpPost]
    public async Task<IActionResult> Them(int maPB, int soLuong = 1, bool muaNgay = false, string? returnUrl = null)
    {
        var loi = await _gioHang.ThemAsync(maPB, soLuong);
        if (loi != null)
        {
            TempData["Loi"] = loi;
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
        }
        TempData["ThongBao"] = "Đã thêm vào giỏ hàng";
        if (muaNgay) return RedirectToAction("DatHang", "DonHang");
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CapNhat(int maPB, int soLuong)
    {
        var loi = await _gioHang.CapNhatAsync(maPB, soLuong);
        if (loi != null) TempData["Loi"] = loi;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Xoa(int maPB)
    {
        _gioHang.Xoa(maPB);
        return RedirectToAction(nameof(Index));
    }
}
