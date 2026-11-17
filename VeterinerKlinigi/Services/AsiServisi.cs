using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// Aşı kayıtları.
/// ÖDEV 3.3 — Aşı adı, yapıldığı tarih, bir sonraki doz tarihi.
/// ÖDEV Beklenen-CRUD — Aşı Kaydı için tam CRUD.
///
/// AsiTakvimiAsync: hem REST API (GET /api/evcil-hayvanlar/{id}/asilar)
/// hem MCP aracı (asi_takvimi) bu TEK metodu çağırır (ÖDEV 6.3).
///
/// Yetki: Aşı kaydını hayvanın sahibi, o hayvana bakan hekim ve Admin ekleyebilir/düzenleyebilir.
/// </summary>
public interface IAsiServisi
{
    Task<AsiKaydi?> GetirAsync(int id, KullaniciBilgisi kb);
    Task<AsiTakvimi?> AsiTakvimiAsync(int evcilHayvanId, KullaniciBilgisi kb);
    Task<IslemSonucu<AsiKaydi>> EkleAsync(AsiKaydi asi, KullaniciBilgisi kb);
    Task<IslemSonucu> GuncelleAsync(int id, AsiKaydi bilgiler, KullaniciBilgisi kb);
    Task<IslemSonucu> SilAsync(int id, KullaniciBilgisi kb);
}

/// <summary>Bir hayvanın aşı durumu: yapılmış aşılar + yaklaşan dozlar.</summary>
public record AsiTakvimi(EvcilHayvan Hayvan, List<AsiKaydi> Gecmis, List<AsiKaydi> Yaklasan);

public class AsiServisi : IAsiServisi
{
    private readonly UygulamaDbContext _db;

    public AsiServisi(UygulamaDbContext db)
    {
        _db = db;
    }

    public async Task<AsiKaydi?> GetirAsync(int id, KullaniciBilgisi kb) =>
        await _db.AsiKayitlari
            .Include(a => a.EvcilHayvan)
            // Aşının hayvanı kullanıcının erişebildiği hayvanlardan biri mi?
            .Where(a => _db.EvcilHayvanlar.ErisilebilirHayvanlar(kb).Any(h => h.Id == a.EvcilHayvanId))
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<AsiTakvimi?> AsiTakvimiAsync(int evcilHayvanId, KullaniciBilgisi kb)
    {
        var hayvan = await _db.EvcilHayvanlar.ErisilebilirHayvanlar(kb)
            .AsNoTracking().FirstOrDefaultAsync(h => h.Id == evcilHayvanId);
        if (hayvan is null)
            return null;

        var bugun = DateOnly.FromDateTime(DateTime.Today);

        // ÖDEV 5.2 — LINQ ile filtreleme: geçmiş = yapılmış tüm aşılar (yeniden eskiye),
        // yaklaşan = sonraki doz tarihi bugün veya sonrası olanlar (yakından uzağa).
        var asilar = await _db.AsiKayitlari
            .Where(a => a.EvcilHayvanId == evcilHayvanId)
            .AsNoTracking()
            .ToListAsync();

        var gecmis = asilar.OrderByDescending(a => a.YapildigiTarih).ToList();
        var yaklasan = asilar
            .Where(a => a.SonrakiDozTarihi != null && a.SonrakiDozTarihi >= bugun)
            .OrderBy(a => a.SonrakiDozTarihi)
            .ToList();

        return new AsiTakvimi(hayvan, gecmis, yaklasan);
    }

    public async Task<IslemSonucu<AsiKaydi>> EkleAsync(AsiKaydi asi, KullaniciBilgisi kb)
    {
        if (!await HayvanaErisebilirMiAsync(asi.EvcilHayvanId, kb))
            return IslemSonucu<AsiKaydi>.KayitYok();

        var hata = TarihKontrol(asi);
        if (hata is not null)
            return IslemSonucu<AsiKaydi>.Basarisiz(hata);

        _db.AsiKayitlari.Add(asi);
        await _db.SaveChangesAsync();
        return IslemSonucu<AsiKaydi>.Tamam(asi);
    }

    public async Task<IslemSonucu> GuncelleAsync(int id, AsiKaydi bilgiler, KullaniciBilgisi kb)
    {
        var asi = await GetirAsync(id, kb);
        if (asi is null)
            return IslemSonucu.KayitYok();

        var hata = TarihKontrol(bilgiler);
        if (hata is not null)
            return IslemSonucu.Basarisiz(hata);

        asi.AsiAdi = bilgiler.AsiAdi;
        asi.YapildigiTarih = bilgiler.YapildigiTarih;
        asi.SonrakiDozTarihi = bilgiler.SonrakiDozTarihi;
        asi.Notlar = bilgiler.Notlar;
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> SilAsync(int id, KullaniciBilgisi kb)
    {
        var asi = await GetirAsync(id, kb);
        if (asi is null)
            return IslemSonucu.KayitYok();
        _db.AsiKayitlari.Remove(asi);
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    private Task<bool> HayvanaErisebilirMiAsync(int hayvanId, KullaniciBilgisi kb) =>
        _db.EvcilHayvanlar.ErisilebilirHayvanlar(kb).AnyAsync(h => h.Id == hayvanId);

    private static string? TarihKontrol(AsiKaydi asi)
    {
        if (asi.YapildigiTarih > DateOnly.FromDateTime(DateTime.Today))
            return "Aşının yapıldığı tarih gelecekte olamaz.";
        if (asi.SonrakiDozTarihi is not null && asi.SonrakiDozTarihi <= asi.YapildigiTarih)
            return "Sonraki doz tarihi, yapıldığı tarihten sonra olmalı.";
        return null;
    }
}
