using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLDienThoai.Models;

public static class TrangThaiDon
{
    public const string ChoXacNhan = "Chờ xác nhận";
    public const string DaXacNhan = "Đã xác nhận";
    public const string DangGiao = "Đang giao";
    public const string HoanThanh = "Hoàn thành";
    public const string DaHuy = "Đã hủy";

    public static readonly string[] TatCa = { ChoXacNhan, DaXacNhan, DangGiao, HoanThanh, DaHuy };

    /// <summary>Thứ tự hợp lệ: chỉ được chuyển tiến, hoặc hủy khi chưa giao.</summary>
    public static bool ChuyenHopLe(string tu, string den)
    {
        if (tu == den) return false;
        if (tu is HoanThanh or DaHuy) return false;
        if (den == DaHuy) return tu is ChoXacNhan or DaXacNhan;
        return Array.IndexOf(TatCa, den) == Array.IndexOf(TatCa, tu) + 1;
    }

    public static string MauBadge(string trangThai) => trangThai switch
    {
        ChoXacNhan => "warning",
        DaXacNhan => "info",
        DangGiao => "primary",
        HoanThanh => "success",
        DaHuy => "secondary",
        _ => "light"
    };
}

public static class PhuongThucThanhToan
{
    public const string COD = "COD";
    public const string VNPay = "VNPay";
    public const string TraGop = "Trả góp";
    public static readonly string[] TatCa = { COD, VNPay, TraGop };
}

public class DonHang
{
    [Key]
    [Display(Name = "Mã đơn")]
    public int MaDH { get; set; }

    [Display(Name = "Khách hàng")]
    public int MaKH { get; set; }

    [Display(Name = "Ngày đặt")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
    public DateTime NgayDat { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
    [StringLength(100)]
    [Display(Name = "Người nhận")]
    public string HoTenNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0")]
    [StringLength(10)]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoaiNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
    [StringLength(300)]
    [Display(Name = "Địa chỉ giao")]
    public string DiaChiGiao { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    [DisplayFormat(DataFormatString = "{0:N0}")]
    [Display(Name = "Tổng tiền")]
    public decimal TongTien { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Thanh toán")]
    public string PhuongThucTT { get; set; } = PhuongThucThanhToan.COD;

    [Required]
    [StringLength(30)]
    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = TrangThaiDon.ChoXacNhan;

    [Display(Name = "Đã thanh toán")]
    public bool DaThanhToan { get; set; }

    /// <summary>Thông tin trả góp (chỉ dùng khi PhuongThucTT = Trả góp).</summary>
    [Display(Name = "Kỳ hạn (tháng)")]
    public int? KyHanTraGop { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    [Display(Name = "Trả trước")]
    public decimal? TraTruoc { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public TaiKhoan? KhachHang { get; set; }
    public ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();
}
