using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace QLDienThoai.Services;

/// <summary>
/// Tích hợp cổng VNPay môi trường sandbox (phiên bản 2.1.0).
/// Đăng ký tài khoản test tại https://sandbox.vnpayment.vn để lấy TmnCode và HashSecret.
/// </summary>
public class VnPayService
{
    private readonly IConfiguration _config;

    public VnPayService(IConfiguration config) => _config = config;

    public bool DaCauHinh
    {
        get
        {
            var tmn = _config["VnPay:TmnCode"];
            return !string.IsNullOrWhiteSpace(tmn) && tmn != "YOUR_TMN_CODE";
        }
    }

    public string TaoUrlThanhToan(int maDH, decimal soTien, string ipAddress, string returnUrl)
    {
        var now = DateTime.Now;
        var p = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _config["VnPay:Version"] ?? "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _config["VnPay:TmnCode"]!,
            ["vnp_Amount"] = ((long)(soTien * 100)).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress,
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = $"Thanh toan don hang {maDH}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = $"{maDH}_{now:HHmmss}"
        };

        var query = TaoChuoi(p);
        var hash = HmacSha512(_config["VnPay:HashSecret"]!, query);
        return $"{_config["VnPay:BaseUrl"]}?{query}&vnp_SecureHash={hash}";
    }

    /// <summary>Kiểm tra chữ ký dữ liệu VNPay trả về; nếu hợp lệ trả về mã đơn.</summary>
    public (bool hopLe, bool thanhCong, int maDH) XuLyKetQua(IQueryCollection query)
    {
        var p = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in query)
        {
            if (key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
                p[key] = value.ToString();
        }

        var hashNhan = query["vnp_SecureHash"].ToString();
        var hashTinh = HmacSha512(_config["VnPay:HashSecret"]!, TaoChuoi(p));
        var hopLe = string.Equals(hashNhan, hashTinh, StringComparison.OrdinalIgnoreCase);

        var txnRef = p.GetValueOrDefault("vnp_TxnRef") ?? "";
        int.TryParse(txnRef.Split('_')[0], out var maDH);
        var thanhCong = p.GetValueOrDefault("vnp_ResponseCode") == "00"
                        && p.GetValueOrDefault("vnp_TransactionStatus") == "00";
        return (hopLe, thanhCong, maDH);
    }

    private static string TaoChuoi(SortedDictionary<string, string> p) =>
        string.Join("&", p.Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

    private static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
