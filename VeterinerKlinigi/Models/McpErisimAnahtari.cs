using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Kullanıcının bir yapay zekâ asistanını (Claude Desktop, VS Code, MCP Inspector)
/// kendi hesabına bağlamak için ürettiği erişim anahtarı.
///
/// ÖDEV 6.4 — MCP ucu yetkilendirme ile korunur; araçlar yalnızca anahtarın
/// sahibi olan kullanıcının verisini döndürür.
///
/// GÜVENLİK: Anahtarın kendisi veritabanına YAZILMAZ, sadece SHA-256 özeti (hash).
/// Anahtar üretildiği anda kullanıcıya BİR KEZ gösterilir. Veritabanı sızsa bile
/// hash'ten anahtar geri elde edilemez (parolalardaki mantığın aynısı).
/// </summary>
public class McpErisimAnahtari
{
    public int Id { get; set; }

    [Required]
    public string KullaniciId { get; set; } = "";
    public Kullanici? Kullanici { get; set; }

    // Kullanıcının verdiği isim: "Claude Desktop", "VS Code" gibi.
    [Required, StringLength(60)]
    public string Ad { get; set; } = "";

    // Anahtarın SHA-256 özeti (64 karakter hex).
    [Required, StringLength(64)]
    public string AnahtarHash { get; set; } = "";

    // Listede tanımak için anahtarın ilk birkaç karakteri ("vk_3fA9..." gibi).
    [Required, StringLength(16)]
    public string OnEk { get; set; } = "";

    public DateTime OlusturmaZamani { get; set; }
    public DateTime? SonKullanimZamani { get; set; }

    // Kullanıcı anahtarı iptal ederse true → artık giriş yapamaz.
    public bool IptalEdildi { get; set; }
}
