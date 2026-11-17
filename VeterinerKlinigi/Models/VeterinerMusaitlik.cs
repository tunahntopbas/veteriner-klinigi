namespace VeterinerKlinigi.Models;

/// <summary>
/// Hekimin haftalık müsaitlik aralığı. Örnek: Salı 10:00–16:00.
/// Bir hekimin aynı gün için birden fazla aralığı olabilir (09:00–12:00 ve 14:00–18:00).
///
/// ÖDEV 2.3 — Müsaitlik saatleri tanımlanır; sahipler YALNIZCA bu aralıklara randevu alabilir.
/// Kontrolün yapıldığı yer: Services/MusaitlikServisi.cs → RandevuUygunMu().
/// </summary>
public class VeterinerMusaitlik
{
    public int Id { get; set; }

    public int VeterinerId { get; set; }
    public Veteriner? Veteriner { get; set; }

    public DayOfWeek Gun { get; set; }
    public TimeOnly BaslangicSaati { get; set; }
    public TimeOnly BitisSaati { get; set; }
}
