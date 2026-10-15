using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;
using QLDienThoai.Services;

namespace QLDienThoai.Controllers;

public class SanPhamController : Controller
{
    private const int PageSize = 9;
    private readonly AppDbContext _db;
    private readonly DaXemService _daXem;
    private readonly IConfiguration _config;

    public SanPhamController(AppDbContext db, DaXemService daXem, IConfiguration config)
    {
        _db = db;
        _daXem = daXem;
        _config = config;
    }

    // GET /SanPham?q=&hang=1&hang=2&gia=5-15&dungLuong=128GB&sapXep=ban-chay&page=
    public async Task<IActionResult> Index([FromQuery] LocSanPham loc)
    {
        loc.SapXep ??= "ban-chay";
        var q = _db.SanPhams.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(loc.Q))
        {
            var tuKhoa = loc.Q.Trim();
            q = q.Where(s => s.TenSP.Contains(tuKhoa) || s.HangSanXuat!.TenHang.Contains(tuKhoa));
        }

        // Lọc nhiều hãng (checkbox)
        var hangs = loc.Hang.Where(h => h > 0).Distinct().ToList();
        if (hangs.Count > 0)
            q = q.Where(s => hangs.Contains(s.MaHang));

        // Lọc khoảng giá + nhiều dung lượng trên cùng một phiên bản
        var (min, max) = loc.KhoangGia();
        var dls = loc.DungLuong.Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().ToList();
        var locDl = dls.Count > 0;
        if (min.HasValue || max.HasValue || locDl)
        {
            q = q.Where(s => s.PhienBans.Any(p => (min == null || p.GiaBan >= min)
                                                  && (max == null || p.GiaBan <= max)
                                                  && (!locDl || dls.Contains(p.DungLuong))));
        }

        q = loc.SapXep switch
        {
            "gia-tang" => q.OrderBy(s => s.PhienBans.Min(p => (decimal?)p.GiaBan)),
            "gia-giam" => q.OrderByDescending(s => s.PhienBans.Min(p => (decimal?)p.GiaBan)),
            "moi" => q.OrderByDescending(s => s.NgayTao),
            "ten" => q.OrderBy(s => s.TenSP),
            _ => q.SapXepBanChay()
        };

        // Sản phẩm đã xem (Cookie) – giữ đúng thứ tự xem gần nhất
        var idsDaXem = _daXem.LayDanhSach().Take(5).ToList();
        var daXem = idsDaXem.Count == 0 ? new List<SanPhamTheVM>()
            : await _db.SanPhams.AsNoTracking().Where(s => idsDaXem.Contains(s.MaSP)).ToThe().ToListAsync();

        var vm = new DanhSachSanPhamVM
        {
            Loc = loc,
            KetQua = await q.ToThe().ToPagedAsync(loc.Page, PageSize),
            Hangs = await _db.HangSanXuats.AsNoTracking().OrderBy(h => h.TenHang).ToListAsync(),
            DungLuongs = (await _db.PhienBans.Select(p => p.DungLuong).Distinct().ToListAsync())
                .OrderBy(DoiRaGB).ToList(),
            DaXem = idsDaXem.Select(id => daXem.FirstOrDefault(d => d.MaSP == id))
                .Where(x => x != null).Select(x => x!).ToList()
        };
        return View(vm);
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var sp = await _db.SanPhams.AsNoTracking()
            .Include(s => s.HangSanXuat)
            .Include(s => s.HinhAnhs)
            .FirstOrDefaultAsync(s => s.MaSP == id);
        if (sp == null) return NotFound();

        var phienBans = await _db.PhienBans.AsNoTracking().Where(p => p.MaSP == id)
            .OrderBy(p => p.GiaBan).ToListAsync();
        var danhGias = await _db.DanhGias.AsNoTracking().Include(d => d.TaiKhoan)
            .Where(d => d.MaSP == id).OrderByDescending(d => d.NgayDanhGia).ToListAsync();

        var duocDanhGia = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var maTK = User.LayMaTK();
            duocDanhGia = !danhGias.Any(d => d.MaTK == maTK) && await _db.ChiTietDonHangs.AnyAsync(c =>
                c.PhienBan!.MaSP == id && c.DonHang!.MaKH == maTK && c.DonHang.TrangThai == TrangThaiDon.HoanThanh);
        }

        // Lấy danh sách đã xem TRƯỚC khi thêm sản phẩm hiện tại
        var idsDaXem = _daXem.LayDanhSach().Where(x => x != id).Take(4).ToList();
        _daXem.Them(id);
        var daXem = await _db.SanPhams.Where(s => idsDaXem.Contains(s.MaSP)).ToThe().ToListAsync();

        var vm = new ChiTietSanPhamVM
        {
            SanPham = sp,
            PhienBans = phienBans,
            DanhGias = danhGias,
            DiemTrungBinh = danhGias.Count == 0 ? 0 : danhGias.Average(d => d.Diem),
            DuocDanhGia = duocDanhGia,
            DaXem = idsDaXem.Select(x => daXem.FirstOrDefault(d => d.MaSP == x)).Where(x => x != null).Select(x => x!).ToList(),
            LaiSuatThang = _config.GetValue<decimal>("TraGop:LaiSuatThang")
        };
        return View(vm);
    }

    private static int DoiRaGB(string dl)
    {
        var so = new string(dl.TakeWhile(char.IsDigit).ToArray());
        if (!int.TryParse(so, out var n)) return int.MaxValue;
        return dl.ToUpperInvariant().Contains("TB") ? n * 1024 : n;
    }
}
