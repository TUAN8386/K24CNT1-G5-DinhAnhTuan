using System.ComponentModel.DataAnnotations;

namespace QLDienThoai.Models;

/// <summary>Bảng bổ sung: tin khuyến mãi (nội dung soạn bằng editor).</summary>
public class KhuyenMai : IValidatableObject
{
    [Key]
    public int MaKM { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
    [StringLength(200)]
    [Display(Name = "Tiêu đề")]
    public string TieuDe { get; set; } = string.Empty;

    [Display(Name = "Nội dung")]
    public string? NoiDung { get; set; }

    [StringLength(300)]
    [Display(Name = "Ảnh bìa")]
    public string? AnhBia { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime NgayBatDau { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày kết thúc")]
    public DateTime NgayKetThuc { get; set; } = DateTime.Today.AddDays(7);

    [Display(Name = "Hiển thị")]
    public bool HienThi { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NgayKetThuc < NgayBatDau)
            yield return new ValidationResult("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu", new[] { nameof(NgayKetThuc) });
    }
}
