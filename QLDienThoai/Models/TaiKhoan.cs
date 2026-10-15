using System.ComponentModel.DataAnnotations;

namespace QLDienThoai.Models;

public static class VaiTro
{
    public const string Admin = "Admin";
    public const string NhanVien = "NhanVien";
    public const string KhachHang = "KhachHang";
    public const string QuanLy = Admin + "," + NhanVien;
    public static readonly string[] TatCa = { Admin, NhanVien, KhachHang };

    public static string TenHienThi(string vaiTro) => vaiTro switch
    {
        Admin => "Quản trị viên",
        NhanVien => "Nhân viên bán hàng",
        _ => "Khách hàng"
    };
}

public class TaiKhoan
{
    [Key]
    [Display(Name = "Mã TK")]
    public int MaTK { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập từ 4 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên đăng nhập chỉ gồm chữ, số, dấu chấm và gạch dưới")]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = string.Empty;

    /// <summary>Mật khẩu đã băm (PBKDF2 qua PasswordHasher), không lưu mật khẩu gốc.</summary>
    [Required]
    [StringLength(500)]
    public string MatKhauHash { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0")]
    [StringLength(10)]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(150)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Vai trò")]
    public string VaiTro { get; set; } = Models.VaiTro.KhachHang;

    [Display(Name = "Bị khóa")]
    public bool BiKhoa { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    public ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
