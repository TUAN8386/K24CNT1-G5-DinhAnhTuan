using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLDienThoai.Models;

public class PhienBan
{
    [Key]
    [Display(Name = "Mã phiên bản")]
    public int MaPB { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
    [Display(Name = "Sản phẩm")]
    public int MaSP { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập dung lượng")]
    [StringLength(20)]
    [Display(Name = "Dung lượng")]
    public string DungLuong { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập màu sắc")]
    [StringLength(50)]
    [Display(Name = "Màu sắc")]
    public string MauSac { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập giá bán")]
    [Range(1, 1_000_000_000, ErrorMessage = "Giá bán phải lớn hơn 0")]
    [Column(TypeName = "decimal(18,0)")]
    [DisplayFormat(DataFormatString = "{0:N0}")]
    [Display(Name = "Giá bán")]
    public decimal GiaBan { get; set; }

    [Range(0, 100000, ErrorMessage = "Số lượng tồn không được âm")]
    [Display(Name = "Số lượng tồn")]
    public int SoLuongTon { get; set; }

    public SanPham? SanPham { get; set; }
    public ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    [NotMapped]
    public string TenDayDu => $"{SanPham?.TenSP} {DungLuong} - {MauSac}".Trim();
}
