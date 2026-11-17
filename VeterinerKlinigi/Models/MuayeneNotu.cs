using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Hekimin tamamlanan randevu için yazdığı muayene notu.
///
/// ÖDEV 3.4 — Tamamlanan her randevu için hekim muayene notu girebilecek.
/// Kural (Services/RandevuServisi.cs → MuayeneNotuKaydet):
///   - Randevu "Tamamlandi" durumunda olmalı
///   - Notu yalnızca o randevunun KENDİ hekimi yazabilir
/// Her randevunun en fazla bir notu olur (RandevuId benzersiz → 1 ↔ 0..1).
/// </summary>
public class MuayeneNotu
{
    public int Id { get; set; }

    public int RandevuId { get; set; }
    public Randevu? Randevu { get; set; }

    [StringLength(300)]
    public string? Teshis { get; set; }

    [StringLength(500)]
    public string? Tedavi { get; set; }

    [Required, StringLength(2000)]
    public string Notlar { get; set; } = "";

    public DateTime YazilmaZamani { get; set; }
}
