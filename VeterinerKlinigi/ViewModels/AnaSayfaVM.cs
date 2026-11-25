using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.ViewModels;

/// <summary>
/// ANA SAYFA = giriş yapan kişinin kişisel özeti (25.11).
/// Rolüne göre üç özetten YALNIZCA biri dolu gelir: Uye, Hekim veya Admin (OzetServisi).
/// Ortak kısım: selamlama ve kliniğin şu anki durumu ("açık, kapanış saati 18:00").
///
/// "Hızlı randevu" kartındaki boş saatler burada DEĞİL: sayfa açıldıktan sonra jQuery ile
/// REST API'den (GET /api/veterinerler/uygun) çekilir (ÖDEV 5.1). Burada yalnızca kartın
/// seçenekleri var: hizmet listesi ve önümüzdeki açık günler.
/// </summary>
public class AnaSayfaVM
{
    public required string Selam { get; init; }            // "Günaydın, Ayşe"
    public required string KlinikDurumu { get; init; }     // "Klinik şu an açık, kapanış saati 18:00."
    public required bool KlinikAcik { get; init; }

    public UyeOzeti? Uye { get; init; }
    public HekimOzeti? Hekim { get; init; }
    public AdminOzeti? Admin { get; init; }

    // Hızlı randevu kartı (yalnızca üye)
    public List<Hizmet> Hizmetler { get; init; } = [];
    public List<DateOnly> AcikGunler { get; init; } = [];   // bugünden itibaren ilk 3 açık gün

    // Hekim çizelgesi için bugünün açılış/kapanış saatleri (kapalıysa null)
    public TimeOnly? BugunAcilis { get; init; }
    public TimeOnly? BugunKapanis { get; init; }
}
