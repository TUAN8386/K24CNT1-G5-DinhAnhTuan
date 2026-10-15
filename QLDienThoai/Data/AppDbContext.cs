using Microsoft.EntityFrameworkCore;
using QLDienThoai.Models;

namespace QLDienThoai.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<HangSanXuat> HangSanXuats => Set<HangSanXuat>();
    public DbSet<SanPham> SanPhams => Set<SanPham>();
    public DbSet<PhienBan> PhienBans => Set<PhienBan>();
    public DbSet<DonHang> DonHangs => Set<DonHang>();
    public DbSet<ChiTietDonHang> ChiTietDonHangs => Set<ChiTietDonHang>();
    public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
    public DbSet<DanhGia> DanhGias => Set<DanhGia>();
    public DbSet<KhuyenMai> KhuyenMais => Set<KhuyenMai>();
    public DbSet<HinhAnhSanPham> HinhAnhSanPhams => Set<HinhAnhSanPham>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<HangSanXuat>(e =>
        {
            e.ToTable("HangSanXuat");
            e.HasIndex(x => x.TenHang).IsUnique();
        });

        mb.Entity<SanPham>(e =>
        {
            e.ToTable("SanPham");
            e.Property(x => x.NgayTao).HasDefaultValueSql("GETDATE()");
            // Không cho xóa hãng khi còn sản phẩm
            e.HasOne(x => x.HangSanXuat).WithMany(h => h.SanPhams)
                .HasForeignKey(x => x.MaHang).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<PhienBan>(e =>
        {
            e.ToTable("PhienBan", t =>
            {
                t.HasCheckConstraint("CK_PhienBan_GiaBan", "[GiaBan] > 0");
                t.HasCheckConstraint("CK_PhienBan_SoLuongTon", "[SoLuongTon] >= 0");
            });
            e.HasOne(x => x.SanPham).WithMany(s => s.PhienBans)
                .HasForeignKey(x => x.MaSP).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.MaSP, x.DungLuong, x.MauSac }).IsUnique();
        });

        mb.Entity<TaiKhoan>(e =>
        {
            e.ToTable("TaiKhoan", t =>
                t.HasCheckConstraint("CK_TaiKhoan_VaiTro", "[VaiTro] IN (N'Admin', N'NhanVien', N'KhachHang')"));
            e.HasIndex(x => x.TenDangNhap).IsUnique();
            e.Property(x => x.NgayTao).HasDefaultValueSql("GETDATE()");
        });

        mb.Entity<DonHang>(e =>
        {
            e.ToTable("DonHang", t => t.HasCheckConstraint("CK_DonHang_TongTien", "[TongTien] >= 0"));
            e.Property(x => x.NgayDat).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.KhachHang).WithMany(k => k.DonHangs)
                .HasForeignKey(x => x.MaKH).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<ChiTietDonHang>(e =>
        {
            e.ToTable("ChiTietDonHang", t =>
            {
                t.HasCheckConstraint("CK_CTDH_SoLuong", "[SoLuong] > 0");
                t.HasCheckConstraint("CK_CTDH_IMEI", "[IMEI] IS NULL OR (LEN([IMEI]) = 15 AND [IMEI] NOT LIKE '%[^0-9]%')");
            });
            e.HasOne(x => x.DonHang).WithMany(d => d.ChiTietDonHangs)
                .HasForeignKey(x => x.MaDH).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PhienBan).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(x => x.MaPB).OnDelete(DeleteBehavior.Restrict);
            // IMEI không trùng (cho phép nhiều dòng chưa gán IMEI)
            e.HasIndex(x => x.IMEI).IsUnique().HasFilter("[IMEI] IS NOT NULL");
        });

        mb.Entity<DanhGia>(e =>
        {
            e.ToTable("DanhGia", t => t.HasCheckConstraint("CK_DanhGia_Diem", "[Diem] BETWEEN 1 AND 5"));
            e.HasOne(x => x.SanPham).WithMany(s => s.DanhGias)
                .HasForeignKey(x => x.MaSP).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TaiKhoan).WithMany()
                .HasForeignKey(x => x.MaTK).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.MaSP, x.MaTK }).IsUnique();
        });

        mb.Entity<KhuyenMai>(e => e.ToTable("KhuyenMai"));

        // Thư viện ảnh sản phẩm: xóa sản phẩm thì xóa luôn các ảnh phụ
        mb.Entity<HinhAnhSanPham>(e =>
        {
            e.ToTable("HinhAnhSanPham");
            e.HasOne(x => x.SanPham).WithMany(s => s.HinhAnhs)
                .HasForeignKey(x => x.MaSP).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.MaSP);
        });
    }
}
