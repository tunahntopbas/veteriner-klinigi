using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// Uzmanlık alanı CRUD işlemleri (küçük hayvan, egzotik, cerrahi, dermatoloji...).
/// ÖDEV 2.2 — Hekimlerin uzmanlık alanları.
/// </summary>
public interface IUzmanlikServisi
{
    Task<List<Uzmanlik>> ListeleAsync();
    Task<Uzmanlik?> GetirAsync(int id);
    Task<IslemSonucu> EkleAsync(string ad);
    Task<IslemSonucu> GuncelleAsync(int id, string ad);
    Task<IslemSonucu> SilAsync(int id);
}

public class UzmanlikServisi : IUzmanlikServisi
{
    private readonly UygulamaDbContext _db;

    public UzmanlikServisi(UygulamaDbContext db)
    {
        _db = db;
    }

    public async Task<List<Uzmanlik>> ListeleAsync() =>
        await _db.Uzmanliklar.Include(u => u.Veterinerler).OrderBy(u => u.Ad).AsNoTracking().ToListAsync();

    public async Task<Uzmanlik?> GetirAsync(int id) =>
        await _db.Uzmanliklar.Include(u => u.Veterinerler).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<IslemSonucu> EkleAsync(string ad)
    {
        if (await _db.Uzmanliklar.AnyAsync(u => u.Ad.ToLower() == ad.ToLower()))
            return IslemSonucu.Basarisiz("Bu uzmanlık zaten tanımlı.");
        _db.Uzmanliklar.Add(new Uzmanlik { Ad = ad });
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> GuncelleAsync(int id, string ad)
    {
        var uzmanlik = await _db.Uzmanliklar.FindAsync(id);
        if (uzmanlik is null)
            return IslemSonucu.KayitYok();
        if (await _db.Uzmanliklar.AnyAsync(u => u.Ad.ToLower() == ad.ToLower() && u.Id != id))
            return IslemSonucu.Basarisiz("Bu uzmanlık zaten tanımlı.");
        uzmanlik.Ad = ad;
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> SilAsync(int id)
    {
        var uzmanlik = await _db.Uzmanliklar.FindAsync(id);
        if (uzmanlik is null)
            return IslemSonucu.KayitYok();
        // Çok ↔ çok ara tablodaki (VeterinerUzmanliklari) satırlar EF tarafından otomatik silinir.
        _db.Uzmanliklar.Remove(uzmanlik);
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }
}
