using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;
using QLDienThoai.Services;

namespace QLDienThoai.Controllers;

[Authorize(Roles = VaiTro.KhachHang)]
public class DanhGiaController : Controller
{
    private readonly AppDbContext _db;
    private readonly HtmlCleaner _cleaner;

    public DanhGiaController(AppDbContext db, HtmlCleaner cleaner)
    {
        _db = db;
        _cleaner = cleaner;
    }

    [HttpPost]
    public async Task<IActionResult> Them(DanhGiaInputVM vm)
    {
        var maTK = User.LayMaTK();
        var url = Url.Action("ChiTiet", "SanPham", new { id = vm.MaSP }) + "#danh-gia";

        if (!ModelState.IsValid)
        {
            TempData["Loi"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Redirect(url);
        }

        var daMua = await _db.ChiTietDonHangs.AnyAsync(c => c.PhienBan!.MaSP == vm.MaSP
            && c.DonHang!.MaKH == maTK && c.DonHang.TrangThai == TrangThaiDon.HoanThanh);
        if (!daMua)
        {
            TempData["Loi"] = "Bạn chỉ có thể đánh giá sản phẩm đã mua (đơn hoàn thành)";
            return Redirect(url);
        }
        if (await _db.DanhGias.AnyAsync(d => d.MaSP == vm.MaSP && d.MaTK == maTK))
        {
            TempData["Loi"] = "Bạn đã đánh giá sản phẩm này";
            return Redirect(url);
        }

        var noiDung = _cleaner.Lam(vm.NoiDung);
        if (string.IsNullOrWhiteSpace(System.Text.RegularExpressions.Regex.Replace(noiDung, "<.*?>", "")))
        {
            TempData["Loi"] = "Nội dung đánh giá không hợp lệ";
            return Redirect(url);
        }

        _db.DanhGias.Add(new DanhGia { MaSP = vm.MaSP, MaTK = maTK, Diem = vm.Diem, NoiDung = noiDung });
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Cảm ơn bạn đã đánh giá!";
        return Redirect(url);
    }
}
