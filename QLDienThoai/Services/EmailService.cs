using System.Net;
using System.Net.Mail;
using System.Text;
using QLDienThoai.Models;

namespace QLDienThoai.Services;

/// <summary>
/// Gửi email xác nhận đơn hàng qua SMTP (cấu hình mục "Smtp" trong appsettings.json).
/// Nếu chưa cấu hình Host, email chỉ được ghi ra log để không làm hỏng luồng đặt hàng.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task GuiXacNhanDonAsync(DonHang dh, string? emailNhan)
    {
        if (string.IsNullOrWhiteSpace(emailNhan)) return;

        var sb = new StringBuilder();
        sb.Append($"<h2>Cảm ơn {WebUtility.HtmlEncode(dh.HoTenNhan)} đã đặt hàng!</h2>");
        sb.Append($"<p>Mã đơn: <b>#{dh.MaDH}</b> – Ngày đặt: {dh.NgayDat:dd/MM/yyyy HH:mm}</p>");
        sb.Append("<table border='1' cellpadding='6' style='border-collapse:collapse'><tr><th>Sản phẩm</th><th>Đơn giá</th></tr>");
        foreach (var ct in dh.ChiTietDonHangs)
        {
            var ten = ct.PhienBan == null ? $"Phiên bản #{ct.MaPB}" : $"{ct.PhienBan.SanPham?.TenSP} {ct.PhienBan.DungLuong} {ct.PhienBan.MauSac}";
            sb.Append($"<tr><td>{WebUtility.HtmlEncode(ten)}</td><td>{ct.DonGia:N0} đ</td></tr>");
        }
        sb.Append("</table>");
        sb.Append($"<p>Tổng tiền: <b>{dh.TongTien:N0} đ</b> – Thanh toán: {WebUtility.HtmlEncode(dh.PhuongThucTT)}</p>");
        sb.Append($"<p>Giao đến: {WebUtility.HtmlEncode(dh.DiaChiGiao)}</p>");

        await GuiAsync(emailNhan, $"Xác nhận đơn hàng #{dh.MaDH}", sb.ToString());
    }

    public async Task GuiAsync(string den, string tieuDe, string htmlBody)
    {
        var s = _config.GetSection("Smtp");
        var host = s["Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("[Email chưa cấu hình SMTP] Tới: {Den} | {TieuDe}", den, tieuDe);
            return;
        }

        try
        {
            using var client = new SmtpClient(host, s.GetValue("Port", 587))
            {
                EnableSsl = s.GetValue("EnableSsl", true),
                Credentials = new NetworkCredential(s["User"], s["Password"])
            };
            var from = string.IsNullOrWhiteSpace(s["From"]) ? s["User"]! : s["From"]!;
            using var msg = new MailMessage(new MailAddress(from, s["FromName"] ?? "QL Điện Thoại"), new MailAddress(den))
            {
                Subject = tieuDe,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            await client.SendMailAsync(msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email tới {Den} thất bại", den);
        }
    }
}
