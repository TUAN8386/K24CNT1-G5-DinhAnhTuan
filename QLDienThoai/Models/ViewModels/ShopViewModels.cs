using System.ComponentModel.DataAnnotations;

namespace QLDienThoai.Models.ViewModels;

/// <summary>Tham số lọc / tìm kiếm / sắp xếp danh sách sản phẩm.</summary>
public class LocSanPham
{
    public string? Q { get; set; }
    /// <summary>Lọc nhiều hãng cùng lúc: ?hang=1&amp;hang=2</summary>
    public List<int> Hang { get; set; } = new();
    /// <summary>Khoảng giá chọn nhanh: duoi-5 | 5-15 | 15-25 | tren-25</summary>
    public string? Gia { get; set; }
    public decimal? GiaMin { get; set; }
    public decimal? GiaMax { get; set; }
    /// <summary>Lọc nhiều dung lượng: ?dungLuong=128GB&amp;dungLuong=256GB</summary>
    public List<string> DungLuong { get; set; } = new();
    /// <summary>ban-chay | moi | gia-tang | gia-giam | ten</summary>
    public string? SapXep { get; set; }
    public int Page { get; set; } = 1;

    /// <summary>Các khoảng giá hiển thị ở bộ lọc (khóa, nhãn, từ, đến).</summary>
    public static readonly (string Khoa, string Nhan, decimal? Tu, decimal? Den)[] CacKhoangGia =
    {
        ("duoi-5", "Dưới 5 triệu", null, 5_000_000m),
        ("5-15", "5 – 15 triệu", 5_000_000m, 15_000_000m),
        ("15-25", "15 – 25 triệu", 15_000_000m, 25_000_000m),
        ("tren-25", "Trên 25 triệu", 25_000_000m, null)
    };

    /// <summary>Khoảng giá thực tế: ưu tiên ô chọn nhanh, nếu không thì dùng giaMin/giaMax.</summary>
    public (decimal? Tu, decimal? Den) KhoangGia()
    {
        foreach (var k in CacKhoangGia)
            if (k.Khoa == Gia) return (k.Tu, k.Den);
        return (GiaMin, GiaMax);
    }
}

public class SanPhamTheVM
{
    public int MaSP { get; set; }
    public string TenSP { get; set; } = string.Empty;
    public string? TenHang { get; set; }
    public string? AnhDaiDien { get; set; }
    public decimal GiaThapNhat { get; set; }
    public bool ConHang { get; set; }
    public double DiemTrungBinh { get; set; }
    public int SoDanhGia { get; set; }
}

public class DanhSachSanPhamVM
{
    public LocSanPham Loc { get; set; } = new();
    public PagedResult<SanPhamTheVM> KetQua { get; set; } = new();
    public List<HangSanXuat> Hangs { get; set; } = new();
    public List<string> DungLuongs { get; set; } = new();
    /// <summary>Sản phẩm đã xem gần đây (đọc từ Cookie SanPhamDaXem).</summary>
    public List<SanPhamTheVM> DaXem { get; set; } = new();
}

public class TrangChuVM
{
    public List<SanPhamTheVM> MoiNhat { get; set; } = new();
    public List<SanPhamTheVM> BanChay { get; set; } = new();
    public List<SanPhamTheVM> DaXem { get; set; } = new();
    public List<KhuyenMai> KhuyenMais { get; set; } = new();
    public List<HangSanXuat> Hangs { get; set; } = new();
}

public class ChiTietSanPhamVM
{
    public SanPham SanPham { get; set; } = null!;
    public List<PhienBan> PhienBans { get; set; } = new();
    public List<DanhGia> DanhGias { get; set; } = new();
    public double DiemTrungBinh { get; set; }
    public bool DuocDanhGia { get; set; }
    public List<SanPhamTheVM> DaXem { get; set; } = new();
    public decimal LaiSuatThang { get; set; }
}

public class DanhGiaInputVM
{
    public int MaSP { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn điểm")]
    [Range(1, 5, ErrorMessage = "Điểm đánh giá từ 1 đến 5")]
    public int Diem { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
    [StringLength(4000, ErrorMessage = "Nội dung tối đa 4000 ký tự")]
    public string NoiDung { get; set; } = string.Empty;
}

public class GioHangItem
{
    public int MaPB { get; set; }
    public int SoLuong { get; set; }
}

public class GioHangDongVM
{
    public int MaPB { get; set; }
    public int MaSP { get; set; }
    public string TenSP { get; set; } = string.Empty;
    public string DungLuong { get; set; } = string.Empty;
    public string MauSac { get; set; } = string.Empty;
    public string? AnhDaiDien { get; set; }
    public decimal GiaBan { get; set; }
    public int SoLuong { get; set; }
    public int SoLuongTon { get; set; }
    public decimal ThanhTien => GiaBan * SoLuong;
}

public class GioHangVM
{
    public List<GioHangDongVM> Dongs { get; set; } = new();
    public decimal TongTien => Dongs.Sum(d => d.ThanhTien);
}

public class DatHangVM : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
    [StringLength(100)]
    [Display(Name = "Họ tên người nhận")]
    public string HoTenNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoaiNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
    [StringLength(300, MinimumLength = 10, ErrorMessage = "Địa chỉ từ 10 đến 300 ký tự")]
    [Display(Name = "Địa chỉ giao hàng")]
    public string DiaChiGiao { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Phương thức thanh toán")]
    public string PhuongThucTT { get; set; } = PhuongThucThanhToan.COD;

    [Display(Name = "Kỳ hạn trả góp (tháng)")]
    public int? KyHanTraGop { get; set; }

    [Display(Name = "Trả trước (%)")]
    public int? PhanTramTraTruoc { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public GioHangVM? GioHang { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PhuongThucThanhToan.TatCa.Contains(PhuongThucTT))
            yield return new ValidationResult("Phương thức thanh toán không hợp lệ", new[] { nameof(PhuongThucTT) });

        if (PhuongThucTT == PhuongThucThanhToan.TraGop)
        {
            if (KyHanTraGop is not (6 or 9 or 12))
                yield return new ValidationResult("Kỳ hạn trả góp phải là 6, 9 hoặc 12 tháng", new[] { nameof(KyHanTraGop) });
            if (PhanTramTraTruoc is null or < 10 or > 70)
                yield return new ValidationResult("Trả trước từ 10% đến 70%", new[] { nameof(PhanTramTraTruoc) });
        }
    }
}

public class TraCuuBaoHanhVM
{
    [Required(ErrorMessage = "Vui lòng nhập IMEI")]
    [RegularExpression(@"^\d{15}$", ErrorMessage = "IMEI phải gồm đúng 15 chữ số")]
    [Display(Name = "Số IMEI")]
    public string? IMEI { get; set; }

    public bool DaTraCuu { get; set; }
    public ChiTietDonHang? KetQua { get; set; }
    public DateTime? HanBaoHanh { get; set; }
    public bool ConBaoHanh => HanBaoHanh.HasValue && HanBaoHanh.Value.Date >= DateTime.Today;
}

public class TraGopVM
{
    public decimal GiaMay { get; set; }
    public int PhanTramTraTruoc { get; set; }
    public int KyHan { get; set; }
    public decimal LaiSuatThang { get; set; }

    public decimal TraTruoc => Math.Round(GiaMay * PhanTramTraTruoc / 100m, 0);
    public decimal SoTienVay => GiaMay - TraTruoc;
    /// <summary>Lãi phẳng: góp mỗi tháng = gốc/kỳ hạn + gốc × lãi suất tháng.</summary>
    public decimal GopMoiThang => KyHan <= 0 ? 0 : Math.Round(SoTienVay / KyHan + SoTienVay * LaiSuatThang / 100m, 0);
    public decimal TongPhaiTra => TraTruoc + GopMoiThang * KyHan;
}
