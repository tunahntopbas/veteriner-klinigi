using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Klinikte çalışan veteriner hekim.
///
/// ÖDEV 2.1 — Hekimler sisteme tanımlanabilecek.
/// ÖDEV 2.2 — Uzmanlıklar ve verebildiği hizmetler (iki ayrı çok ↔ çok ilişki).
/// ÖDEV 2.3 — Müsaitlik saatleri (VeterinerMusaitlik tablosu, 1 → çok).
///
/// NEDEN Kullanici'dan AYRI bir tablo?
///   Giriş bilgileri (e-posta, parola hash'i, rol) Identity'nin AspNetUsers
///   tablosunda durur — Admin, Veteriner ve Üye için TEK kullanıcı tablosu.
///   Hekime özgü bilgiler (unvan, uzmanlık, müsaitlik, verdiği hizmetler) ise
///   sadece hekimleri ilgilendirdiği için bu tabloda. İkisi KullaniciId ile bağlı (1 ↔ 1).
/// </summary>
public class Veteriner
{
    public int Id { get; set; }

    public int KlinikId { get; set; }
    public Klinik? Klinik { get; set; }

    // Hekimin giriş yaptığı kullanıcı hesabı (AspNetUsers.Id → string).
    [Required]
    public string KullaniciId { get; set; } = "";
    public Kullanici? Kullanici { get; set; }

    [Required, StringLength(100)]
    public string AdSoyad { get; set; } = "";

    // "Uzm. Vet. Hek." gibi
    [StringLength(50)]
    public string? Unvan { get; set; }

    [StringLength(30)]
    public string? Telefon { get; set; }

    // Pasif hekim yeni randevu alamaz, eski kayıtları korunur.
    public bool Aktif { get; set; } = true;

    // ---- İlişkiler ----
    public List<Uzmanlik> Uzmanliklar { get; set; } = [];          // çok ↔ çok
    public List<Hizmet> Hizmetler { get; set; } = [];              // çok ↔ çok
    public List<VeterinerMusaitlik> Musaitlikler { get; set; } = []; // 1 → çok
    public List<Randevu> Randevular { get; set; } = [];            // 1 → çok
}
