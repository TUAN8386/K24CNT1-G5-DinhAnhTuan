# QLDienThoai – Website bán điện thoại di động (Đề 17)

ASP.NET Core MVC (.NET 10) + Entity Framework Core 10 + SQL Server.

## Cấu trúc thư mục nộp

```
K24CNT1-G5-DinhAnhTuan/
├── QLDienThoai/          ← mã nguồn (mở QLDienThoai.csproj bằng Visual Studio 2022/2026 hoặc VS Code)
├── Database/
│   ├── QLDienThoai.sql   ← script tạo CSDL + dữ liệu mẫu
│   ├── ERD.png / ERD.dot ← sơ đồ quan hệ
├── TestCases_QLDienThoai.xlsx
└── README.md
```

## Chạy dự án

1. Cài .NET 10 SDK và SQL Server (bản Express/Developer hoặc LocalDB).
2. Mở `QLDienThoai/appsettings.json`, sửa `ConnectionStrings:DefaultConnection` cho đúng máy:
   - SQL Server mặc định: `Server=.;Database=QLDienThoaiDb;Trusted_Connection=True;TrustServerCertificate=True`
   - SQL Express: `Server=.\\SQLEXPRESS;...`
   - LocalDB: `Server=(localdb)\\MSSQLLocalDB;...`
3. Tạo CSDL – chọn **một** trong hai cách:
   - **Tự động**: chỉ cần chạy ứng dụng, `DbSeeder` sẽ gọi `EnsureCreated()` và nạp dữ liệu mẫu.
   - **Bằng script**: chạy `Database/QLDienThoai.sql` trong SSMS rồi chạy ứng dụng.
4. Chạy: `cd QLDienThoai` → `dotnet run` (hoặc F5 trong Visual Studio), mở https://localhost:7207

> Nếu muốn dùng Migrations thay cho EnsureCreated: xóa dòng `EnsureCreatedAsync` trong `Data/DbSeeder.cs`,
> chạy `dotnet ef migrations add Init` rồi `dotnet ef database update`.

## Tài khoản mẫu

| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Quản trị viên | admin | Admin@123 |
| Nhân viên bán hàng | nhanvien | Nhanvien@123 |
| Khách hàng | khachhang | Khachhang@123 |
| Khách hàng | lehoang | Khachhang@123 |

IMEI mẫu để tra cứu bảo hành: `351234560000001`.

## Chức năng theo yêu cầu đề

| Yêu cầu | Vị trí trong mã nguồn |
|---|---|
| CRUD Hãng, Sản phẩm, Phiên bản, Đơn hàng (kèm chi tiết) | `Areas/Admin/Controllers/*` + Views tương ứng (Index/Details/Create/Edit/Delete) |
| Xác nhận trước khi xóa, chặn xóa khi có liên kết | Trang `Delete` + kiểm tra trong `DeleteConfirmed`; FK `ON DELETE NO ACTION` |
| Phân trang, tìm kiếm tên máy, lọc hãng/khoảng giá/dung lượng, sắp xếp giá/bán chạy, lọc đơn theo trạng thái & ngày | `Controllers/SanPhamController.Index`, `Models/PagedResult.cs`, `Views/Shared/_Pager.cshtml`, `Areas/Admin/Controllers/DonHangController.Index` |
| Session: giỏ hàng, danh sách so sánh (tối đa 3) | `Services/GioHangService.cs`, `Services/SoSanhService.cs` |
| Cookie: sản phẩm đã xem | `Services/DaXemService.cs` |
| Validation: IMEI 15 số không trùng, giá > 0, số lượng ≤ tồn, SĐT, địa chỉ bắt buộc, điểm 1–5 | Data Annotations trong `Models/*`, `IValidatableObject`, kiểm tra server trong controller, CHECK/UNIQUE trong CSDL |
| Upload ảnh + editor | `Services/UploadService.cs` (kiểm tra đuôi, ≤ 5 MB, magic bytes), xem trước ảnh trước khi tải (`wwwroot/js/site.js`), CKEditor 5 (`Views/Shared/_EditorScripts.cshtml`) |
| Ảnh sản phẩm | Ảnh đại diện (thay / xóa) + **thư viện ảnh phụ** tối đa 10 ảnh (bảng `HinhAnhSanPham`): thêm nhiều ảnh, xóa ảnh, đặt làm ảnh chính trong *Admin → Sản phẩm → Sửa*; trang chi tiết có ảnh nhỏ để chuyển ảnh. Ảnh mẫu trong `wwwroot/images/sanpham`, logo hãng trong `wwwroot/images/hang` |
| Bảo mật | Mật khẩu băm PBKDF2 (`PasswordHasher`), Cookie Authentication + `[Authorize(Roles=...)]`, `AutoValidateAntiforgeryToken` (CSRF), Razor encode + `HtmlSanitizer` (XSS), EF Core tham số hóa (SQL Injection), khách chỉ xem đơn của mình |
| Giao diện responsive | Bootstrap 5.3 |
| Trả góp, VNPay sandbox, email xác nhận (điểm cộng) | `TraGopVM`, `site.js`; `Services/VnPayService.cs` + `ThanhToanController`; `Services/EmailService.cs` |

### Cấu hình điểm cộng
- **VNPay sandbox**: đăng ký tại https://sandbox.vnpayment.vn, điền `VnPay:TmnCode` và `VnPay:HashSecret`. Chưa cấu hình thì đơn VNPay vẫn được ghi nhận và hiện thông báo.
- **Email**: điền mục `Smtp` (ví dụ Gmail: Host `smtp.gmail.com`, Port 587, User là địa chỉ Gmail, Password là *App Password*). Chưa cấu hình thì email chỉ ghi ra log.
- **Lãi suất trả góp**: `TraGop:LaiSuatThang` (% / tháng, mặc định 0).

## Ghi chú thiết kế
- Ngoài 6 bảng theo đề, có thêm **DanhGia** (điểm 1–5 + bài viết), **KhuyenMai** (tin khuyến mãi) và **HinhAnhSanPham** (thư viện ảnh). CSDL đã tạo từ bản trước sẽ được tự thêm bảng `HinhAnhSanPham` và ảnh mẫu khi chạy ứng dụng (`DbSeeder`).
- `ChiTietDonHang` dùng khóa chính `MaCT`; **mỗi dòng là một máy** để gán đúng một IMEI (unique index lọc `IMEI IS NOT NULL`).
- Luồng đơn hàng: Chờ xác nhận → Đã xác nhận → Đang giao → Hoàn thành (hủy được khi chưa giao, tự hoàn tồn kho). Phải gán đủ IMEI trước khi chuyển sang "Đang giao".
- Tồn kho được trừ nguyên tử bằng `ExecuteUpdate ... WHERE SoLuongTon >= n` trong transaction để tránh bán vượt tồn.
- Bảo hành 12 tháng tính từ ngày đặt đơn.
