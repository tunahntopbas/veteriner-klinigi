using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Kliniğin sunduğu bir hizmet: muayene, aşı, kısırlaştırma, diş temizliği, tıraş ve bakım...
///
/// ÖDEV 1.2 — Hizmetlerin süre ve ücretleri detaylı tanımlanabilecek.
/// SureDakika, randevu çakışma hesabında kullanılır (ÖDEV 4.2).
/// </summary>
public class Hizmet
{
    public int Id { get; set; }

    public int KlinikId { get; set; }
    public Klinik? Klinik { get; set; }

    [Required, StringLength(100)]
    public string Ad { get; set; } = "";

    [StringLength(500)]
    public string? Aciklama { get; set; }

    // Hizmetin kaç dakika sürdüğü. Randevunun bitiş saati = başlangıç + bu süre.
    public int SureDakika { get; set; }

    // Para için her zaman decimal (double yuvarlama hatası yapar).
    // numeric(10,2) → en fazla 99.999.999,99 TL, virgülden sonra 2 hane.
    [Column(TypeName = "numeric(10,2)")]
    public decimal Ucret { get; set; }

    // Pasif hizmet yeni randevuda seçilemez ama eski randevular bozulmaz.
    public bool Aktif { get; set; } = true;

    // Bu hizmeti verebilen hekimler (çok ↔ çok). Ara tablo EF Core tarafından otomatik oluşturulur.
    public List<Veteriner> Veterinerler { get; set; } = [];
}
