using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Models.ViewModels;

namespace QLDienThoai.Controllers;

public class TaiKhoanController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<TaiKhoan> _hasher;

    public TaiKhoanController(AppDbContext db, IPasswordHasher<TaiKhoan> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    [HttpGet]
    public IActionResult DangNhap(string? returnUrl = null) => View(new DangNhapVM { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> DangNhap(DangNhapVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == vm.TenDangNhap.Trim());
        var ketQua = tk == null ? PasswordVerificationResult.Failed : _hasher.VerifyHashedPassword(tk, tk.MatKhauHash, vm.MatKhau);
        if (tk == null || ketQua == PasswordVerificationResult.Failed)
        {
            // Thông báo chung, không tiết lộ tài khoản có tồn tại hay không
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
            return View(vm);
        }
        if (tk.BiKhoa)
        {
            ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
            return View(vm);
        }
        if (ketQua == PasswordVerificationResult.SuccessRehashNeeded)
        {
            tk.MatKhauHash = _hasher.HashPassword(tk, vm.MatKhau);
            await _db.SaveChangesAsync();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, tk.MaTK.ToString()),
            new(ClaimTypes.Name, tk.TenDangNhap),
            new("HoTen", tk.HoTen),
            new(ClaimTypes.Role, tk.VaiTro)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = vm.GhiNho });

        if (Url.IsLocalUrl(vm.ReturnUrl)) return LocalRedirect(vm.ReturnUrl);
        return tk.VaiTro == VaiTro.KhachHang
            ? RedirectToAction("Index", "Home")
            : RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [HttpGet]
    public IActionResult DangKy() => View(new DangKyVM());

    [HttpPost]
    public async Task<IActionResult> DangKy(DangKyVM vm)
    {
        if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == vm.TenDangNhap))
            ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
        if (!ModelState.IsValid) return View(vm);

        var tk = new TaiKhoan
        {
            TenDangNhap = vm.TenDangNhap.Trim(),
            HoTen = vm.HoTen.Trim(),
            SoDienThoai = vm.SoDienThoai.Trim(),
            Email = vm.Email?.Trim(),
            VaiTro = VaiTro.KhachHang   // đăng ký công khai luôn là khách hàng
        };
        tk.MatKhauHash = _hasher.HashPassword(tk, vm.MatKhau);
        _db.TaiKhoans.Add(tk);
        await _db.SaveChangesAsync();

        TempData["ThongBao"] = "Đăng ký thành công, mời bạn đăng nhập";
        return RedirectToAction(nameof(DangNhap));
    }

    [HttpPost]
    public async Task<IActionResult> DangXuat()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult TuChoi() => View();
}
