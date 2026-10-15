using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Data;

namespace QLDienThoai.Controllers;

public class KhuyenMaiController : Controller
{
    private readonly AppDbContext _db;

    public KhuyenMaiController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.KhuyenMais.AsNoTracking()
            .Where(k => k.HienThi && k.NgayKetThuc >= DateTime.Today)
            .OrderByDescending(k => k.NgayBatDau).ToListAsync());

    public async Task<IActionResult> ChiTiet(int id)
    {
        var km = await _db.KhuyenMais.AsNoTracking().FirstOrDefaultAsync(k => k.MaKM == id && k.HienThi);
        return km == null ? NotFound() : View(km);
    }
}
