using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;
using QLDienThoai.Services;

namespace QLDienThoai.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class TaiKhoanController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<TaiKhoan> _hasher;

    public TaiKhoanController(AppDbContext db, IPasswordHasher<TaiKhoan> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<IActionResult> Index(string? q, string? vaiTro, int page = 1)
    {
        ViewBag.Q = q;
        ViewBag.VaiTro = vaiTro;
        var query = _db.TaiKhoans.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.TenDangNhap.Contains(q) || t.HoTen.Contains(q) || t.SoDienThoai.Contains(q));
        if (!string.IsNullOrEmpty(vaiTro)) query = query.Where(t => t.VaiTro == vaiTro);
        return View(await query.OrderBy(t => t.VaiTro).ThenBy(t => t.TenDangNhap).ToPagedAsync(page, 15));
    }

    public async Task<IActionResult> Details(int id)
    {
        var tk = await _db.TaiKhoans.AsNoTracking().Include(t => t.DonHangs).FirstOrDefaultAsync(t => t.MaTK == id);
        return tk == null ? NotFound() : View(tk);
    }

    public IActionResult Create() => View(new TaiKhoanFormVM());

    [HttpPost]
    public async Task<IActionResult> Create(TaiKhoanFormVM vm)
    {
        if (string.IsNullOrEmpty(vm.MatKhau)) ModelState.AddModelError(nameof(vm.MatKhau), "Vui lòng nhập mật khẩu");
        await KiemTraAsync(vm);
        if (!ModelState.IsValid) return View(vm);

        var tk = new TaiKhoan
        {
            TenDangNhap = vm.TenDangNhap.Trim(), HoTen = vm.HoTen.Trim(), SoDienThoai = vm.SoDienThoai,
            Email = vm.Email, VaiTro = vm.VaiTro, BiKhoa = vm.BiKhoa
        };
        tk.MatKhauHash = _hasher.HashPassword(tk, vm.MatKhau!);
        _db.TaiKhoans.Add(tk);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã tạo tài khoản " + tk.TenDangNhap;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var tk = await _db.TaiKhoans.FindAsync(id);
        if (tk == null) return NotFound();
        return View(new TaiKhoanFormVM
        {
            MaTK = tk.MaTK, TenDangNhap = tk.TenDangNhap, HoTen = tk.HoTen, SoDienThoai = tk.SoDienThoai,
            Email = tk.Email, VaiTro = tk.VaiTro, BiKhoa = tk.BiKhoa
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, TaiKhoanFormVM vm)
    {
        if (id != vm.MaTK) return BadRequest();
        var tk = await _db.TaiKhoans.FindAsync(id);
        if (tk == null) return NotFound();
        await KiemTraAsync(vm);
        if (id == User.LayMaTK() && (vm.VaiTro != VaiTro.Admin || vm.BiKhoa))
            ModelState.AddModelError("", "Không thể tự hạ quyền hoặc tự khóa tài khoản đang đăng nhập");
        if (!ModelState.IsValid) return View(vm);

        tk.TenDangNhap = vm.TenDangNhap.Trim();
        tk.HoTen = vm.HoTen.Trim();
        tk.SoDienThoai = vm.SoDienThoai;
        tk.Email = vm.Email;
        tk.VaiTro = vm.VaiTro;
        tk.BiKhoa = vm.BiKhoa;
        if (!string.IsNullOrEmpty(vm.MatKhau)) tk.MatKhauHash = _hasher.HashPassword(tk, vm.MatKhau);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã cập nhật tài khoản " + tk.TenDangNhap;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DoiKhoa(int id)
    {
        var tk = await _db.TaiKhoans.FindAsync(id);
        if (tk == null) return NotFound();
        if (id == User.LayMaTK())
        {
            TempData["Loi"] = "Không thể tự khóa tài khoản đang đăng nhập";
            return RedirectToAction(nameof(Index));
        }
        tk.BiKhoa = !tk.BiKhoa;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = (tk.BiKhoa ? "Đã khóa " : "Đã mở khóa ") + tk.TenDangNhap;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var tk = await _db.TaiKhoans.AsNoTracking().Include(t => t.DonHangs).FirstOrDefaultAsync(t => t.MaTK == id);
        return tk == null ? NotFound() : View(tk);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var tk = await _db.TaiKhoans.FindAsync(id);
        if (tk == null) return NotFound();
        if (id == User.LayMaTK())
        {
            TempData["Loi"] = "Không thể xóa tài khoản đang đăng nhập";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.DonHangs.AnyAsync(d => d.MaKH == id) || await _db.DanhGias.AnyAsync(d => d.MaTK == id))
        {
            TempData["Loi"] = $"Tài khoản {tk.TenDangNhap} đã có đơn hàng/đánh giá – hãy khóa thay vì xóa";
            return RedirectToAction(nameof(Index));
        }
        _db.TaiKhoans.Remove(tk);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã xóa tài khoản " + tk.TenDangNhap;
        return RedirectToAction(nameof(Index));
    }

    private async Task KiemTraAsync(TaiKhoanFormVM vm)
    {
        if (!VaiTro.TatCa.Contains(vm.VaiTro)) ModelState.AddModelError(nameof(vm.VaiTro), "Vai trò không hợp lệ");
        if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == vm.TenDangNhap && t.MaTK != vm.MaTK))
            ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
    }
}
