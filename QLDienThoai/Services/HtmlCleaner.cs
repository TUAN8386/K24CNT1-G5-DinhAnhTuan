using Ganss.Xss;

namespace QLDienThoai.Services;

/// <summary>Lọc HTML từ editor để chống XSS (bỏ script, on* event, javascript: URL...).</summary>
public class HtmlCleaner
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlCleaner()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedAttributes.Add("class");
    }

    public string Lam(string? html) => string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html);
}
