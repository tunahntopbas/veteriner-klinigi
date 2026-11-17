using System.Security.Claims;
using VeterinerKlinigi.Data;

namespace VeterinerKlinigi.Services;

/// <summary>
/// "İşlemi KİM yapıyor?" sorusunun cevabı. Servislere bu nesne verilir;
/// servis, kullanıcının rolüne ve Id'sine bakarak yetki kontrolü yapar.
///
/// NEDEN ClaimsPrincipal yerine bu küçük record?
///   MVC controller'da User, REST API'de User, MCP aracında HttpContext.User var.
///   Üçü de aynı ClaimsPrincipal; biz onu servislere sade bir nesne olarak taşıyoruz.
///   Böylece servis HTTP'den habersiz kalır ve üç yerden de aynı şekilde çağrılır
///   (ÖDEV 6.3 — iş kuralları tek yerde).
/// </summary>
public record KullaniciBilgisi(string Id, bool AdminMi, bool VeterinerMi);

public static class KullaniciBilgisiUzantilari
{
    /// <summary>
    /// Giriş yapmış kullanıcının bilgisini çıkarır. Kullanım: User.Bilgi()
    /// ClaimTypes.NameIdentifier = AspNetUsers tablosundaki Id.
    /// </summary>
    public static KullaniciBilgisi Bilgi(this ClaimsPrincipal user) => new(
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
        user.IsInRole(Roller.Admin),
        user.IsInRole(Roller.Veteriner));
}
