using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Kliniğin kendisi: adı, adresi, telefonu, çalışma saatleri ve sunduğu hizmetler.
///
/// ÖDEV 1.1 — Klinik tanımlama. Ödevde "farklı klinikler tanımlanabilir" deniyor;
/// biz TEK klinikle başladık ama tablo yapısı birden fazla kliniğe hazırdır
/// (Hizmet ve Veteriner kayıtları KlinikId ile bir kliniğe bağlı).
/// Kliniği yalnızca Admin düzenleyebilir ("yetki çerçevesinde").
/// </summary>
public class Klinik
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Ad { get; set; } = "";

    [StringLength(300)]
    public string? Adres { get; set; }

    [StringLength(30)]
    public string? Telefon { get; set; }

    // ---- İlişkiler (navigation property) ----
    // Bir kliniğin haftanın her günü için bir çalışma saati kaydı vardır (1 → çok).
    public List<CalismaSaati> CalismaSaatleri { get; set; } = [];

    // Kliniğin sunduğu hizmetler (1 → çok).
    public List<Hizmet> Hizmetler { get; set; } = [];

    // Klinikte çalışan hekimler (1 → çok).
    public List<Veteriner> Veterinerler { get; set; } = [];
}
