using System.ComponentModel.DataAnnotations;

namespace QLDienThoai.Models.ViewModels;

public class GanImeiDongVM
{
    public int MaCT { get; set; }
    public string TenMay { get; set; } = string.Empty;

    /// <summary>Có thể để trống để gán sau; nếu nhập phải đủ 15 chữ số.</summary>
    [RegularExpression(@"^\d{15}$", ErrorMessage = "IMEI phải gồm đúng 15 chữ số")]
    public string? IMEI { get; set; }
}

public class GanImeiVM
{
    public int MaDH { get; set; }
    public List<GanImeiDongVM> Dongs { get; set; } = new();
}

public class NhapHangVM
{
    public int MaPB { get; set; }
    public string TenPhienBan { get; set; } = string.Empty;
    public int TonHienTai { get; set; }

    [Range(1, 10000, ErrorMessage = "Số lượng nhập từ 1 đến 10.000")]
    [Display(Name = "Số lượng nhập thêm")]
    public int SoLuongNhap { get; set; } = 1;
}

public class DashboardVM
{
    public int SoDonChoXacNhan { get; set; }
    public int SoDonHomNay { get; set; }
    public decimal DoanhThuThangNay { get; set; }
    public int SoSanPham { get; set; }
    public int SoKhachHang { get; set; }
    public List<PhienBan> SapHetHang { get; set; } = new();
    public List<DonHang> DonMoi { get; set; } = new();
}

public class ThongKeVM
{
    public int Nam { get; set; }
    public List<int> CacNam { get; set; } = new();
    public decimal[] DoanhThuTheoThang { get; set; } = new decimal[12];
    public List<(string TenHang, decimal DoanhThu, int SoMay)> TheoHang { get; set; } = new();
    public List<(string TenSP, int SoMay, decimal DoanhThu)> TopSanPham { get; set; } = new();
    public Dictionary<string, int> DonTheoTrangThai { get; set; } = new();
    public decimal TongDoanhThu => DoanhThuTheoThang.Sum();
}
