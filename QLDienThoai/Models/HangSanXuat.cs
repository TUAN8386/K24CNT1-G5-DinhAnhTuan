using System.ComponentModel.DataAnnotations;

namespace QLDienThoai.Models;

public class HangSanXuat
{
    [Key]
    [Display(Name = "Mã hãng")]
    public int MaHang { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên hãng")]
    [StringLength(100, ErrorMessage = "Tên hãng tối đa 100 ký tự")]
    [Display(Name = "Tên hãng")]
    public string TenHang { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Quốc gia")]
    public string? QuocGia { get; set; }

    [StringLength(300)]
    [Display(Name = "Logo")]
    public string? Logo { get; set; }

    public ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
