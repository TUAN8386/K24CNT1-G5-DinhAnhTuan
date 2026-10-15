/* =====================================================================
   ĐỀ 17 – WEBSITE BÁN ĐIỆN THOẠI DI ĐỘNG
   Script tạo CSDL SQL Server: QLDienThoaiDb
   - 6 bảng chính theo đề: HangSanXuat, SanPham, PhienBan, DonHang, ChiTietDonHang, TaiKhoan
   - 3 bảng bổ sung: DanhGia (điểm 1–5 + bài viết), KhuyenMai (tin khuyến mãi),
     HinhAnhSanPham (thư viện ảnh phụ của sản phẩm)
   - Cấu trúc khớp với AppDbContext (EF Core). Có thể dùng script này HOẶC để ứng dụng tự tạo
     (Database.EnsureCreated + DbSeeder) – không cần chạy cả hai.
   Tài khoản mẫu:  admin / Admin@123   nhanvien / Nhanvien@123   khachhang / Khachhang@123
   ===================================================================== */

USE master;
GO
IF DB_ID(N'QLDienThoaiDb') IS NOT NULL
BEGIN
    ALTER DATABASE QLDienThoaiDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QLDienThoaiDb;
END
GO
CREATE DATABASE QLDienThoaiDb COLLATE Vietnamese_CI_AS;
GO
USE QLDienThoaiDb;
GO

/* ---------------------------- 1. HangSanXuat ---------------------------- */
CREATE TABLE HangSanXuat (
    MaHang   INT IDENTITY(1,1) NOT NULL,
    TenHang  NVARCHAR(100)     NOT NULL,
    QuocGia  NVARCHAR(100)     NULL,
    Logo     NVARCHAR(300)     NULL,
    CONSTRAINT PK_HangSanXuat PRIMARY KEY (MaHang),
    CONSTRAINT UQ_HangSanXuat_TenHang UNIQUE (TenHang)
);
GO

/* ------------------------------ 2. SanPham ------------------------------ */
CREATE TABLE SanPham (
    MaSP        INT IDENTITY(1,1) NOT NULL,
    TenSP       NVARCHAR(200)     NOT NULL,
    MaHang      INT               NOT NULL,
    ManHinh     NVARCHAR(100)     NULL,
    Chip        NVARCHAR(100)     NULL,
    Camera      NVARCHAR(150)     NULL,
    Pin         NVARCHAR(50)      NULL,
    HeDieuHanh  NVARCHAR(50)      NULL,
    AnhDaiDien  NVARCHAR(300)     NULL,
    MoTa        NVARCHAR(MAX)     NULL,
    NgayTao     DATETIME2         NOT NULL CONSTRAINT DF_SanPham_NgayTao DEFAULT (GETDATE()),
    CONSTRAINT PK_SanPham PRIMARY KEY (MaSP),
    -- Không cho xóa hãng khi còn sản phẩm
    CONSTRAINT FK_SanPham_HangSanXuat_MaHang FOREIGN KEY (MaHang) REFERENCES HangSanXuat(MaHang) ON DELETE NO ACTION
);
CREATE INDEX IX_SanPham_MaHang ON SanPham(MaHang);
GO

/* ------------------------------ 3. PhienBan ----------------------------- */
CREATE TABLE PhienBan (
    MaPB        INT IDENTITY(1,1) NOT NULL,
    MaSP        INT               NOT NULL,
    DungLuong   NVARCHAR(20)      NOT NULL,
    MauSac      NVARCHAR(50)      NOT NULL,
    GiaBan      DECIMAL(18,0)     NOT NULL,
    SoLuongTon  INT               NOT NULL,
    CONSTRAINT PK_PhienBan PRIMARY KEY (MaPB),
    CONSTRAINT FK_PhienBan_SanPham_MaSP FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP) ON DELETE NO ACTION,
    CONSTRAINT CK_PhienBan_GiaBan CHECK (GiaBan > 0),
    CONSTRAINT CK_PhienBan_SoLuongTon CHECK (SoLuongTon >= 0)
);
-- Mỗi sản phẩm không có 2 phiên bản trùng dung lượng + màu
CREATE UNIQUE INDEX IX_PhienBan_MaSP_DungLuong_MauSac ON PhienBan(MaSP, DungLuong, MauSac);
GO

/* ------------------------------ 4. TaiKhoan ----------------------------- */
CREATE TABLE TaiKhoan (
    MaTK         INT IDENTITY(1,1) NOT NULL,
    TenDangNhap  NVARCHAR(50)      NOT NULL,
    MatKhauHash  NVARCHAR(500)     NOT NULL,   -- PBKDF2 (ASP.NET Core PasswordHasher), không lưu mật khẩu gốc
    HoTen        NVARCHAR(100)     NOT NULL,
    SoDienThoai  NVARCHAR(10)      NOT NULL,
    Email        NVARCHAR(150)     NULL,
    VaiTro       NVARCHAR(20)      NOT NULL,
    BiKhoa       BIT               NOT NULL,
    NgayTao      DATETIME2         NOT NULL CONSTRAINT DF_TaiKhoan_NgayTao DEFAULT (GETDATE()),
    CONSTRAINT PK_TaiKhoan PRIMARY KEY (MaTK),
    CONSTRAINT CK_TaiKhoan_VaiTro CHECK (VaiTro IN (N'Admin', N'NhanVien', N'KhachHang'))
);
CREATE UNIQUE INDEX IX_TaiKhoan_TenDangNhap ON TaiKhoan(TenDangNhap);
GO

/* ------------------------------ 5. DonHang ------------------------------ */
CREATE TABLE DonHang (
    MaDH             INT IDENTITY(1,1) NOT NULL,
    MaKH             INT               NOT NULL,
    NgayDat          DATETIME2         NOT NULL CONSTRAINT DF_DonHang_NgayDat DEFAULT (GETDATE()),
    HoTenNhan        NVARCHAR(100)     NOT NULL,
    SoDienThoaiNhan  NVARCHAR(10)      NOT NULL,
    DiaChiGiao       NVARCHAR(300)     NOT NULL,
    TongTien         DECIMAL(18,0)     NOT NULL,
    PhuongThucTT     NVARCHAR(30)      NOT NULL,   -- COD | VNPay | Trả góp
    TrangThai        NVARCHAR(30)      NOT NULL,   -- Chờ xác nhận → Đã xác nhận → Đang giao → Hoàn thành | Đã hủy
    DaThanhToan      BIT               NOT NULL,
    KyHanTraGop      INT               NULL,
    TraTruoc         DECIMAL(18,0)     NULL,
    GhiChu           NVARCHAR(500)     NULL,
    CONSTRAINT PK_DonHang PRIMARY KEY (MaDH),
    CONSTRAINT FK_DonHang_TaiKhoan_MaKH FOREIGN KEY (MaKH) REFERENCES TaiKhoan(MaTK) ON DELETE NO ACTION,
    CONSTRAINT CK_DonHang_TongTien CHECK (TongTien >= 0)
);
CREATE INDEX IX_DonHang_MaKH ON DonHang(MaKH);
GO

/* --------------------------- 6. ChiTietDonHang -------------------------- */
-- Mỗi dòng = 1 máy bán ra, để gán đúng 1 IMEI (khách mua 2 máy cùng phiên bản → 2 dòng)
CREATE TABLE ChiTietDonHang (
    MaCT     INT IDENTITY(1,1) NOT NULL,
    MaDH     INT               NOT NULL,
    MaPB     INT               NOT NULL,
    SoLuong  INT               NOT NULL,
    DonGia   DECIMAL(18,0)     NOT NULL,
    IMEI     NVARCHAR(15)      NULL,
    CONSTRAINT PK_ChiTietDonHang PRIMARY KEY (MaCT),
    CONSTRAINT FK_ChiTietDonHang_DonHang_MaDH FOREIGN KEY (MaDH) REFERENCES DonHang(MaDH) ON DELETE CASCADE,
    CONSTRAINT FK_ChiTietDonHang_PhienBan_MaPB FOREIGN KEY (MaPB) REFERENCES PhienBan(MaPB) ON DELETE NO ACTION,
    CONSTRAINT CK_CTDH_SoLuong CHECK (SoLuong > 0),
    -- IMEI: đúng 15 chữ số
    CONSTRAINT CK_CTDH_IMEI CHECK (IMEI IS NULL OR (LEN(IMEI) = 15 AND IMEI NOT LIKE '%[^0-9]%'))
);
CREATE INDEX IX_ChiTietDonHang_MaDH ON ChiTietDonHang(MaDH);
CREATE INDEX IX_ChiTietDonHang_MaPB ON ChiTietDonHang(MaPB);
-- IMEI không trùng (bỏ qua các dòng chưa gán)
CREATE UNIQUE INDEX IX_ChiTietDonHang_IMEI ON ChiTietDonHang(IMEI) WHERE IMEI IS NOT NULL;
GO

/* --------------------------- 7. DanhGia (bổ sung) ----------------------- */
CREATE TABLE DanhGia (
    MaDG         INT IDENTITY(1,1) NOT NULL,
    MaSP         INT               NOT NULL,
    MaTK         INT               NOT NULL,
    Diem         INT               NOT NULL,
    NoiDung      NVARCHAR(4000)    NOT NULL,
    NgayDanhGia  DATETIME2         NOT NULL,
    CONSTRAINT PK_DanhGia PRIMARY KEY (MaDG),
    CONSTRAINT FK_DanhGia_SanPham_MaSP FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP) ON DELETE CASCADE,
    CONSTRAINT FK_DanhGia_TaiKhoan_MaTK FOREIGN KEY (MaTK) REFERENCES TaiKhoan(MaTK) ON DELETE NO ACTION,
    CONSTRAINT CK_DanhGia_Diem CHECK (Diem BETWEEN 1 AND 5)
);
CREATE UNIQUE INDEX IX_DanhGia_MaSP_MaTK ON DanhGia(MaSP, MaTK);
CREATE INDEX IX_DanhGia_MaTK ON DanhGia(MaTK);
GO

/* -------------------------- 8. KhuyenMai (bổ sung) ---------------------- */
CREATE TABLE KhuyenMai (
    MaKM         INT IDENTITY(1,1) NOT NULL,
    TieuDe       NVARCHAR(200)     NOT NULL,
    NoiDung      NVARCHAR(MAX)     NULL,
    AnhBia       NVARCHAR(300)     NULL,
    NgayBatDau   DATETIME2         NOT NULL,
    NgayKetThuc  DATETIME2         NOT NULL,
    HienThi      BIT               NOT NULL,
    CONSTRAINT PK_KhuyenMai PRIMARY KEY (MaKM),
    CONSTRAINT CK_KhuyenMai_Ngay CHECK (NgayKetThuc >= NgayBatDau)
);
GO

/* ------------------------- 9. HinhAnhSanPham ------------------------- */
CREATE TABLE HinhAnhSanPham (
    MaHA      INT IDENTITY(1,1) NOT NULL,
    MaSP      INT               NOT NULL,
    DuongDan  NVARCHAR(300)     NOT NULL,
    ThuTu     INT               NOT NULL CONSTRAINT DF_HinhAnhSanPham_ThuTu DEFAULT (0),
    CONSTRAINT PK_HinhAnhSanPham PRIMARY KEY (MaHA),
    -- Xóa sản phẩm thì xóa luôn ảnh phụ
    CONSTRAINT FK_HinhAnhSanPham_SanPham_MaSP FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP) ON DELETE CASCADE
);
CREATE INDEX IX_HinhAnhSanPham_MaSP ON HinhAnhSanPham(MaSP);
GO

/* =========================== DỮ LIỆU MẪU =========================== */

SET IDENTITY_INSERT HangSanXuat ON;
INSERT INTO HangSanXuat (MaHang, TenHang, QuocGia) VALUES
 (1, N'Apple',   N'Mỹ'),
 (2, N'Samsung', N'Hàn Quốc'),
 (3, N'Xiaomi',  N'Trung Quốc'),
 (4, N'OPPO',    N'Trung Quốc'),
 (5, N'vivo',    N'Trung Quốc');
SET IDENTITY_INSERT HangSanXuat OFF;
GO

SET IDENTITY_INSERT SanPham ON;
INSERT INTO SanPham (MaSP, TenSP, MaHang, ManHinh, Chip, Camera, Pin, HeDieuHanh, MoTa, NgayTao) VALUES
 (1,  N'iPhone 16 Pro Max',          1, N'6.9" Super Retina XDR 120Hz',    N'Apple A18 Pro',                  N'48MP + 48MP + 12MP',          N'4685 mAh', N'iOS 18',   N'<p><strong>iPhone 16 Pro Max</strong> chính hãng, bảo hành 12 tháng.</p>', DATEADD(DAY,-60,GETDATE())),
 (2,  N'iPhone 16',                  1, N'6.1" Super Retina XDR',          N'Apple A18',                      N'48MP + 12MP',                 N'3561 mAh', N'iOS 18',   N'<p><strong>iPhone 16</strong> chính hãng.</p>', DATEADD(DAY,-55,GETDATE())),
 (3,  N'iPhone 15',                  1, N'6.1" Super Retina XDR',          N'Apple A16 Bionic',               N'48MP + 12MP',                 N'3349 mAh', N'iOS 17',   N'<p><strong>iPhone 15</strong> chính hãng.</p>', DATEADD(DAY,-300,GETDATE())),
 (4,  N'Samsung Galaxy S25 Ultra',   2, N'6.9" Dynamic AMOLED 2X 120Hz',   N'Snapdragon 8 Elite for Galaxy',  N'200MP + 50MP + 50MP + 10MP',  N'5000 mAh', N'Android 15', N'<p>Flagship có <em>bút S Pen</em>.</p>', DATEADD(DAY,-30,GETDATE())),
 (5,  N'Samsung Galaxy A56 5G',      2, N'6.7" Super AMOLED 120Hz',        N'Exynos 1580',                    N'50MP + 12MP + 5MP',           N'5000 mAh', N'Android 15', N'<p>Tầm trung bền bỉ.</p>', DATEADD(DAY,-20,GETDATE())),
 (6,  N'Samsung Galaxy Z Flip6',     2, N'6.7" Dynamic AMOLED 2X (gập)',   N'Snapdragon 8 Gen 3',             N'50MP + 12MP',                 N'4000 mAh', N'Android 14', N'<p>Điện thoại gập vỏ sò.</p>', DATEADD(DAY,-120,GETDATE())),
 (7,  N'Xiaomi 15',                  3, N'6.36" AMOLED 120Hz',             N'Snapdragon 8 Elite',             N'50MP x3 Leica',               N'5240 mAh', N'Android 15 (HyperOS 2)', N'<p>Camera Leica.</p>', DATEADD(DAY,-15,GETDATE())),
 (8,  N'Xiaomi Redmi Note 14 Pro',   3, N'6.67" AMOLED 120Hz',             N'Helio G100 Ultra',               N'200MP + 8MP + 2MP',           N'5500 mAh', N'Android 14', N'<p>Giá rẻ, camera 200MP.</p>', DATEADD(DAY,-90,GETDATE())),
 (9,  N'OPPO Reno13 F',              4, N'6.67" AMOLED 120Hz',             N'Snapdragon 6 Gen 1',             N'50MP + 8MP + 2MP',            N'5800 mAh', N'Android 15', N'<p>Thiết kế mỏng nhẹ.</p>', DATEADD(DAY,-40,GETDATE())),
 (10, N'OPPO Find X8',               4, N'6.59" AMOLED 120Hz',             N'Dimensity 9400',                 N'50MP x3 Hasselblad',          N'5630 mAh', N'Android 15', N'<p>Camera Hasselblad.</p>', DATEADD(DAY,-70,GETDATE())),
 (11, N'vivo V40',                   5, N'6.78" AMOLED 120Hz',             N'Snapdragon 7 Gen 3',             N'50MP + 50MP ZEISS',           N'5500 mAh', N'Android 14', N'<p>Camera ZEISS chân dung.</p>', DATEADD(DAY,-150,GETDATE())),
 (12, N'vivo Y29',                   5, N'6.68" LCD 120Hz',                N'Dimensity 6300',                 N'50MP + 0.08MP',               N'5500 mAh', N'Android 14', N'<p>Pin lớn, giá tốt.</p>', DATEADD(DAY,-10,GETDATE()));
SET IDENTITY_INSERT SanPham OFF;
GO

/* Ảnh đại diện + ảnh phụ (file có sẵn trong wwwroot/images/sanpham, logo trong wwwroot/images/hang) */
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/iphone-16-pro-max.png' WHERE MaSP = 1;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/iphone-16.png' WHERE MaSP = 2;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/iphone-15.png' WHERE MaSP = 3;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/samsung-galaxy-s25-ultra.png' WHERE MaSP = 4;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/samsung-galaxy-a56-5g.png' WHERE MaSP = 5;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/samsung-galaxy-z-flip6.png' WHERE MaSP = 6;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/xiaomi-15.png' WHERE MaSP = 7;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/xiaomi-redmi-note-14-pro.png' WHERE MaSP = 8;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/oppo-reno13-f.png' WHERE MaSP = 9;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/oppo-find-x8.png' WHERE MaSP = 10;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/vivo-v40.png' WHERE MaSP = 11;
UPDATE SanPham SET AnhDaiDien = N'/images/sanpham/vivo-y29.png' WHERE MaSP = 12;
INSERT INTO HinhAnhSanPham (MaSP, DuongDan, ThuTu) VALUES
 (1, N'/images/sanpham/iphone-16-pro-max-2.png', 1),
 (2, N'/images/sanpham/iphone-16-2.png', 1),
 (3, N'/images/sanpham/iphone-15-2.png', 1),
 (4, N'/images/sanpham/samsung-galaxy-s25-ultra-2.png', 1),
 (5, N'/images/sanpham/samsung-galaxy-a56-5g-2.png', 1),
 (6, N'/images/sanpham/samsung-galaxy-z-flip6-2.png', 1),
 (7, N'/images/sanpham/xiaomi-15-2.png', 1),
 (8, N'/images/sanpham/xiaomi-redmi-note-14-pro-2.png', 1),
 (9, N'/images/sanpham/oppo-reno13-f-2.png', 1),
 (10, N'/images/sanpham/oppo-find-x8-2.png', 1),
 (11, N'/images/sanpham/vivo-v40-2.png', 1),
 (12, N'/images/sanpham/vivo-y29-2.png', 1);
UPDATE HangSanXuat SET Logo = N'/images/hang/logo-apple.png' WHERE MaHang = 1;
UPDATE HangSanXuat SET Logo = N'/images/hang/logo-samsung.png' WHERE MaHang = 2;
UPDATE HangSanXuat SET Logo = N'/images/hang/logo-xiaomi.png' WHERE MaHang = 3;
UPDATE HangSanXuat SET Logo = N'/images/hang/logo-oppo.png' WHERE MaHang = 4;
UPDATE HangSanXuat SET Logo = N'/images/hang/logo-vivo.png' WHERE MaHang = 5;
GO

SET IDENTITY_INSERT PhienBan ON;
INSERT INTO PhienBan (MaPB, MaSP, DungLuong, MauSac, GiaBan, SoLuongTon) VALUES
 (1, 1, N'256GB', N'Titan Sa Mạc',   30990000, 15),
 (2, 1, N'256GB', N'Titan Đen',      30990000, 10),
 (3, 1, N'512GB', N'Titan Tự Nhiên', 37490000, 6),
 (4, 1, N'1TB',   N'Titan Trắng',    43990000, 3),
 (5, 2, N'128GB', N'Đen',            19990000, 20),
 (6, 2, N'128GB', N'Hồng',           19990000, 12),
 (7, 2, N'256GB', N'Xanh Lưu Ly',    22990000, 8),
 (8, 3, N'128GB', N'Đen',            16490000, 9),
 (9, 3, N'256GB', N'Xanh Lá',        19190000, 2),
 (10, 4, N'256GB', N'Xám Titan',     29990000, 10),
 (11, 4, N'512GB', N'Đen Titan',     33990000, 5),
 (12, 4, N'1TB',   N'Xanh Titan',    41990000, 2),
 (13, 5, N'128GB', N'Xám',            9490000, 25),
 (14, 5, N'256GB', N'Hồng',          10490000, 14),
 (15, 6, N'256GB', N'Xanh Mint',     25990000, 4),
 (16, 6, N'512GB', N'Vàng',          29990000, 0),
 (17, 7, N'256GB', N'Đen',           22990000, 8),
 (18, 7, N'512GB', N'Trắng',         24990000, 5),
 (19, 8, N'128GB', N'Tím',            7490000, 30),
 (20, 8, N'256GB', N'Đen',            8290000, 18),
 (21, 9, N'256GB', N'Tím',            9990000, 16),
 (22, 9, N'256GB', N'Xám',            9990000, 11),
 (23, 10, N'512GB', N'Đen',          22990000, 6),
 (24, 11, N'256GB', N'Tím Tinh Vân', 12990000, 9),
 (25, 11, N'512GB', N'Xám Thiên Hà', 14990000, 4),
 (26, 12, N'128GB', N'Trắng',         4990000, 40);
SET IDENTITY_INSERT PhienBan OFF;
GO

-- Mật khẩu đã băm bằng PBKDF2-HMAC-SHA512 (định dạng ASP.NET Core Identity V3)
SET IDENTITY_INSERT TaiKhoan ON;
INSERT INTO TaiKhoan (MaTK, TenDangNhap, MatKhauHash, HoTen, SoDienThoai, Email, VaiTro, BiKhoa) VALUES
 (1, N'admin',     N'AQAAAAIAAYagAAAAECYzhAInfc2H8HT3ofBOlmqurDfgMeAstYbb7RTQGz2BKJDhZT9vxOLFQ1BCD+Q02A==', N'Quản Trị Viên',   N'0901000001', N'admin@qldienthoai.vn',  N'Admin',     0),
 (2, N'nhanvien',  N'AQAAAAIAAYagAAAAEEo60ShWhQcmp0/u3FTJZZrpNNZcZWZK5mBxNXpFJPfTgf/nWb+7q93mBdMp3oARLQ==', N'Nguyễn Văn Bán',  N'0901000002', NULL,                     N'NhanVien',  0),
 (3, N'khachhang', N'AQAAAAIAAYagAAAAEJaDuMVq+xH8BhWH4biZtKwD7/94JG4SzRPEUcYgjtnPrgA9SS37tWrOYgbDMhb2UA==', N'Trần Thị Mua',    N'0912345678', N'khachhang@example.com', N'KhachHang', 0),
 (4, N'lehoang',   N'AQAAAAIAAYagAAAAEJaDuMVq+xH8BhWH4biZtKwD7/94JG4SzRPEUcYgjtnPrgA9SS37tWrOYgbDMhb2UA==', N'Lê Văn Hoàng',    N'0987654321', NULL,                     N'KhachHang', 0);
SET IDENTITY_INSERT TaiKhoan OFF;
GO

SET IDENTITY_INSERT DonHang ON;
INSERT INTO DonHang (MaDH, MaKH, NgayDat, HoTenNhan, SoDienThoaiNhan, DiaChiGiao, TongTien, PhuongThucTT, TrangThai, DaThanhToan) VALUES
 (1, 3, DATEADD(DAY,-200,GETDATE()), N'Trần Thị Mua', N'0912345678', N'12 Nguyễn Trãi, Thanh Xuân, Hà Nội',  30990000, N'VNPay', N'Hoàn thành', 1),
 (2, 4, DATEADD(DAY,-150,GETDATE()), N'Lê Văn Hoàng', N'0987654321', N'45 Lê Lợi, Quận 1, TP. Hồ Chí Minh', 39480000, N'COD',   N'Hoàn thành', 1),
 (3, 3, DATEADD(DAY,-90,GETDATE()),  N'Trần Thị Mua', N'0912345678', N'12 Nguyễn Trãi, Thanh Xuân, Hà Nội',  29990000, N'COD',   N'Hoàn thành', 1),
 (4, 4, DATEADD(DAY,-60,GETDATE()),  N'Lê Văn Hoàng', N'0987654321', N'45 Lê Lợi, Quận 1, TP. Hồ Chí Minh',  7490000, N'COD',   N'Hoàn thành', 1),
 (5, 3, DATEADD(DAY,-20,GETDATE()),  N'Trần Thị Mua', N'0912345678', N'12 Nguyễn Trãi, Thanh Xuân, Hà Nội',  22990000, N'VNPay', N'Hoàn thành', 1),
 (6, 3, DATEADD(DAY,-1,GETDATE()),   N'Trần Thị Mua', N'0912345678', N'12 Nguyễn Trãi, Thanh Xuân, Hà Nội',  19990000, N'COD',   N'Chờ xác nhận', 0);
SET IDENTITY_INSERT DonHang OFF;
GO

INSERT INTO ChiTietDonHang (MaDH, MaPB, SoLuong, DonGia, IMEI) VALUES
 (1, 1,  1, 30990000, N'351234560000001'),
 (2, 13, 1,  9490000, N'351234560000002'),
 (2, 10, 1, 29990000, N'351234560000003'),
 (3, 10, 1, 29990000, N'351234560000004'),
 (4, 19, 1,  7490000, N'351234560000005'),
 (5, 17, 1, 22990000, N'351234560000006'),
 (6, 5,  1, 19990000, NULL);
GO

INSERT INTO DanhGia (MaSP, MaTK, Diem, NoiDung, NgayDanhGia) VALUES
 (1, 3, 5, N'<p>Máy rất mượt, <strong>camera chụp đêm</strong> xuất sắc.</p>', DATEADD(DAY,-20,GETDATE())),
 (4, 4, 4, N'<p>Bút S Pen tiện, pin ổn.</p>',                                  DATEADD(DAY,-8,GETDATE())),
 (8, 4, 4, N'<p>Giá rẻ cấu hình ổn, pin trâu.</p>',                              DATEADD(DAY,-30,GETDATE()));
GO

INSERT INTO KhuyenMai (TieuDe, NoiDung, NgayBatDau, NgayKetThuc, HienThi) VALUES
 (N'Trả góp 0% lãi suất cho iPhone 16 Series', N'<p>Áp dụng trả góp <strong>0%</strong> kỳ hạn 6–12 tháng, trả trước từ 10%.</p>', CAST(DATEADD(DAY,-5,GETDATE()) AS DATE), CAST(DATEADD(DAY,25,GETDATE()) AS DATE), 1),
 (N'Thu cũ đổi mới – trợ giá đến 2 triệu',       N'<p>Mang máy cũ đến cửa hàng để được <em>trợ giá lên đời</em>.</p>',         CAST(DATEADD(DAY,-2,GETDATE()) AS DATE), CAST(DATEADD(DAY,30,GETDATE()) AS DATE), 1);
GO

/* ====================== MỘT SỐ TRUY VẤN KIỂM TRA ====================== */
-- Doanh thu theo tháng (đơn hoàn thành)
SELECT YEAR(NgayDat) AS Nam, MONTH(NgayDat) AS Thang, SUM(TongTien) AS DoanhThu
FROM DonHang WHERE TrangThai = N'Hoàn thành'
GROUP BY YEAR(NgayDat), MONTH(NgayDat) ORDER BY Nam, Thang;

-- Tra cứu bảo hành theo IMEI (12 tháng từ ngày đặt)
SELECT ct.IMEI, sp.TenSP, pb.DungLuong, pb.MauSac, dh.NgayDat, DATEADD(MONTH, 12, dh.NgayDat) AS HanBaoHanh
FROM ChiTietDonHang ct
JOIN DonHang dh  ON dh.MaDH = ct.MaDH
JOIN PhienBan pb ON pb.MaPB = ct.MaPB
JOIN SanPham sp  ON sp.MaSP = pb.MaSP
WHERE ct.IMEI = N'351234560000001';
GO
