using System.Security.Claims;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;

namespace QLDienThoai.Services;

public static class SanPhamQuery
{
    /// <summary>Chiếu sản phẩm sang thẻ hiển thị (EF dịch thành 1 câu SQL có tham số).</summary>
    public static IQueryable<SanPhamTheVM> ToThe(this IQueryable<SanPham> q) =>
        q.Select(s => new SanPhamTheVM
        {
            MaSP = s.MaSP,
            TenSP = s.TenSP,
            TenHang = s.HangSanXuat!.TenHang,
            AnhDaiDien = s.AnhDaiDien,
            GiaThapNhat = s.PhienBans.Min(p => (decimal?)p.GiaBan) ?? 0,
            ConHang = s.PhienBans.Any(p => p.SoLuongTon > 0),
            DiemTrungBinh = s.DanhGias.Average(d => (double?)d.Diem) ?? 0,
            SoDanhGia = s.DanhGias.Count()
        });

    public static IQueryable<SanPham> SapXepBanChay(this IQueryable<SanPham> q) =>
        q.OrderByDescending(s => s.PhienBans.SelectMany(p => p.ChiTietDonHangs)
            .Count(c => c.DonHang!.TrangThai != TrangThaiDon.DaHuy));
}

public static class ClaimsExtensions
{
    public static int LayMaTK(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
