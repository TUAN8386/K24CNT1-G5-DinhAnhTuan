namespace QLDienThoai.Services;

/// <summary>Danh sách sản phẩm đang so sánh, lưu trong Session, tối đa 3 máy.</summary>
public class SoSanhService
{
    public const string SessionKey = "SoSanh";
    public const int ToiDa = 3;
    private readonly IHttpContextAccessor _http;

    public SoSanhService(IHttpContextAccessor http) => _http = http;

    private ISession Session => _http.HttpContext!.Session;

    public List<int> LayDanhSach() => Session.GetJson<List<int>>(SessionKey) ?? new();

    public string? Them(int maSP)
    {
        var list = LayDanhSach();
        if (list.Contains(maSP)) return "Sản phẩm đã có trong danh sách so sánh";
        if (list.Count >= ToiDa) return $"Chỉ so sánh tối đa {ToiDa} sản phẩm. Hãy bỏ bớt một máy.";
        list.Add(maSP);
        Session.SetJson(SessionKey, list);
        return null;
    }

    public void Xoa(int maSP)
    {
        var list = LayDanhSach();
        list.Remove(maSP);
        Session.SetJson(SessionKey, list);
    }

    public void XoaHet() => Session.Remove(SessionKey);
}
