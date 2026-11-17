using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// Klinik bilgileri ve çalışma saatleri.
/// ÖDEV 1.1 / 1.2 — Tek klinik kabulüyle: sistemdeki ilk (ve tek) klinik kaydı.
/// Düzenleme yetkisi controller'da [Authorize(Roles = Admin)] ile verilir.
/// </summary>
public interface IKlinikServisi
{
    Task<Klinik> GetirAsync();
    Task<IslemSonucu> GuncelleAsync(string ad, string? adres, string? telefon, List<CalismaSaati> saatler);
}

public class KlinikServisi : IKlinikServisi
{
    private readonly UygulamaDbContext _db;

    public KlinikServisi(UygulamaDbContext db)
    {
        _db = db;
    }

    public async Task<Klinik> GetirAsync() =>
        await _db.Klinikler
            .Include(k => k.CalismaSaatleri.OrderBy(c => c.Gun))
            .OrderBy(k => k.Id)
            .FirstAsync();

    public async Task<IslemSonucu> GuncelleAsync(string ad, string? adres, string? telefon, List<CalismaSaati> saatler)
    {
        // Her açık gün için açılış < kapanış olmalı.
        foreach (var s in saatler.Where(s => !s.Kapali))
        {
            if (s.AcilisSaati >= s.KapanisSaati)
                return IslemSonucu.Basarisiz($"{GunAdlari.Ad(s.Gun)}: açılış saati kapanıştan önce olmalı.");
        }

        var klinik = await GetirAsync();
        klinik.Ad = ad;
        klinik.Adres = adres;
        klinik.Telefon = telefon;

        foreach (var yeni in saatler)
        {
            var mevcut = klinik.CalismaSaatleri.FirstOrDefault(c => c.Gun == yeni.Gun);
            if (mevcut is null)
            {
                klinik.CalismaSaatleri.Add(new CalismaSaati
                {
                    Gun = yeni.Gun, AcilisSaati = yeni.AcilisSaati, KapanisSaati = yeni.KapanisSaati, Kapali = yeni.Kapali
                });
            }
            else
            {
                mevcut.AcilisSaati = yeni.AcilisSaati;
                mevcut.KapanisSaati = yeni.KapanisSaati;
                mevcut.Kapali = yeni.Kapali;
            }
        }

        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }
}

/// <summary>DayOfWeek → Türkçe gün adı. Görünümlerde ve hata mesajlarında kullanılır.</summary>
public static class GunAdlari
{
    // Pazartesiden başlayan sıra (DayOfWeek.Sunday = 0 olduğu için ayrıca sıralıyoruz).
    public static readonly DayOfWeek[] HaftaSirasi =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    public static string Ad(DayOfWeek gun) => gun switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        DayOfWeek.Saturday => "Cumartesi",
        _ => "Pazar"
    };
}
