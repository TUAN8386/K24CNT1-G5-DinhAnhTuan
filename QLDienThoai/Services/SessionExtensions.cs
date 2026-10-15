using System.Text.Json;

namespace QLDienThoai.Services;

public static class SessionExtensions
{
    public static void SetJson<T>(this ISession session, string key, T value)
        => session.SetString(key, JsonSerializer.Serialize(value));

    public static T? GetJson<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        if (string.IsNullOrEmpty(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json); }
        catch (JsonException) { return default; }
    }
}
