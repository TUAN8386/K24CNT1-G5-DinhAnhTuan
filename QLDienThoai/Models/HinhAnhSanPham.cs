using System.ComponentModel.DataAnnotations;

namespace QLDienThoai.Models;

/// <summary>Ảnh phụ (thư viện ảnh) của sản phẩm: mặt sau, cạnh bên, hộp máy...</summary>
public class HinhAnhSanPham
{
    [Key]
    public int MaHA { get; set; }

    public int MaSP { get; set; }

    [Required, StringLength(300)]
    [Display(Name = "Đường dẫn ảnh")]
    public string DuongDan { get; set; } = string.Empty;

    [Display(Name = "Thứ tự")]
    public int ThuTu { get; set; }

    public SanPham? SanPham { get; set; }
}
