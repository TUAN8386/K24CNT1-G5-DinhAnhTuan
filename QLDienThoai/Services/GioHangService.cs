using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models.ViewModels;

namespace QLDienThoai.Services;

/// <summary>Giỏ hàng lưu trong Session (chỉ lưu MaPB + số lượng, giá luôn lấy lại từ CSDL).</summary>
public class GioHangService
{
    public const string SessionKey = "GioHang";
    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext _db;

    public GioHangService(IHttpContextAccessor http, AppDbContext db)
    {
        _http = http;
        _db = db;
    }

    private ISession Session => _http.HttpContext!.Session;

    public List<GioHangItem> LayItems() => Session.GetJson<List<GioHangItem>>(SessionKey) ?? new();

    private void Luu(List<GioHangItem> items) => Session.SetJson(SessionKey, items);

    public int DemSoLuong() => LayItems().Sum(i => i.SoLuong);

    /// <summary>Thêm vào giỏ, trả về thông báo lỗi nếu vượt tồn kho.</summary>
    public async Task<string?> ThemAsync(int maPB, int soLuong)
    {
        if (soLuong < 1) return "Số lượng phải lớn hơn 0";
        var pb = await _db.PhienBans.AsNoTracking().FirstOrDefaultAsync(p => p.MaPB == maPB);
        if (pb == null) return "Phiên bản không tồn tại";

        var items = LayItems();
        var item = items.FirstOrDefault(i => i.MaPB == maPB);
        var tong = (item?.SoLuong ?? 0) + soLuong;
        if (tong > pb.SoLuongTon)
            return $"Chỉ còn {pb.SoLuongTon} máy cho phiên bản này";

        if (item == null) items.Add(new GioHangItem { MaPB = maPB, SoLuong = soLuong });
        else item.SoLuong = tong;
        Luu(items);
        return null;
    }

    public async Task<string?> CapNhatAsync(int maPB, int soLuong)
    {
        var items = LayItems();
        var item = items.FirstOrDefault(i => i.MaPB == maPB);
        if (item == null) return "Sản phẩm không có trong giỏ";
        if (soLuong <= 0)
        {
            items.Remove(item);
            Luu(items);
            return null;
        }
        var ton = await _db.PhienBans.Where(p => p.MaPB == maPB).Select(p => p.SoLuongTon).FirstOrDefaultAsync();
        if (soLuong > ton) return $"Chỉ còn {ton} máy cho phiên bản này";
        item.SoLuong = soLuong;
        Luu(items);
        return null;
    }

    public void Xoa(int maPB)
    {
        var items = LayItems();
        items.RemoveAll(i => i.MaPB == maPB);
        Luu(items);
    }

    public void XoaHet() => Session.Remove(SessionKey);

    public async Task<GioHangVM> LayGioHangAsync()
    {
        var items = LayItems();
        var ids = items.Select(i => i.MaPB).ToList();
        var pbs = await _db.PhienBans.AsNoTracking().Include(p => p.SanPham)
            .Where(p => ids.Contains(p.MaPB)).ToListAsync();

        var vm = new GioHangVM();
        foreach (var it in items)
        {
            var pb = pbs.FirstOrDefault(p => p.MaPB == it.MaPB);
            if (pb == null) continue;
            vm.Dongs.Add(new GioHangDongVM
            {
                MaPB = pb.MaPB, MaSP = pb.MaSP, TenSP = pb.SanPham!.TenSP, DungLuong = pb.DungLuong, MauSac = pb.MauSac,
                AnhDaiDien = pb.SanPham.AnhDaiDien, GiaBan = pb.GiaBan, SoLuong = it.SoLuong, SoLuongTon = pb.SoLuongTon
            });
        }
        return vm;
    }
}
