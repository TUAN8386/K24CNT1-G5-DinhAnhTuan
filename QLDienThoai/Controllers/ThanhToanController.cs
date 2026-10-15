using Microsoft.AspNetCore.Mvc;
using QLDienThoai.Data;
using QLDienThoai.Services;

namespace QLDienThoai.Controllers;

public class ThanhToanController : Controller
{
    private readonly AppDbContext _db;
    private readonly VnPayService _vnPay;

    public ThanhToanController(AppDbContext db, VnPayService vnPay)
    {
        _db = db;
        _vnPay = vnPay;
    }

    /// <summary>VNPay chuyển hướng về đây sau khi khách thanh toán (sandbox).</summary>
    [HttpGet]
    public async Task<IActionResult> VnPayReturn()
    {
        var (hopLe, thanhCong, maDH) = _vnPay.XuLyKetQua(Request.Query);
        ViewBag.MaDH = maDH;

        if (!hopLe)
        {
            ViewBag.KetQua = "Chữ ký không hợp lệ – dữ liệu thanh toán có thể đã bị sửa đổi.";
            ViewBag.ThanhCong = false;
            return View();
        }

        var dh = await _db.DonHangs.FindAsync(maDH);
        if (dh != null && thanhCong && !dh.DaThanhToan)
        {
            dh.DaThanhToan = true;
            await _db.SaveChangesAsync();
        }

        ViewBag.ThanhCong = thanhCong;
        ViewBag.KetQua = thanhCong
            ? $"Thanh toán thành công cho đơn #{maDH}."
            : $"Thanh toán chưa thành công (mã {Request.Query["vnp_ResponseCode"]}). Bạn có thể thanh toán lại trong chi tiết đơn.";
        return View();
    }
}
