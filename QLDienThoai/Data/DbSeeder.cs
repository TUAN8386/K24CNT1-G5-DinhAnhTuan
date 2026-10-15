using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLDienThoai.Models;

namespace QLDienThoai.Data;

/// <summary>Tạo CSDL (nếu chưa có) và dữ liệu mẫu để chạy thử.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<TaiKhoan>>();

        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        await db.Database.EnsureCreatedAsync();
        await TaoBangHinhAnhAsync(db);
        if (await db.TaiKhoans.AnyAsync())
        {
            // CSDL đã có dữ liệu (tạo bằng script hoặc lần chạy trước): chỉ bổ sung ảnh mẫu còn thiếu
            await BoSungAnhMauAsync(db, env);
            return;
        }

        // ---------- Tài khoản ----------
        TaiKhoan TK(string user, string pass, string ten, string sdt, string vaiTro, string? email = null)
        {
            var tk = new TaiKhoan { TenDangNhap = user, HoTen = ten, SoDienThoai = sdt, VaiTro = vaiTro, Email = email };
            tk.MatKhauHash = hasher.HashPassword(tk, pass);
            return tk;
        }

        var admin = TK("admin", "Admin@123", "Quản Trị Viên", "0901000001", VaiTro.Admin, "admin@qldienthoai.vn");
        var nv = TK("nhanvien", "Nhanvien@123", "Nguyễn Văn Bán", "0901000002", VaiTro.NhanVien);
        var kh1 = TK("khachhang", "Khachhang@123", "Trần Thị Mua", "0912345678", VaiTro.KhachHang, "khachhang@example.com");
        var kh2 = TK("lehoang", "Khachhang@123", "Lê Văn Hoàng", "0987654321", VaiTro.KhachHang);
        db.TaiKhoans.AddRange(admin, nv, kh1, kh2);

        // ---------- Hãng ----------
        var apple = new HangSanXuat { TenHang = "Apple", QuocGia = "Mỹ" };
        var samsung = new HangSanXuat { TenHang = "Samsung", QuocGia = "Hàn Quốc" };
        var xiaomi = new HangSanXuat { TenHang = "Xiaomi", QuocGia = "Trung Quốc" };
        var oppo = new HangSanXuat { TenHang = "OPPO", QuocGia = "Trung Quốc" };
        var vivo = new HangSanXuat { TenHang = "vivo", QuocGia = "Trung Quốc" };
        db.HangSanXuats.AddRange(apple, samsung, xiaomi, oppo, vivo);

        // ---------- Sản phẩm + phiên bản ----------
        SanPham SP(string ten, HangSanXuat hang, string mh, string chip, string cam, string pin, string os, int ngayTruoc,
            params (string dl, string mau, decimal gia, int ton)[] pbs)
        {
            var sp = new SanPham
            {
                TenSP = ten, HangSanXuat = hang, ManHinh = mh, Chip = chip, Camera = cam, Pin = pin, HeDieuHanh = os,
                NgayTao = DateTime.Now.AddDays(-ngayTruoc),
                MoTa = $"<p><strong>{ten}</strong> chính hãng, bảo hành 12 tháng.</p><ul><li>Màn hình: {mh}</li><li>Chip: {chip}</li><li>Camera: {cam}</li><li>Pin: {pin}</li></ul>"
            };
            foreach (var p in pbs)
                sp.PhienBans.Add(new PhienBan { DungLuong = p.dl, MauSac = p.mau, GiaBan = p.gia, SoLuongTon = p.ton });
            return sp;
        }

        var ip16pm = SP("iPhone 16 Pro Max", apple, "6.9\" Super Retina XDR 120Hz", "Apple A18 Pro", "48MP + 48MP + 12MP", "4685 mAh", "iOS 18", 60,
            ("256GB", "Titan Sa Mạc", 30990000, 15), ("256GB", "Titan Đen", 30990000, 10), ("512GB", "Titan Tự Nhiên", 37490000, 6), ("1TB", "Titan Trắng", 43990000, 3));
        var ip16 = SP("iPhone 16", apple, "6.1\" Super Retina XDR", "Apple A18", "48MP + 12MP", "3561 mAh", "iOS 18", 55,
            ("128GB", "Đen", 19990000, 20), ("128GB", "Hồng", 19990000, 12), ("256GB", "Xanh Lưu Ly", 22990000, 8));
        var ip15 = SP("iPhone 15", apple, "6.1\" Super Retina XDR", "Apple A16 Bionic", "48MP + 12MP", "3349 mAh", "iOS 17", 300,
            ("128GB", "Đen", 16490000, 9), ("256GB", "Xanh Lá", 19190000, 2));
        var s25u = SP("Samsung Galaxy S25 Ultra", samsung, "6.9\" Dynamic AMOLED 2X 120Hz", "Snapdragon 8 Elite for Galaxy", "200MP + 50MP + 50MP + 10MP", "5000 mAh", "Android 15", 30,
            ("256GB", "Xám Titan", 29990000, 10), ("512GB", "Đen Titan", 33990000, 5), ("1TB", "Xanh Titan", 41990000, 2));
        var a56 = SP("Samsung Galaxy A56 5G", samsung, "6.7\" Super AMOLED 120Hz", "Exynos 1580", "50MP + 12MP + 5MP", "5000 mAh", "Android 15", 20,
            ("128GB", "Xám", 9490000, 25), ("256GB", "Hồng", 10490000, 14));
        var zflip = SP("Samsung Galaxy Z Flip6", samsung, "6.7\" Dynamic AMOLED 2X (gập)", "Snapdragon 8 Gen 3", "50MP + 12MP", "4000 mAh", "Android 14", 120,
            ("256GB", "Xanh Mint", 25990000, 4), ("512GB", "Vàng", 29990000, 0));
        var x15 = SP("Xiaomi 15", xiaomi, "6.36\" AMOLED 120Hz", "Snapdragon 8 Elite", "50MP x3 Leica", "5240 mAh", "Android 15 (HyperOS 2)", 15,
            ("256GB", "Đen", 22990000, 8), ("512GB", "Trắng", 24990000, 5));
        var rn14 = SP("Xiaomi Redmi Note 14 Pro", xiaomi, "6.67\" AMOLED 120Hz", "Helio G100 Ultra", "200MP + 8MP + 2MP", "5500 mAh", "Android 14", 90,
            ("128GB", "Tím", 7490000, 30), ("256GB", "Đen", 8290000, 18));
        var reno13 = SP("OPPO Reno13 F", oppo, "6.67\" AMOLED 120Hz", "Snapdragon 6 Gen 1", "50MP + 8MP + 2MP", "5800 mAh", "Android 15", 40,
            ("256GB", "Tím", 9990000, 16), ("256GB", "Xám", 9990000, 11));
        var findx8 = SP("OPPO Find X8", oppo, "6.59\" AMOLED 120Hz", "Dimensity 9400", "50MP x3 Hasselblad", "5630 mAh", "Android 15", 70,
            ("512GB", "Đen", 22990000, 6));
        var v40 = SP("vivo V40", vivo, "6.78\" AMOLED 120Hz", "Snapdragon 7 Gen 3", "50MP + 50MP ZEISS", "5500 mAh", "Android 14", 150,
            ("256GB", "Tím Tinh Vân", 12990000, 9), ("512GB", "Xám Thiên Hà", 14990000, 4));
        var y29 = SP("vivo Y29", vivo, "6.68\" LCD 120Hz", "Dimensity 6300", "50MP + 0.08MP", "5500 mAh", "Android 14", 10,
            ("128GB", "Trắng", 4990000, 40));

        var spList = new[] { ip16pm, ip16, ip15, s25u, a56, zflip, x15, rn14, reno13, findx8, v40, y29 };
        db.SanPhams.AddRange(spList);
        await db.SaveChangesAsync();

        // ---------- Đơn hàng mẫu (để có thống kê + tra cứu bảo hành) ----------
        var rnd = new Random(17);
        long imeiBase = 351234560000000;
        var tatCaPB = spList.SelectMany(s => s.PhienBans).ToList();
        var khachs = new[] { kh1, kh2 };

        for (int i = 0; i < 24; i++)
        {
            var ngay = DateTime.Today.AddDays(-rnd.Next(5, 330)).AddHours(rnd.Next(8, 21));
            var kh = khachs[i % 2];
            var dh = new DonHang
            {
                MaKH = kh.MaTK, NgayDat = ngay, HoTenNhan = kh.HoTen, SoDienThoaiNhan = kh.SoDienThoai,
                DiaChiGiao = i % 2 == 0 ? "12 Nguyễn Trãi, Thanh Xuân, Hà Nội" : "45 Lê Lợi, Quận 1, TP. Hồ Chí Minh",
                PhuongThucTT = i % 3 == 0 ? PhuongThucThanhToan.VNPay : PhuongThucThanhToan.COD,
                TrangThai = TrangThaiDon.HoanThanh, DaThanhToan = true
            };
            int soDong = rnd.Next(1, 3);
            for (int j = 0; j < soDong; j++)
            {
                var pb = tatCaPB[rnd.Next(tatCaPB.Count)];
                dh.ChiTietDonHangs.Add(new ChiTietDonHang
                {
                    MaPB = pb.MaPB, SoLuong = 1, DonGia = pb.GiaBan, IMEI = (imeiBase++).ToString()
                });
            }
            dh.TongTien = dh.ChiTietDonHangs.Sum(c => c.DonGia * c.SoLuong);
            db.DonHangs.Add(dh);
        }

        // Một đơn đang chờ xác nhận để nhân viên xử lý
        var dhCho = new DonHang
        {
            MaKH = kh1.MaTK, HoTenNhan = kh1.HoTen, SoDienThoaiNhan = kh1.SoDienThoai,
            DiaChiGiao = "12 Nguyễn Trãi, Thanh Xuân, Hà Nội", PhuongThucTT = PhuongThucThanhToan.COD,
            TrangThai = TrangThaiDon.ChoXacNhan
        };
        var pbCho = ip16.PhienBans.First();
        dhCho.ChiTietDonHangs.Add(new ChiTietDonHang { MaPB = pbCho.MaPB, SoLuong = 1, DonGia = pbCho.GiaBan });
        dhCho.TongTien = pbCho.GiaBan;
        db.DonHangs.Add(dhCho);

        // ---------- Đánh giá ----------
        db.DanhGias.AddRange(
            new DanhGia { MaSP = ip16pm.MaSP, MaTK = kh1.MaTK, Diem = 5, NoiDung = "<p>Máy rất mượt, <strong>camera chụp đêm</strong> xuất sắc.</p>", NgayDanhGia = DateTime.Now.AddDays(-20) },
            new DanhGia { MaSP = ip16pm.MaSP, MaTK = kh2.MaTK, Diem = 4, NoiDung = "<p>Pin tốt, nhưng giá hơi cao.</p>", NgayDanhGia = DateTime.Now.AddDays(-12) },
            new DanhGia { MaSP = s25u.MaSP, MaTK = kh1.MaTK, Diem = 5, NoiDung = "<p>Bút S Pen tiện, zoom 100x ấn tượng.</p>", NgayDanhGia = DateTime.Now.AddDays(-8) },
            new DanhGia { MaSP = rn14.MaSP, MaTK = kh2.MaTK, Diem = 4, NoiDung = "<p>Giá rẻ cấu hình ổn, pin trâu.</p>", NgayDanhGia = DateTime.Now.AddDays(-30) }
        );

        // ---------- Khuyến mãi ----------
        db.KhuyenMais.AddRange(
            new KhuyenMai
            {
                TieuDe = "Trả góp 0% lãi suất cho iPhone 16 Series",
                NoiDung = "<p>Áp dụng trả góp <strong>0%</strong> kỳ hạn 6–12 tháng, trả trước từ 10%.</p>",
                NgayBatDau = DateTime.Today.AddDays(-5), NgayKetThuc = DateTime.Today.AddDays(25)
            },
            new KhuyenMai
            {
                TieuDe = "Thu cũ đổi mới – trợ giá đến 2 triệu",
                NoiDung = "<p>Mang máy cũ đến cửa hàng để được thẩm định và <em>trợ giá lên đời</em> Samsung Galaxy S25.</p>",
                NgayBatDau = DateTime.Today.AddDays(-2), NgayKetThuc = DateTime.Today.AddDays(30)
            });

        await db.SaveChangesAsync();
        await BoSungAnhMauAsync(db, env);
    }

    /// <summary>
    /// Tạo bảng HinhAnhSanPham cho CSDL đã tạo từ phiên bản trước (EnsureCreated không tự thêm bảng mới).
    /// </summary>
    private static async Task TaoBangHinhAnhAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.HinhAnhSanPham', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HinhAnhSanPham (
        MaHA     INT IDENTITY(1,1) NOT NULL,
        MaSP     INT               NOT NULL,
        DuongDan NVARCHAR(300)     NOT NULL,
        ThuTu    INT               NOT NULL CONSTRAINT DF_HinhAnhSanPham_ThuTu DEFAULT (0),
        CONSTRAINT PK_HinhAnhSanPham PRIMARY KEY (MaHA),
        CONSTRAINT FK_HinhAnhSanPham_SanPham_MaSP FOREIGN KEY (MaSP) REFERENCES dbo.SanPham(MaSP) ON DELETE CASCADE
    );
    CREATE INDEX IX_HinhAnhSanPham_MaSP ON dbo.HinhAnhSanPham(MaSP);
END");
    }

    /// <summary>Ảnh mẫu có sẵn trong wwwroot/images cho 12 sản phẩm và 5 hãng của dữ liệu mẫu.</summary>
    private static readonly Dictionary<string, string> AnhMauSanPham = new()
    {
        ["iPhone 16 Pro Max"] = "iphone-16-pro-max",
        ["iPhone 16"] = "iphone-16",
        ["iPhone 15"] = "iphone-15",
        ["Samsung Galaxy S25 Ultra"] = "samsung-galaxy-s25-ultra",
        ["Samsung Galaxy A56 5G"] = "samsung-galaxy-a56-5g",
        ["Samsung Galaxy Z Flip6"] = "samsung-galaxy-z-flip6",
        ["Xiaomi 15"] = "xiaomi-15",
        ["Xiaomi Redmi Note 14 Pro"] = "xiaomi-redmi-note-14-pro",
        ["OPPO Reno13 F"] = "oppo-reno13-f",
        ["OPPO Find X8"] = "oppo-find-x8",
        ["vivo V40"] = "vivo-v40",
        ["vivo Y29"] = "vivo-y29",
    };

    /// <summary>
    /// Gán ảnh mẫu cho sản phẩm/hãng CHƯA có ảnh (không ghi đè ảnh quản trị viên đã tải lên)
    /// và thêm 1 ảnh mặt sau vào thư viện ảnh nếu sản phẩm chưa có ảnh phụ nào.
    /// </summary>
    private static async Task BoSungAnhMauAsync(AppDbContext db, IWebHostEnvironment env)
    {
        var goc = string.IsNullOrEmpty(env.WebRootPath) ? Path.Combine(env.ContentRootPath, "wwwroot") : env.WebRootPath;
        bool Co(string webPath) => File.Exists(Path.Combine(goc, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        var tenSP = AnhMauSanPham.Keys.ToList();
        var sps = await db.SanPhams.Include(s => s.HinhAnhs).Where(s => tenSP.Contains(s.TenSP)).ToListAsync();
        foreach (var sp in sps)
        {
            var slug = AnhMauSanPham[sp.TenSP];
            var chinh = $"/images/sanpham/{slug}.png";
            var sau = $"/images/sanpham/{slug}-2.png";
            if (string.IsNullOrEmpty(sp.AnhDaiDien) && Co(chinh)) sp.AnhDaiDien = chinh;
            if (sp.HinhAnhs.Count == 0 && Co(sau)) sp.HinhAnhs.Add(new HinhAnhSanPham { DuongDan = sau, ThuTu = 1 });
        }
        foreach (var h in await db.HangSanXuats.Where(h => h.Logo == null || h.Logo == "").ToListAsync())
        {
            var logo = $"/images/hang/logo-{h.TenHang.ToLowerInvariant()}.png";
            if (Co(logo)) h.Logo = logo;
        }
        await db.SaveChangesAsync();
    }
}