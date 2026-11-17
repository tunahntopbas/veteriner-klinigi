using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Sahibin kaydettiği evcil hayvan.
///
/// ÖDEV 3.1 — Sahip birden fazla hayvan kaydedebilir (SahipId ile Kullanici'ya bağlı).
/// ÖDEV 3.2 — Tür, ırk, doğum tarihi, kilo ve isteğe bağlı fotoğraf.
///
/// FOTOĞRAF NEREDE? Dosyanın kendisi diskte, wwwroot DIŞINDA
/// (Yuklemeler/hayvanlar/ klasörü). Veritabanında sadece dosya ADI tutulur.
/// Ayrıntı: Services/FotografServisi.cs
/// </summary>
public class EvcilHayvan
{
    public int Id { get; set; }

    // Hayvanın sahibi (AspNetUsers.Id). Yetki kontrolleri bu alana bakar:
    // bir üye yalnızca SahipId'si kendisi olan hayvanları görebilir.
    [Required]
    public string SahipId { get; set; } = "";
    public Kullanici? Sahip { get; set; }

    [Required, StringLength(50)]
    public string Ad { get; set; } = "";

    // Kedi, Köpek, Kuş, Tavşan...
    [Required, StringLength(40)]
    public string Tur { get; set; } = "";

    [StringLength(60)]
    public string? Irk { get; set; }

    // DateOnly: sadece tarih (saat yok). PostgreSQL'de "date" sütunu.
    public DateOnly DogumTarihi { get; set; }

    // kg cinsinden, isteğe bağlı. numeric(6,2) → 9999,99 kg'a kadar.
    [Column(TypeName = "numeric(6,2)")]
    public decimal? Kilo { get; set; }

    // Diskteki dosyanın GUID'li adı ("3f2a...b1.jpg"). Fotoğraf yoksa null.
    [StringLength(100)]
    public string? FotografDosyaAdi { get; set; }

    public DateTime KayitZamani { get; set; }

    // ---- İlişkiler ----
    public List<AsiKaydi> AsiKayitlari { get; set; } = [];
    public List<Randevu> Randevular { get; set; } = [];
}
