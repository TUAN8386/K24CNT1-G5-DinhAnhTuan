using System.ComponentModel.DataAnnotations;

namespace QLDienThoai.Models;

public class SanPham
{
    [Key]
    [Display(Name = "Mã SP")]
    public int MaSP { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự")]
    [Display(Name = "Tên sản phẩm")]
    public string TenSP { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn hãng")]
    [Display(Name = "Hãng sản xuất")]
    public int MaHang { get; set; }

    [StringLength(100)]
    [Display(Name = "Màn hình")]
    public string? ManHinh { get; set; }

    [StringLength(100)]
    [Display(Name = "Chip")]
    public string? Chip { get; set; }

    [StringLength(150)]
    [Display(Name = "Camera")]
    public string? Camera { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập dung lượng pin")]
    [StringLength(50, ErrorMessage = "Pin tối đa 50 ký tự")]
    [Display(Name = "Pin")]
    public string? Pin { get; set; }

    [StringLength(50)]
    [Display(Name = "Hệ điều hành")]
    public string? HeDieuHanh { get; set; }

    [StringLength(300)]
    [Display(Name = "Ảnh đại diện")]
    public string? AnhDaiDien { get; set; }

    /// <summary>Mô tả / thông số kỹ thuật chi tiết (HTML từ editor, đã được lọc XSS).</summary>
    [Display(Name = "Mô tả chi tiết")]
    public string? MoTa { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    public HangSanXuat? HangSanXuat { get; set; }
    public ICollection<PhienBan> PhienBans { get; set; } = new List<PhienBan>();
    public ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
    /// <summary>Thư viện ảnh phụ (ngoài ảnh đại diện).</summary>
    public ICollection<HinhAnhSanPham> HinhAnhs { get; set; } = new List<HinhAnhSanPham>();
}
