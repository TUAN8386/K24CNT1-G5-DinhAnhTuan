namespace QLDienThoai.Services;

/// <summary>Lưu danh sách sản phẩm đã xem gần đây trong Cookie (tối đa 8, giữ 30 ngày).</summary>
public class DaXemService
{
    public const string CookieName = "SanPhamDaXem";
    private const int ToiDa = 8;
    private readonly IHttpContextAccessor _http;

    public DaXemService(IHttpContextAccessor http) => _http = http;

    public List<int> LayDanhSach()
    {
        var raw = _http.HttpContext?.Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(raw)) return new();
        return raw.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .Take(ToiDa)
            .ToList();
    }

    public void Them(int maSP)
    {
        var list = LayDanhSach();
        list.Remove(maSP);
        list.Insert(0, maSP);
        _http.HttpContext!.Response.Cookies.Append(CookieName, string.Join('-', list.Take(ToiDa)), new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddDays(30),
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = _http.HttpContext.Request.IsHttps
        });
    }
}
