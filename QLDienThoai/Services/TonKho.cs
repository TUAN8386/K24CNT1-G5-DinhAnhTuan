using QLDienThoai.Data;
using QLDienThoai.Models;

namespace QLDienThoai.Services;

public static class TonKho
{
    /// <summary>Hoàn lại tồn kho khi hủy đơn (chi tiết đơn phải được Include sẵn).</summary>
    public static async Task HoanAsync(AppDbContext db, DonHang dh)
    {
        foreach (var g in dh.ChiTietDonHangs.GroupBy(c => c.MaPB))
        {
            var pb = await db.PhienBans.FindAsync(g.Key);
            if (pb != null) pb.SoLuongTon += g.Sum(c => c.SoLuong);
        }
    }
}
