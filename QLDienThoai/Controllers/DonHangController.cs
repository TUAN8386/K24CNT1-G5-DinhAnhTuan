using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;
using QLDienThoai.Services;

namespace QLDienThoai.Controllers;

[Authorize(Roles = VaiTro.KhachHang)]
public class DonHangController : Controller
{
    private readonly AppDbContext _db;
    private readonly GioHangService _gioHang;
    private readonly EmailService _email;
    private readonly VnPayService _vnPay;
    private readonly IConfiguration _config;

    public DonHangController(AppDbContext db, GioHangService gioHang, EmailService email, VnPayService vnPay, IConfiguration config)
    {
        _db = db;
        _gioHang = gioHang;
        _email = email;
        _vnPay = vnPay;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> DatHang()
    {
        var gio = await _gioHang.LayGioHangAsync();
        if (gio.Dongs.Count == 0)
        {
            TempData["Loi"] = "Giỏ hàng đang trống";
            return RedirectToAction("Index", "GioHang");
        }
        var tk = await _db.TaiKhoans.FindAsync(User.LayMaTK());
        ViewBag.LaiSuatThang = _config.GetValue<decimal>("TraGop:LaiSuatThang");
        return View(new DatHangVM
        {
            HoTenNhan = tk?.HoTen ?? "",
            SoDienThoaiNhan = tk?.SoDienThoai ?? "",
            GioHang = gio,
            KyHanTraGop = 6,
            PhanTramTraTruoc = 30
        });
    }

    [HttpPost]
    public async Task<IActionResult> DatHang(DatHangVM vm)
    {
        var gio = await _gioHang.LayGioHangAsync();
        vm.GioHang = gio;
        ViewBag.LaiSuatThang = _config.GetValue<decimal>("TraGop:LaiSuatThang");

        if (gio.Dongs.Count == 0)
        {
            TempData["Loi"] = "Giỏ hàng đang trống";
            return RedirectToAction("Index", "GioHang");
        }
        if (!ModelState.IsValid) return View(vm);

        var maKH = User.LayMaTK();
        DonHang dh;

        await using (var tx = await _db.Database.BeginTransactionAsync())
        {
            // Trừ tồn kho nguyên tử: chỉ trừ khi còn đủ hàng (tránh bán vượt tồn khi nhiều người đặt cùng lúc)
            foreach (var d in gio.Dongs)
            {
                var soDong = await _db.PhienBans
                    .Where(p => p.MaPB == d.MaPB && p.SoLuongTon >= d.SoLuong)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.SoLuongTon, p => p.SoLuongTon - d.SoLuong));
                if (soDong == 0)
                {
                    await tx.RollbackAsync();
                    ModelState.AddModelError("", $"{d.TenSP} {d.DungLuong} {d.MauSac}: không đủ hàng (còn {d.SoLuongTon}). Vui lòng cập nhật giỏ hàng.");
                    return View(vm);
                }
            }

            // Lấy lại giá từ CSDL, không tin dữ liệu phía client
            var ids = gio.Dongs.Select(d => d.MaPB).ToList();
            var gia = await _db.PhienBans.Where(p => ids.Contains(p.MaPB)).ToDictionaryAsync(p => p.MaPB, p => p.GiaBan);

            dh = new DonHang
            {
                MaKH = maKH,
                NgayDat = DateTime.Now,
                HoTenNhan = vm.HoTenNhan.Trim(),
                SoDienThoaiNhan = vm.SoDienThoaiNhan.Trim(),
                DiaChiGiao = vm.DiaChiGiao.Trim(),
                PhuongThucTT = vm.PhuongThucTT,
                GhiChu = vm.GhiChu,
                TrangThai = TrangThaiDon.ChoXacNhan
            };
            // Mỗi máy một dòng để gán IMEI riêng
            foreach (var d in gio.Dongs)
                for (int i = 0; i < d.SoLuong; i++)
                    dh.ChiTietDonHangs.Add(new ChiTietDonHang { MaPB = d.MaPB, SoLuong = 1, DonGia = gia[d.MaPB] });

            dh.TongTien = dh.ChiTietDonHangs.Sum(c => c.DonGia * c.SoLuong);

            if (vm.PhuongThucTT == PhuongThucThanhToan.TraGop)
            {
                dh.KyHanTraGop = vm.KyHanTraGop;
                dh.TraTruoc = Math.Round(dh.TongTien * (vm.PhanTramTraTruoc ?? 0) / 100m, 0);
            }

            _db.DonHangs.Add(dh);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        _gioHang.XoaHet();

        // Gửi email xác nhận (không chặn đơn nếu lỗi)
        var email = await _db.TaiKhoans.Where(t => t.MaTK == maKH).Select(t => t.Email).FirstOrDefaultAsync();
        var dhDayDu = await _db.DonHangs.AsNoTracking()
            .Include(x => x.ChiTietDonHangs).ThenInclude(c => c.PhienBan).ThenInclude(p => p!.SanPham)
            .FirstAsync(x => x.MaDH == dh.MaDH);
        await _email.GuiXacNhanDonAsync(dhDayDu, email);

        if (dh.PhuongThucTT == PhuongThucThanhToan.VNPay)
            return RedirectToAction(nameof(ThanhToanVnPay), new { id = dh.MaDH });

        return RedirectToAction(nameof(ThanhCong), new { id = dh.MaDH });
    }

    public async Task<IActionResult> ThanhToanVnPay(int id)
    {
        var dh = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDH == id && d.MaKH == User.LayMaTK());
        if (dh == null) return NotFound();
        if (dh.DaThanhToan || dh.TrangThai == TrangThaiDon.DaHuy) return RedirectToAction(nameof(ChiTiet), new { id });

        if (!_vnPay.DaCauHinh)
        {
            TempData["Loi"] = "Chưa cấu hình VNPay sandbox (TmnCode/HashSecret trong appsettings.json). Đơn hàng vẫn được ghi nhận.";
            return RedirectToAction(nameof(ThanhCong), new { id });
        }

        var returnUrl = Url.Action("VnPayReturn", "ThanhToan", null, Request.Scheme)!;
        var ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        return Redirect(_vnPay.TaoUrlThanhToan(dh.MaDH, dh.TongTien, ip, returnUrl));
    }

    public async Task<IActionResult> ThanhCong(int id)
    {
        var dh = await _db.DonHangs.AsNoTracking()
            .FirstOrDefaultAsync(d => d.MaDH == id && d.MaKH == User.LayMaTK());
        if (dh == null) return NotFound();
        return View(dh);
    }

    public async Task<IActionResult> LichSu(int page = 1)
    {
        var maKH = User.LayMaTK();
        var kq = await _db.DonHangs.AsNoTracking()
            .Where(d => d.MaKH == maKH)
            .OrderByDescending(d => d.NgayDat)
            .ToPagedAsync(page, 10);
        return View(kq);
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var dh = await _db.DonHangs.AsNoTracking()
            .Include(d => d.ChiTietDonHangs).ThenInclude(c => c.PhienBan).ThenInclude(p => p!.SanPham)
            .FirstOrDefaultAsync(d => d.MaDH == id && d.MaKH == User.LayMaTK());
        if (dh == null) return NotFound();
        return View(dh);
    }

    /// <summary>Khách chỉ được hủy khi đơn còn "Chờ xác nhận"; hoàn lại tồn kho.</summary>
    [HttpPost]
    public async Task<IActionResult> Huy(int id)
    {
        var dh = await _db.DonHangs.Include(d => d.ChiTietDonHangs)
            .FirstOrDefaultAsync(d => d.MaDH == id && d.MaKH == User.LayMaTK());
        if (dh == null) return NotFound();
        if (dh.TrangThai != TrangThaiDon.ChoXacNhan)
        {
            TempData["Loi"] = "Chỉ hủy được đơn đang chờ xác nhận";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        await TonKho.HoanAsync(_db, dh);
        dh.TrangThai = TrangThaiDon.DaHuy;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = $"Đã hủy đơn #{id}";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }
}
