using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;
using QLDienThoai.Services;

namespace QLDienThoai.Areas.Admin.Controllers;

/// <summary>Tạo đơn tại quầy (admin/nhân viên lập đơn cho khách).</summary>
public class TaoDonVM
{
    [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
    [Display(Name = "Khách hàng")]
    public int MaKH { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phiên bản")]
    [Display(Name = "Phiên bản máy")]
    public int MaPB { get; set; }

    [Range(1, 10, ErrorMessage = "Số lượng từ 1 đến 10")]
    [Display(Name = "Số lượng")]
    public int SoLuong { get; set; } = 1;

    [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
    [StringLength(300, MinimumLength = 10, ErrorMessage = "Địa chỉ từ 10 đến 300 ký tự")]
    [Display(Name = "Địa chỉ giao")]
    public string DiaChiGiao { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Thanh toán")]
    public string PhuongThucTT { get; set; } = PhuongThucThanhToan.COD;
}

public class SuaDonVM
{
    public int MaDH { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
    [StringLength(100)]
    [Display(Name = "Người nhận")]
    public string HoTenNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoaiNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
    [StringLength(300, MinimumLength = 10, ErrorMessage = "Địa chỉ từ 10 đến 300 ký tự")]
    [Display(Name = "Địa chỉ giao")]
    public string DiaChiGiao { get; set; } = string.Empty;

    [Display(Name = "Đã thanh toán")]
    public bool DaThanhToan { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }
}

[Area("Admin")]
[Authorize(Roles = VaiTro.QuanLy)]
public class DonHangController : Controller
{
    private readonly AppDbContext _db;

    public DonHangController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, string? trangThai, DateTime? tuNgay, DateTime? denNgay, int page = 1)
    {
        ViewBag.Q = q;
        ViewBag.TrangThai = trangThai;
        ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
        ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

        var query = _db.DonHangs.AsNoTracking().Include(d => d.KhachHang).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var tk = q.Trim().TrimStart('#');
            if (int.TryParse(tk, out var ma))
                query = query.Where(d => d.MaDH == ma || d.SoDienThoaiNhan.Contains(tk));
            else
                query = query.Where(d => d.HoTenNhan.Contains(tk) || d.KhachHang!.TenDangNhap.Contains(tk));
        }
        if (!string.IsNullOrEmpty(trangThai)) query = query.Where(d => d.TrangThai == trangThai);
        if (tuNgay.HasValue) query = query.Where(d => d.NgayDat >= tuNgay.Value.Date);
        if (denNgay.HasValue) query = query.Where(d => d.NgayDat < denNgay.Value.Date.AddDays(1));

        return View(await query.OrderByDescending(d => d.NgayDat).ToPagedAsync(page, 12));
    }

    public async Task<IActionResult> Details(int id)
    {
        var dh = await LayDonDayDuAsync(id);
        return dh == null ? NotFound() : View(dh);
    }

    // ---------- Tạo đơn tại quầy ----------
    private async Task NapDuLieuTaoDonAsync()
    {
        ViewBag.MaKH = new SelectList(await _db.TaiKhoans.Where(t => t.VaiTro == VaiTro.KhachHang && !t.BiKhoa)
            .OrderBy(t => t.HoTen).Select(t => new { t.MaTK, Ten = t.HoTen + " – " + t.SoDienThoai }).ToListAsync(), "MaTK", "Ten");
        ViewBag.MaPB = new SelectList(await _db.PhienBans.Where(p => p.SoLuongTon > 0).OrderBy(p => p.SanPham!.TenSP)
            .Select(p => new { p.MaPB, Ten = p.SanPham!.TenSP + " " + p.DungLuong + " " + p.MauSac + " (còn " + p.SoLuongTon + ")" })
            .ToListAsync(), "MaPB", "Ten");
        ViewBag.PhuongThuc = new SelectList(PhuongThucThanhToan.TatCa);
    }

    public async Task<IActionResult> Create()
    {
        await NapDuLieuTaoDonAsync();
        return View(new TaoDonVM());
    }

    [HttpPost]
    public async Task<IActionResult> Create(TaoDonVM vm)
    {
        var kh = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == vm.MaKH && t.VaiTro == VaiTro.KhachHang);
        if (kh == null) ModelState.AddModelError(nameof(vm.MaKH), "Khách hàng không hợp lệ");
        var pb = await _db.PhienBans.FindAsync(vm.MaPB);
        if (pb == null) ModelState.AddModelError(nameof(vm.MaPB), "Phiên bản không tồn tại");
        else if (vm.SoLuong > pb.SoLuongTon) ModelState.AddModelError(nameof(vm.SoLuong), $"Chỉ còn {pb.SoLuongTon} máy");
        if (!PhuongThucThanhToan.TatCa.Contains(vm.PhuongThucTT)) ModelState.AddModelError(nameof(vm.PhuongThucTT), "Không hợp lệ");

        if (!ModelState.IsValid)
        {
            await NapDuLieuTaoDonAsync();
            return View(vm);
        }

        var dh = new DonHang
        {
            MaKH = kh!.MaTK, HoTenNhan = kh.HoTen, SoDienThoaiNhan = kh.SoDienThoai, DiaChiGiao = vm.DiaChiGiao,
            PhuongThucTT = vm.PhuongThucTT, TrangThai = TrangThaiDon.DaXacNhan, NgayDat = DateTime.Now,
            GhiChu = "Đơn lập tại quầy bởi " + User.Identity!.Name
        };
        for (int i = 0; i < vm.SoLuong; i++)
            dh.ChiTietDonHangs.Add(new ChiTietDonHang { MaPB = pb!.MaPB, SoLuong = 1, DonGia = pb.GiaBan });
        dh.TongTien = dh.ChiTietDonHangs.Sum(c => c.DonGia);
        pb!.SoLuongTon -= vm.SoLuong;

        _db.DonHangs.Add(dh);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = $"Đã tạo đơn #{dh.MaDH}. Hãy gán IMEI trước khi giao.";
        return RedirectToAction(nameof(GanImei), new { id = dh.MaDH });
    }

    // ---------- Sửa thông tin giao hàng ----------
    public async Task<IActionResult> Edit(int id)
    {
        var dh = await _db.DonHangs.FindAsync(id);
        if (dh == null) return NotFound();
        return View(new SuaDonVM
        {
            MaDH = dh.MaDH, HoTenNhan = dh.HoTenNhan, SoDienThoaiNhan = dh.SoDienThoaiNhan,
            DiaChiGiao = dh.DiaChiGiao, DaThanhToan = dh.DaThanhToan, GhiChu = dh.GhiChu
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, SuaDonVM vm)
    {
        if (id != vm.MaDH) return BadRequest();
        var dh = await _db.DonHangs.FindAsync(id);
        if (dh == null) return NotFound();
        if (dh.TrangThai is TrangThaiDon.HoanThanh or TrangThaiDon.DaHuy)
            ModelState.AddModelError("", "Không sửa được đơn đã hoàn thành hoặc đã hủy");
        if (!ModelState.IsValid) return View(vm);

        dh.HoTenNhan = vm.HoTenNhan;
        dh.SoDienThoaiNhan = vm.SoDienThoaiNhan;
        dh.DiaChiGiao = vm.DiaChiGiao;
        dh.DaThanhToan = vm.DaThanhToan;
        dh.GhiChu = vm.GhiChu;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = $"Đã cập nhật đơn #{id}";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Cập nhật tiến độ ----------
    [HttpPost]
    public async Task<IActionResult> CapNhatTrangThai(int id, string trangThaiMoi)
    {
        var dh = await _db.DonHangs.Include(d => d.ChiTietDonHangs).FirstOrDefaultAsync(d => d.MaDH == id);
        if (dh == null) return NotFound();

        if (!TrangThaiDon.ChuyenHopLe(dh.TrangThai, trangThaiMoi))
        {
            TempData["Loi"] = $"Không thể chuyển từ \"{dh.TrangThai}\" sang \"{trangThaiMoi}\"";
            return RedirectToAction(nameof(Details), new { id });
        }
        // Bắt buộc gán đủ IMEI trước khi giao hàng
        if (trangThaiMoi == TrangThaiDon.DangGiao && dh.ChiTietDonHangs.Any(c => string.IsNullOrEmpty(c.IMEI)))
        {
            TempData["Loi"] = "Phải gán IMEI cho tất cả máy trước khi chuyển sang \"Đang giao\"";
            return RedirectToAction(nameof(GanImei), new { id });
        }
        if (trangThaiMoi == TrangThaiDon.DaHuy)
            await TonKho.HoanAsync(_db, dh);
        if (trangThaiMoi == TrangThaiDon.HoanThanh && dh.PhuongThucTT == PhuongThucThanhToan.COD)
            dh.DaThanhToan = true;

        dh.TrangThai = trangThaiMoi;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = $"Đơn #{id} → {trangThaiMoi}";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Gán IMEI cho máy bán ra ----------
    public async Task<IActionResult> GanImei(int id)
    {
        var dh = await LayDonDayDuAsync(id);
        if (dh == null) return NotFound();
        if (dh.TrangThai is TrangThaiDon.DaHuy or TrangThaiDon.HoanThanh)
        {
            TempData["Loi"] = "Đơn đã kết thúc, không thể gán IMEI";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(new GanImeiVM
        {
            MaDH = id,
            Dongs = dh.ChiTietDonHangs.Select(c => new GanImeiDongVM
            {
                MaCT = c.MaCT, TenMay = c.PhienBan!.TenDayDu, IMEI = c.IMEI
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> GanImei(GanImeiVM vm)
    {
        var dh = await _db.DonHangs.Include(d => d.ChiTietDonHangs).ThenInclude(c => c.PhienBan).ThenInclude(p => p!.SanPham)
            .FirstOrDefaultAsync(d => d.MaDH == vm.MaDH);
        if (dh == null) return NotFound();

        // Trùng trong chính form
        var trungForm = vm.Dongs.Where(d => !string.IsNullOrEmpty(d.IMEI)).GroupBy(d => d.IMEI).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        // Trùng với IMEI đã bán ở dòng khác
        var maCTs = vm.Dongs.Select(d => d.MaCT).ToList();
        var imeis = vm.Dongs.Select(d => d.IMEI).Where(i => !string.IsNullOrEmpty(i)).ToList();
        var daDung = await _db.ChiTietDonHangs.Where(c => c.IMEI != null && imeis.Contains(c.IMEI) && !maCTs.Contains(c.MaCT))
            .Select(c => c.IMEI!).ToListAsync();

        for (int i = 0; i < vm.Dongs.Count; i++)
        {
            var d = vm.Dongs[i];
            d.TenMay = dh.ChiTietDonHangs.FirstOrDefault(c => c.MaCT == d.MaCT)?.PhienBan?.TenDayDu ?? "";
            if (string.IsNullOrEmpty(d.IMEI)) continue;
            if (trungForm.Contains(d.IMEI)) ModelState.AddModelError($"Dongs[{i}].IMEI", "IMEI bị nhập trùng trong đơn");
            else if (daDung.Contains(d.IMEI)) ModelState.AddModelError($"Dongs[{i}].IMEI", "IMEI đã được gán cho máy khác");
        }
        if (!ModelState.IsValid) return View(vm);

        foreach (var d in vm.Dongs)
        {
            var ct = dh.ChiTietDonHangs.FirstOrDefault(c => c.MaCT == d.MaCT);
            if (ct != null) ct.IMEI = d.IMEI;
        }
        if (dh.TrangThai == TrangThaiDon.ChoXacNhan) dh.TrangThai = TrangThaiDon.DaXacNhan;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = $"Đã gán IMEI cho đơn #{dh.MaDH}";
        return RedirectToAction(nameof(Details), new { id = dh.MaDH });
    }

    // ---------- Xóa (chỉ Admin, chỉ đơn đã hủy) ----------
    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var dh = await LayDonDayDuAsync(id);
        return dh == null ? NotFound() : View(dh);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var dh = await _db.DonHangs.FindAsync(id);
        if (dh == null) return NotFound();
        if (dh.TrangThai != TrangThaiDon.DaHuy)
        {
            TempData["Loi"] = "Chỉ được xóa đơn đã hủy (để giữ lịch sử bán hàng và bảo hành)";
            return RedirectToAction(nameof(Details), new { id });
        }
        _db.DonHangs.Remove(dh);   // chi tiết xóa theo (cascade)
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = $"Đã xóa đơn #{id}";
        return RedirectToAction(nameof(Index));
    }

    private Task<DonHang?> LayDonDayDuAsync(int id) =>
        _db.DonHangs.Include(d => d.KhachHang)
            .Include(d => d.ChiTietDonHangs).ThenInclude(c => c.PhienBan).ThenInclude(p => p!.SanPham)
            .FirstOrDefaultAsync(d => d.MaDH == id);
}
