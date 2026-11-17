using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// MCP erişim anahtarlarını üretir, listeler, iptal eder ve doğrular.
///
/// ÖDEV 6.4 — MCP ucu yetkilendirme ile korunur.
///
/// AKIŞ:
///   1) Kullanıcı Profil sayfasında "Yeni anahtar üret" der.
///   2) Rastgele bir anahtar üretilir ("vk_" + 43 karakter). Kullanıcıya BİR KEZ gösterilir.
///   3) Veritabanına anahtarın kendisi değil SHA-256 özeti yazılır.
///   4) AI istemcisi her istekte "Authorization: Bearer vk_..." başlığını gönderir.
///   5) Mcp/McpAnahtarKimlikDogrulama.cs başlıktaki anahtarın özetini alıp veritabanında arar;
///      bulursa o kullanıcı adına giriş yapılmış sayılır (HttpContext.User dolar).
/// </summary>
public interface IMcpAnahtarServisi
{
    Task<(McpErisimAnahtari Kayit, string AcikAnahtar)> UretAsync(string kullaniciId, string ad);
    Task<List<McpErisimAnahtari>> ListeleAsync(string kullaniciId);
    Task<IslemSonucu> IptalEtAsync(int id, string kullaniciId);
    Task<string?> DogrulaAsync(string acikAnahtar);
}

public class McpAnahtarServisi : IMcpAnahtarServisi
{
    private const string OnEkMetni = "vk_";
    private readonly UygulamaDbContext _db;

    public McpAnahtarServisi(UygulamaDbContext db)
    {
        _db = db;
    }

    public async Task<(McpErisimAnahtari Kayit, string AcikAnahtar)> UretAsync(string kullaniciId, string ad)
    {
        // 32 bayt kriptografik rastgele sayı → URL'de sorun çıkarmayan Base64 metni.
        // RandomNumberGenerator: tahmin edilemez (Random sınıfı güvenlik için KULLANILMAZ).
        var rastgele = RandomNumberGenerator.GetBytes(32);
        var acikAnahtar = OnEkMetni + Convert.ToBase64String(rastgele)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var kayit = new McpErisimAnahtari
        {
            KullaniciId = kullaniciId,
            Ad = ad.Trim(),
            AnahtarHash = HashHesapla(acikAnahtar),
            OnEk = acikAnahtar[..8],          // listede tanımak için "vk_AbCdE"
            OlusturmaZamani = DateTime.Now
        };
        _db.McpErisimAnahtarlari.Add(kayit);
        await _db.SaveChangesAsync();

        return (kayit, acikAnahtar);          // açık anahtar SADECE burada, bir kez döner
    }

    public async Task<List<McpErisimAnahtari>> ListeleAsync(string kullaniciId) =>
        await _db.McpErisimAnahtarlari
            .Where(a => a.KullaniciId == kullaniciId)
            .OrderByDescending(a => a.OlusturmaZamani)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IslemSonucu> IptalEtAsync(int id, string kullaniciId)
    {
        // Sadece kendi anahtarını iptal edebilir.
        var anahtar = await _db.McpErisimAnahtarlari.FirstOrDefaultAsync(a => a.Id == id && a.KullaniciId == kullaniciId);
        if (anahtar is null)
            return IslemSonucu.KayitYok();
        anahtar.IptalEdildi = true;
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    /// <summary>Anahtar geçerliyse sahibinin kullanıcı Id'sini döner, değilse null.</summary>
    public async Task<string?> DogrulaAsync(string acikAnahtar)
    {
        if (string.IsNullOrWhiteSpace(acikAnahtar) || !acikAnahtar.StartsWith(OnEkMetni))
            return null;

        var hash = HashHesapla(acikAnahtar);
        var anahtar = await _db.McpErisimAnahtarlari
            .FirstOrDefaultAsync(a => a.AnahtarHash == hash && !a.IptalEdildi);
        if (anahtar is null)
            return null;

        anahtar.SonKullanimZamani = DateTime.Now;
        await _db.SaveChangesAsync();
        return anahtar.KullaniciId;
    }

    // SHA-256 → 64 karakterlik hex. Aynı girdi her zaman aynı özeti verir, özetten girdi bulunamaz.
    private static string HashHesapla(string metin) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(metin))).ToLowerInvariant();
}
