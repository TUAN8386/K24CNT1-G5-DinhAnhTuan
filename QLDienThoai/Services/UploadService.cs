namespace QLDienThoai.Services;

/// <summary>Upload ảnh an toàn: kiểm tra đuôi file, kích thước, chữ ký (magic bytes), đổi tên ngẫu nhiên.</summary>
public class UploadService
{
    public const long KichThuocToiDa = 5 * 1024 * 1024; // 5 MB
    private static readonly Dictionary<string, byte[][]> ChuKy = new()
    {
        [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jfif"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
        [".webp"] = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } },
        [".gif"] = new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } }
    };

    private readonly IWebHostEnvironment _env;

    public UploadService(IWebHostEnvironment env) => _env = env;

    /// <summary>Thư mục wwwroot thực tế (phòng khi WebRootPath = null).</summary>
    private string GocWeb => string.IsNullOrEmpty(_env.WebRootPath) ? Path.Combine(_env.ContentRootPath, "wwwroot") : _env.WebRootPath;

    /// <summary>Kiểm tra file, trả về thông báo lỗi hoặc null nếu hợp lệ.</summary>
    public string? KiemTra(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "Chưa chọn file ảnh";
        if (file.Length > KichThuocToiDa) return "Ảnh tối đa 5 MB";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ChuKy.ContainsKey(ext)) return "Chỉ chấp nhận ảnh .jpg, .jpeg, .jfif, .png, .webp, .gif";

        using var s = file.OpenReadStream();
        var header = new byte[8];
        var n = s.Read(header, 0, header.Length);
        var hopLe = ChuKy[ext].Any(sig => n >= sig.Length && header.Take(sig.Length).SequenceEqual(sig));
        return hopLe ? null : "Nội dung file không phải ảnh hợp lệ";
    }

    /// <summary>Lưu ảnh vào wwwroot/uploads/{thuMuc}, trả về đường dẫn web.</summary>
    public async Task<string> LuuAsync(IFormFile file, string thuMuc)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var dir = Path.Combine(GocWeb, "uploads", thuMuc);
        Directory.CreateDirectory(dir);
        var ten = $"{Guid.NewGuid():N}{ext}";
        await using (var fs = new FileStream(Path.Combine(dir, ten), FileMode.Create))
            await file.CopyToAsync(fs);
        return $"/uploads/{thuMuc}/{ten}";
    }

    public void Xoa(string? duongDan)
    {
        if (string.IsNullOrEmpty(duongDan) || !duongDan.StartsWith("/uploads/")) return;
        var path = Path.GetFullPath(Path.Combine(GocWeb, duongDan.TrimStart('/')));
        var root = Path.GetFullPath(Path.Combine(GocWeb, "uploads"));
        if (path.StartsWith(root) && File.Exists(path)) File.Delete(path);
    }
}
