using System.ComponentModel.DataAnnotations;

namespace QLDienThoai.Models;

/// <summary>Bảng bổ sung: đánh giá sản phẩm (điểm 1–5 + bài viết từ editor).</summary>
public class DanhGia
{
    [Key]
    public int MaDG { get; set; }

    public int MaSP { get; set; }

    public int MaTK { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn điểm")]
    [Range(1, 5, ErrorMessage = "Điểm đánh giá từ 1 đến 5")]
    [Display(Name = "Điểm")]
    public int Diem { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
    [StringLength(4000, ErrorMessage = "Nội dung tối đa 4000 ký tự")]
    [Display(Name = "Nội dung")]
    public string NoiDung { get; set; } = string.Empty;

    public DateTime NgayDanhGia { get; set; } = DateTime.Now;

    public SanPham? SanPham { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }
}
