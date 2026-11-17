using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// Hizmet CRUD işlemleri.
/// ÖDEV 1.2 — Hizmetlerin süre ve ücretleri.
/// ÖDEV Beklenen-CRUD — Hizmet için tam CRUD (Admin paneli bu servisi kullanır).
/// </summary>
public interface IHizmetServisi
{
    Task<List<Hizmet>> ListeleAsync(bool sadeceAktifler);
    Task<Hizmet?> GetirAsync(int id);
    Task<IslemSonucu<Hizmet>> EkleAsync(Hizmet hizmet);
    Task<IslemSonucu> GuncelleAsync(int id, Hizmet bilgiler);
    Task<IslemSonucu> SilAsync(int id);
}

public class HizmetServisi : IHizmetServisi
{
    private readonly UygulamaDbContext _db;

    public HizmetServisi(UygulamaDbContext db)
    {
        _db = db;
    }

    public async Task<List<Hizmet>> ListeleAsync(bool sadeceAktifler) =>
        await _db.Hizmetler
            .Where(h => !sadeceAktifler || h.Aktif)
            .OrderBy(h => h.Ad)
            .AsNoTracking()                 // sadece okuyacağız → EF değişiklik takibi yapmasın (hızlı)
            .ToListAsync();

    public async Task<Hizmet?> GetirAsync(int id) =>
        await _db.Hizmetler
            .Include(h => h.Veterinerler)
            .FirstOrDefaultAsync(h => h.Id == id);

    public async Task<IslemSonucu<Hizmet>> EkleAsync(Hizmet hizmet)
    {
        if (await AyniAdVarMiAsync(hizmet.Ad, haricId: null))
            return IslemSonucu<Hizmet>.Basarisiz("Bu adla bir hizmet zaten var.");

        // Tek klinik: hizmet sistemdeki kliniğe bağlanır.
        hizmet.KlinikId = await _db.Klinikler.OrderBy(k => k.Id).Select(k => k.Id).FirstAsync();
        _db.Hizmetler.Add(hizmet);
        await _db.SaveChangesAsync();
        return IslemSonucu<Hizmet>.Tamam(hizmet);
    }

    public async Task<IslemSonucu> GuncelleAsync(int id, Hizmet bilgiler)
    {
        var hizmet = await _db.Hizmetler.FindAsync(id);
        if (hizmet is null)
            return IslemSonucu.KayitYok();
        if (await AyniAdVarMiAsync(bilgiler.Ad, haricId: id))
            return IslemSonucu.Basarisiz("Bu adla bir hizmet zaten var.");

        hizmet.Ad = bilgiler.Ad;
        hizmet.Aciklama = bilgiler.Aciklama;
        hizmet.SureDakika = bilgiler.SureDakika;
        hizmet.Ucret = bilgiler.Ucret;
        hizmet.Aktif = bilgiler.Aktif;
        // DİKKAT: Mevcut randevuların süre/ücreti DEĞİŞMEZ; onlar kendi kopyalarını tutuyor (ÖDEV 4.3).

        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> SilAsync(int id)
    {
        var hizmet = await _db.Hizmetler.FindAsync(id);
        if (hizmet is null)
            return IslemSonucu.KayitYok();

        // Randevusu olan hizmet silinirse geçmiş kayıtlar ve ciro raporu bozulur.
        if (await _db.Randevular.AnyAsync(r => r.HizmetId == id))
            return IslemSonucu.Basarisiz("Bu hizmete ait randevular var; silmek yerine 'Pasif' yapın.");

        _db.Hizmetler.Remove(hizmet);
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    private Task<bool> AyniAdVarMiAsync(string ad, int? haricId) =>
        _db.Hizmetler.AnyAsync(h => h.Ad.ToLower() == ad.ToLower() && h.Id != haricId);
}
