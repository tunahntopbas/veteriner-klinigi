using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Mcp;

/// <summary>
/// MCP ucu (/mcp) için kimlik doğrulama: "Authorization: Bearer vk_..." başlığını okur.
///
/// ÖDEV 6.4 — MCP ucu yetkilendirme ile korunur.
///
/// NEDEN çerez değil? AI istemcisi (Claude Desktop, VS Code, MCP Inspector) tarayıcı
/// değildir; giriş sayfası açıp çerez alamaz. Kullanıcı Profil sayfasından bir anahtar
/// üretir ve istemcinin ayar dosyasına yazar. İstemci her istekte bu anahtarı gönderir.
///
/// Bu sınıf ASP.NET Core'un kimlik doğrulama hattına yeni bir "şema" (scheme) ekler.
/// Program.cs: AddAuthentication().AddScheme&lt;..., McpAnahtarKimlikDogrulama&gt;("McpAnahtari", ...)
/// </summary>
public class McpAnahtarKimlikDogrulama : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SemaAdi = "McpAnahtari";

    private readonly IMcpAnahtarServisi _anahtarlar;
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;
    private readonly SignInManager<Kullanici> _girisYoneticisi;

    public McpAnahtarKimlikDogrulama(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        IMcpAnahtarServisi anahtarlar, UserManager<Kullanici> kullaniciYoneticisi, SignInManager<Kullanici> girisYoneticisi)
        : base(options, logger, encoder)
    {
        _anahtarlar = anahtarlar;
        _kullaniciYoneticisi = kullaniciYoneticisi;
        _girisYoneticisi = girisYoneticisi;
    }

    // Her /mcp isteğinde çalışır: "Bu istek KİMİN adına?"
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? baslik = Request.Headers.Authorization;
        if (string.IsNullOrEmpty(baslik) || !baslik.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();            // başlık yok → anonim (sonra 401 olur)

        var anahtar = baslik["Bearer ".Length..].Trim();
        var kullaniciId = await _anahtarlar.DogrulaAsync(anahtar);
        if (kullaniciId is null)
            return AuthenticateResult.Fail("Geçersiz veya iptal edilmiş MCP anahtarı.");

        var kullanici = await _kullaniciYoneticisi.FindByIdAsync(kullaniciId);
        if (kullanici is null)
            return AuthenticateResult.Fail("Anahtarın kullanıcısı bulunamadı.");

        // Identity'nin giriş çereziyle AYNI kimliği üretir: Id, e-posta ve ROLLER (Admin/Veteriner/Uye).
        // Böylece MCP araçlarında User.Bilgi() web sitesindekiyle aynı sonucu verir.
        var kimlik = await _girisYoneticisi.CreateUserPrincipalAsync(kullanici);
        var bilet = new AuthenticationTicket(kimlik, SemaAdi);
        return AuthenticateResult.Success(bilet);
    }

    // Kimlik doğrulanamazsa giriş sayfasına yönlendirme DEĞİL, 401 + açıklayıcı başlık.
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer realm=\"mcp\", error=\"invalid_token\", " +
            "error_description=\"Profil sayfasindan MCP erisim anahtari uretip Authorization: Bearer ile gonderin\"";
        return Task.CompletedTask;
    }
}
