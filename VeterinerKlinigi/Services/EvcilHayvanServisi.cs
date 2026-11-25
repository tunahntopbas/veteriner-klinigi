using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// Evcil hayvan CRUD işlemleri + fotoğraf.
/// ÖDEV 3.1 / 3.2 — Sahip birden fazla hayvan kaydeder; tür, ırk, doğum tarihi, kilo, fotoğraf.
/// ÖDEV Beklenen-CRUD — Evcil Hayvan için tam CRUD.
/// ÖDEV Beklenen-Authorization — Her metot KullaniciBilgisi alır; sorgular
///   YetkiSorgulari.ErisilebilirHayvanlar ile filtrelenir (başkasının hayvanı → null → 404).
/// </summary>
public interface IEvcilHayvanServisi
{
    Task<List<EvcilHayvan>> ListeleAsync(KullaniciBilgisi kb);
    Task<EvcilHayvan?> GetirAsync(int id, KullaniciBilgisi kb);
    Task<List<EvcilHayvan>> AdIleBulAsync(string ad, KullaniciBilgisi kb);
    Task<IslemSonucu<EvcilHayvan>> EkleAsync(EvcilHayvan hayvan, IFormFile? fotograf, KullaniciBilgisi kb);
    Task<IslemSonucu> GuncelleAsync(int id, EvcilHayvan bilgiler, IFormFile? yeniFotograf, bool fotografiKaldir, KullaniciBilgisi kb);
    Task<IslemSonucu> SilAsync(int id, KullaniciBilgisi kb);
}

public class EvcilHayvanServisi : IEvcilHayvanServisi
{
    private readonly UygulamaDbContext _db;
    private readonly IFotografServisi _fotograflar;

    public EvcilHayvanServisi(UygulamaDbContext db, IFotografServisi fotograflar)
    {
        _db = db;
        _fotograflar = fotograflar;
    }

    public async Task<List<EvcilHayvan>> ListeleAsync(KullaniciBilgisi kb) =>
        await _db.EvcilHayvanlar
            .ErisilebilirHayvanlar(kb)        // ← yetki filtresi
            .Include(h => h.Sahip)
            .Include(h => h.AsiKayitlari)     // kartta "sıradaki aşı" satırı için
            .OrderBy(h => h.Ad)
            .AsNoTracking()
            .ToListAsync();

    public async Task<EvcilHayvan?> GetirAsync(int id, KullaniciBilgisi kb) =>
        await _db.EvcilHayvanlar
            .ErisilebilirHayvanlar(kb)        // ← başkasının hayvanıysa sorgu boş döner
            .Include(h => h.Sahip)
            .Include(h => h.AsiKayitlari.OrderByDescending(a => a.YapildigiTarih))
            .Include(h => h.Randevular.OrderByDescending(r => r.BaslangicZamani)).ThenInclude(r => r.Hizmet)
            .Include(h => h.Randevular).ThenInclude(r => r.Veteriner)
            .AsSplitQuery()
            .FirstOrDefaultAsync(h => h.Id == id);

    // MCP aracı asi_takvimi("Pamuk") için: isimle arama, yine sadece erişilebilir hayvanlar arasında.
    public async Task<List<EvcilHayvan>> AdIleBulAsync(string ad, KullaniciBilgisi kb) =>
        await _db.EvcilHayvanlar
            .ErisilebilirHayvanlar(kb)
            .Where(h => h.Ad.ToLower() == ad.Trim().ToLower())
            .AsNoTracking()
            .ToListAsync();

    public async Task<IslemSonucu<EvcilHayvan>> EkleAsync(EvcilHayvan hayvan, IFormFile? fotograf, KullaniciBilgisi kb)
    {
        var tarihHatasi = DogumTarihiKontrol(hayvan.DogumTarihi);
        if (tarihHatasi is not null)
            return IslemSonucu<EvcilHayvan>.Basarisiz(tarihHatasi);

        // DİKKAT: Sahip formdan GELMEZ; giriş yapan kullanıcıdır.
        hayvan.SahipId = kb.Id;
        hayvan.KayitZamani = DateTime.Now;

        if (fotograf is not null)
        {
            var foto = await _fotograflar.KaydetAsync(fotograf);
            if (!foto.Basarili)
                return IslemSonucu<EvcilHayvan>.Basarisiz(foto.Hata!);
            hayvan.FotografDosyaAdi = foto.Veri;
        }

        _db.EvcilHayvanlar.Add(hayvan);
        await _db.SaveChangesAsync();
        return IslemSonucu<EvcilHayvan>.Tamam(hayvan);
    }

    public async Task<IslemSonucu> GuncelleAsync(int id, EvcilHayvan bilgiler, IFormFile? yeniFotograf,
        bool fotografiKaldir, KullaniciBilgisi kb)
    {
        // Düzenleme: sadece sahibi veya Admin (veteriner görebilir ama düzenleyemez).
        var hayvan = await _db.EvcilHayvanlar
            .Where(h => kb.AdminMi || h.SahipId == kb.Id)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hayvan is null)
            return IslemSonucu.KayitYok();

        var tarihHatasi = DogumTarihiKontrol(bilgiler.DogumTarihi);
        if (tarihHatasi is not null)
            return IslemSonucu.Basarisiz(tarihHatasi);

        hayvan.Ad = bilgiler.Ad;
        hayvan.Tur = bilgiler.Tur;
        hayvan.Irk = bilgiler.Irk;
        hayvan.DogumTarihi = bilgiler.DogumTarihi;
        hayvan.Kilo = bilgiler.Kilo;

        if (yeniFotograf is not null)
        {
            var foto = await _fotograflar.KaydetAsync(yeniFotograf);
            if (!foto.Basarili)
                return IslemSonucu.Basarisiz(foto.Hata!);
            _fotograflar.Sil(hayvan.FotografDosyaAdi);   // eski dosya diskten silinir
            hayvan.FotografDosyaAdi = foto.Veri;
        }
        else if (fotografiKaldir)
        {
            _fotograflar.Sil(hayvan.FotografDosyaAdi);
            hayvan.FotografDosyaAdi = null;
        }

        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> SilAsync(int id, KullaniciBilgisi kb)
    {
        var hayvan = await _db.EvcilHayvanlar
            .Where(h => kb.AdminMi || h.SahipId == kb.Id)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hayvan is null)
            return IslemSonucu.KayitYok();

        var aktifRandevuVar = await _db.Randevular.AnyAsync(r => r.EvcilHayvanId == id &&
            (r.Durum == RandevuDurumu.Bekliyor || r.Durum == RandevuDurumu.Onaylandi));
        if (aktifRandevuVar)
            return IslemSonucu.Basarisiz("Bu hayvanın bekleyen veya onaylı randevusu var. Önce randevuyu iptal edin.");

        _fotograflar.Sil(hayvan.FotografDosyaAdi);
        // Aşı kayıtları ve geçmiş randevular cascade ile birlikte silinir (DbContext'teki ayar).
        _db.EvcilHayvanlar.Remove(hayvan);
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    private static string? DogumTarihiKontrol(DateOnly tarih)
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today);
        if (tarih > bugun) return "Doğum tarihi gelecekte olamaz.";
        if (tarih < bugun.AddYears(-50)) return "Doğum tarihi çok eski görünüyor.";
        return null;
    }
}
