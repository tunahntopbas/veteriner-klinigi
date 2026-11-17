using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Hekim uzmanlık alanı: küçük hayvan, egzotik hayvan, cerrahi, dermatoloji...
///
/// ÖDEV 2.2 — Her hekimin uzmanlık alanları belirtilebilecek.
/// Ayrı bir tablo yaptık ki Admin yeni uzmanlık alanı ekleyebilsin ve
/// bir hekime birden fazla uzmanlık atanabilsin (çok ↔ çok).
/// </summary>
public class Uzmanlik
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Ad { get; set; } = "";

    public List<Veteriner> Veterinerler { get; set; } = [];
}
