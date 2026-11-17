using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Bir hayvan için, bir hekimden, bir hizmet için alınan randevu.
///
/// ÖDEV 4.1 — Sahip hayvanını, hizmeti ve uygun hekimi seçerek randevu alır.
/// ÖDEV 4.3 — Randevu detayları (hizmet, süre, ücret, hekim, hayvan, tarih/saat) saklanır.
///
/// DİKKAT — SureDakika ve Ucret NEDEN hizmetten KOPYALANIYOR?
///   Admin yarın "Muayene" ücretini 500'den 600'e çıkarırsa, geçen ayki randevunun
///   ücreti 600 olarak görünmemeli; ciro raporu da bozulmamalı. Randevu alındığı
///   andaki süre ve ücret randevunun KENDİ satırına yazılır.
///
/// TARİHLER: Klinik yerel saati, PostgreSQL'de "timestamp without time zone".
/// BitisZamani = BaslangicZamani + SureDakika (çakışma sorgusu kolay olsun diye saklanıyor).
/// </summary>
public class Randevu
{
    public int Id { get; set; }

    public int EvcilHayvanId { get; set; }
    public EvcilHayvan? EvcilHayvan { get; set; }

    public int HizmetId { get; set; }
    public Hizmet? Hizmet { get; set; }

    public int VeterinerId { get; set; }
    public Veteriner? Veteriner { get; set; }

    public DateTime BaslangicZamani { get; set; }
    public DateTime BitisZamani { get; set; }

    // ÖDEV 4.3 — Randevu anındaki süre ve ücret (hizmetten kopya).
    public int SureDakika { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal Ucret { get; set; }

    // ÖDEV 4.4 — Yeni randevu her zaman Bekliyor ile başlar.
    public RandevuDurumu Durum { get; set; } = RandevuDurumu.Bekliyor;

    // Sahibin randevu alırken yazdığı not ("iştahsız, 2 gündür halsiz" gibi).
    [StringLength(500)]
    public string? SahipNotu { get; set; }

    // Reddedilirse nedeni (sahip görebilsin diye).
    [StringLength(300)]
    public string? RedNedeni { get; set; }

    public DateTime OlusturmaZamani { get; set; }

    // ÖDEV 3.4 — Tamamlanan randevunun muayene notu (1 ↔ 0..1).
    public MuayeneNotu? MuayeneNotu { get; set; }
}
