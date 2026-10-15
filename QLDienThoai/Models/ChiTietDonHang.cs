using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLDienThoai.Models;

/// <summary>
/// Mỗi dòng chi tiết ứng với MỘT máy bán ra để gán đúng một IMEI.
/// Khi khách đặt 2 máy cùng phiên bản, hệ thống tạo 2 dòng (SoLuong = 1).
/// </summary>
public class ChiTietDonHang
{
    [Key]
    public int MaCT { get; set; }

    public int MaDH { get; set; }

    public int MaPB { get; set; }

    [Range(1, 100)]
    [Display(Name = "Số lượng")]
    public int SoLuong { get; set; } = 1;

    [Column(TypeName = "decimal(18,0)")]
    [DisplayFormat(DataFormatString = "{0:N0}")]
    [Display(Name = "Đơn giá")]
    public decimal DonGia { get; set; }

    [RegularExpression(@"^\d{15}$", ErrorMessage = "IMEI phải gồm đúng 15 chữ số")]
    [StringLength(15)]
    [Display(Name = "IMEI")]
    public string? IMEI { get; set; }

    public DonHang? DonHang { get; set; }
    public PhienBan? PhienBan { get; set; }
}
