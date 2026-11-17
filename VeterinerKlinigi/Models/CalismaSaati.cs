using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Kliniğin haftanın bir günündeki açılış–kapanış saati.
///
/// ÖDEV 1.2 — Kliniğin çalışma saatleri. Her gün için bir satır tutulur
/// (Pazartesi 09:00–18:00, Pazar kapalı gibi).
/// Randevu kontrolünde "klinik o saatte açık mı?" sorusu bu tablodan cevaplanır.
/// </summary>
public class CalismaSaati
{
    public int Id { get; set; }

    public int KlinikId { get; set; }
    public Klinik? Klinik { get; set; }

    // DayOfWeek: .NET'in hazır enum'u (Sunday = 0, Monday = 1 ...).
    // Veritabanında sayı olarak saklanır.
    public DayOfWeek Gun { get; set; }

    // TimeOnly: sadece saat bilgisi (tarih yok). PostgreSQL'de "time" sütunu olur.
    public TimeOnly AcilisSaati { get; set; }
    public TimeOnly KapanisSaati { get; set; }

    // Klinik o gün tamamen kapalıysa true (saatler dikkate alınmaz).
    public bool Kapali { get; set; }
}
