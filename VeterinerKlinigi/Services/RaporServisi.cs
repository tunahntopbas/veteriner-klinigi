using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// Raporlar.
/// ÖDEV 5.2 — "Hizmet türüne göre randevu sayısı ve ciro raporu" (LINQ GroupBy).
/// Kullanan: GET /api/raporlar/hizmet-ozeti (Admin) → Admin panelindeki rapor sayfası jQuery ile çağırır.
/// </summary>
public interface IRaporServisi
{
    Task<List<HizmetOzeti>> HizmetOzetiAsync(DateOnly baslangic, DateOnly bitis);
}

public record HizmetOzeti(int HizmetId, string HizmetAdi, int RandevuSayisi, int TamamlananSayisi, decimal Ciro);

public class RaporServisi : IRaporServisi
{
    private readonly UygulamaDbContext _db;

    public RaporServisi(UygulamaDbContext db)
    {
        _db = db;
    }

    public async Task<List<HizmetOzeti>> HizmetOzetiAsync(DateOnly baslangic, DateOnly bitis)
    {
        var bas = baslangic.ToDateTime(TimeOnly.MinValue);
        var bit = bitis.ToDateTime(TimeOnly.MinValue).AddDays(1); // bitiş günü DAHİL

        // LINQ → SQL: WHERE + GROUP BY + COUNT + SUM tek sorguda, veritabanında çalışır.
        //   SELECT "HizmetId", h."Ad", COUNT(*), COUNT(*) FILTER (...), SUM(...) ... GROUP BY ...
        // Reddedilen ve iptal edilen randevular sayılmaz.
        // Ciro = sadece TAMAMLANAN randevuların (randevu anındaki) ücretleri.
        var gruplar = await _db.Randevular
            .Where(r => r.BaslangicZamani >= bas && r.BaslangicZamani < bit
                        && r.Durum != RandevuDurumu.Reddedildi && r.Durum != RandevuDurumu.IptalEdildi)
            .GroupBy(r => new { r.HizmetId, r.Hizmet!.Ad })
            .Select(g => new
            {
                g.Key.HizmetId,
                g.Key.Ad,
                Toplam = g.Count(),
                Tamamlanan = g.Count(r => r.Durum == RandevuDurumu.Tamamlandi),
                // Koşullu toplam: tamamlandıysa ücreti, değilse 0 ekle → SQL'de SUM(CASE WHEN ...)
                Ciro = g.Sum(r => r.Durum == RandevuDurumu.Tamamlandi ? r.Ucret : 0m)
            })
            .ToListAsync();

        // Sıralama ve record'a dönüştürme bellekte (veri zaten gruplanmış, birkaç satır).
        return gruplar
            .Select(g => new HizmetOzeti(g.HizmetId, g.Ad, g.Toplam, g.Tamamlanan, g.Ciro))
            .OrderByDescending(o => o.Ciro).ThenByDescending(o => o.RandevuSayisi)
            .ToList();
    }
}
