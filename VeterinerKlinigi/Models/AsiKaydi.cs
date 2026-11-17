using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Bir hayvana yapılan aşı.
///
/// ÖDEV 3.3 — Aşı adı, yapıldığı tarih ve bir sonraki doz tarihi.
/// "Yaklaşan aşılar" = SonrakiDozTarihi bugünden sonra olan kayıtlar
/// (REST API: GET /api/evcil-hayvanlar/{id}/asilar, MCP: asi_takvimi).
/// </summary>
public class AsiKaydi
{
    public int Id { get; set; }

    public int EvcilHayvanId { get; set; }
    public EvcilHayvan? EvcilHayvan { get; set; }

    [Required, StringLength(80)]
    public string AsiAdi { get; set; } = "";

    public DateOnly YapildigiTarih { get; set; }

    // Tek dozluk aşılarda boş kalabilir.
    public DateOnly? SonrakiDozTarihi { get; set; }

    [StringLength(300)]
    public string? Notlar { get; set; }
}
