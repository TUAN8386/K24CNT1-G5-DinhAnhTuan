using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;
using QLDienThoai.Models.ViewModels;
using QLDienThoai.Services;

namespace QLDienThoai.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly DaXemService _daXem;

    public HomeController(AppDbContext db, DaXemService daXem)
    {
        _db = db;
        _daXem = daXem;
    }

    public async Task<IActionResult> Index()
    {
        var idsDaXem = _daXem.LayDanhSach();
        var daXem = await _db.SanPhams.Where(s => idsDaXem.Contains(s.MaSP)).ToThe().ToListAsync();

        var vm = new TrangChuVM
        {
            MoiNhat = await _db.SanPhams.OrderByDescending(s => s.NgayTao).Take(8).ToThe().ToListAsync(),
            BanChay = await _db.SanPhams.SapXepBanChay().Take(4).ToThe().ToListAsync(),
            DaXem = idsDaXem.Select(id => daXem.FirstOrDefault(x => x.MaSP == id)).Where(x => x != null).Select(x => x!).ToList(),
            KhuyenMais = await _db.KhuyenMais
                .Where(k => k.HienThi && k.NgayBatDau <= DateTime.Today && k.NgayKetThuc >= DateTime.Today)
                .OrderByDescending(k => k.NgayBatDau).Take(3).ToListAsync(),
            Hangs = await _db.HangSanXuats.OrderBy(h => h.TenHang).ToListAsync()
        };
        return View(vm);
    }

    public IActionResult Error() => View();
}
