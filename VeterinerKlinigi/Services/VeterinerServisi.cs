using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// Hekim yönetimi.
/// ÖDEV 2.1 — Hekim tanımlama (giriş hesabıyla birlikte).
/// ÖDEV 2.2 — Uzmanlık alanları ve verebildiği hizmetler.
/// ÖDEV 2.3 — Müsaitlik aralıkları ekleme/silme.
/// ÖDEV Beklenen-CRUD — Veteriner için tam CRUD.
/// </summary>
public interface IVeterinerServisi
{
    Task<List<Veteriner>> ListeleAsync(bool sadeceAktifler);
    Task<Veteriner?> GetirAsync(int id);
    Task<Veteriner?> KullaniciIleGetirAsync(string kullaniciId);
    Task<IslemSonucu<Veteriner>> EkleAsync(Veteriner veteriner, string eposta, string sifre, int[] uzmanlikIdleri, int[] hizmetIdleri);
    Task<IslemSonucu> GuncelleAsync(int id, Veteriner bilgiler, int[] uzmanlikIdleri, int[] hizmetIdleri);
    Task<IslemSonucu> SilAsync(int id);
    Task<IslemSonucu> MusaitlikEkleAsync(int veterinerId, DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis);
    Task<IslemSonucu> MusaitlikSilAsync(int veterinerId, int musaitlikId);
}

public class VeterinerServisi : IVeterinerServisi
{
    private readonly UygulamaDbContext _db;
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;

    public VeterinerServisi(UygulamaDbContext db, UserManager<Kullanici> kullaniciYoneticisi)
    {
        _db = db;
        _kullaniciYoneticisi = kullaniciYoneticisi;
    }

    public async Task<List<Veteriner>> ListeleAsync(bool sadeceAktifler) =>
        await _db.Veterinerler
            .Where(v => !sadeceAktifler || v.Aktif)
            .Include(v => v.Uzmanliklar)
            .Include(v => v.Hizmetler)
            .Include(v => v.Kullanici)
            .OrderBy(v => v.AdSoyad)
            .AsSplitQuery()          // birden fazla Include → tek dev JOIN yerine ayrı sorgular
            .AsNoTracking()
            .ToListAsync();

    public async Task<Veteriner?> GetirAsync(int id) =>
        await _db.Veterinerler
            .Include(v => v.Uzmanliklar)
            .Include(v => v.Hizmetler)
            .Include(v => v.Musaitlikler)
            .Include(v => v.Kullanici)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<Veteriner?> KullaniciIleGetirAsync(string kullaniciId) =>
        await _db.Veterinerler.FirstOrDefaultAsync(v => v.KullaniciId == kullaniciId);

    public async Task<IslemSonucu<Veteriner>> EkleAsync(Veteriner veteriner, string eposta, string sifre,
        int[] uzmanlikIdleri, int[] hizmetIdleri)
    {
        // Hekim de giriş yapacağı için önce bir kullanıcı hesabı (AspNetUsers) oluşturulur.
        var kullanici = new Kullanici { UserName = eposta, Email = eposta, EmailConfirmed = true, AdSoyad = veteriner.AdSoyad };

        // DİKKAT: Hesap + rol + hekim kaydı ya HEP BİRLİKTE olmalı ya HİÇ (transaction).
        // Aksi hâlde hesap oluşup hekim kaydı hata verirse sahipsiz bir kullanıcı kalır.
        await using var islem = await _db.Database.BeginTransactionAsync();

        var sonuc = await _kullaniciYoneticisi.CreateAsync(kullanici, sifre);
        if (!sonuc.Succeeded)
            return IslemSonucu<Veteriner>.Basarisiz(string.Join(" ", sonuc.Errors.Select(e => e.Description)));
        await _kullaniciYoneticisi.AddToRoleAsync(kullanici, Roller.Veteriner);

        veteriner.KullaniciId = kullanici.Id;
        veteriner.KlinikId = await _db.Klinikler.OrderBy(k => k.Id).Select(k => k.Id).FirstAsync();
        veteriner.Uzmanliklar = await _db.Uzmanliklar.Where(u => uzmanlikIdleri.Contains(u.Id)).ToListAsync();
        veteriner.Hizmetler = await _db.Hizmetler.Where(h => hizmetIdleri.Contains(h.Id)).ToListAsync();

        _db.Veterinerler.Add(veteriner);
        await _db.SaveChangesAsync();
        await islem.CommitAsync();

        return IslemSonucu<Veteriner>.Tamam(veteriner);
    }

    public async Task<IslemSonucu> GuncelleAsync(int id, Veteriner bilgiler, int[] uzmanlikIdleri, int[] hizmetIdleri)
    {
        var veteriner = await GetirAsync(id);
        if (veteriner is null)
            return IslemSonucu.KayitYok();

        veteriner.AdSoyad = bilgiler.AdSoyad;
        veteriner.Unvan = bilgiler.Unvan;
        veteriner.Telefon = bilgiler.Telefon;
        veteriner.Aktif = bilgiler.Aktif;
        if (veteriner.Kullanici is not null)
            veteriner.Kullanici.AdSoyad = bilgiler.AdSoyad;

        // Çok ↔ çok: listeyi baştan kuruyoruz; EF ara tabloya ekleme/silme SQL'lerini kendisi üretir.
        veteriner.Uzmanliklar.Clear();
        veteriner.Uzmanliklar.AddRange(await _db.Uzmanliklar.Where(u => uzmanlikIdleri.Contains(u.Id)).ToListAsync());
        veteriner.Hizmetler.Clear();
        veteriner.Hizmetler.AddRange(await _db.Hizmetler.Where(h => hizmetIdleri.Contains(h.Id)).ToListAsync());

        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> SilAsync(int id)
    {
        var veteriner = await _db.Veterinerler.FindAsync(id);
        if (veteriner is null)
            return IslemSonucu.KayitYok();

        if (await _db.Randevular.AnyAsync(r => r.VeterinerId == id))
            return IslemSonucu.Basarisiz("Bu hekimin randevu geçmişi var; silmek yerine 'Pasif' yapın.");

        var kullanici = await _kullaniciYoneticisi.FindByIdAsync(veteriner.KullaniciId);
        _db.Veterinerler.Remove(veteriner);
        await _db.SaveChangesAsync();
        if (kullanici is not null)
            await _kullaniciYoneticisi.DeleteAsync(kullanici); // giriş hesabı da silinir
        return IslemSonucu.Tamam();
    }

    // ÖDEV 2.3 — Müsaitlik aralığı ekleme
    public async Task<IslemSonucu> MusaitlikEkleAsync(int veterinerId, DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis)
    {
        if (baslangic >= bitis)
            return IslemSonucu.Basarisiz("Başlangıç saati bitişten önce olmalı.");
        if (!await _db.Veterinerler.AnyAsync(v => v.Id == veterinerId))
            return IslemSonucu.KayitYok();

        // Aynı gün kendi başka aralığıyla çakışmasın (aralık çakışma formülü: aBas < bBit && bBas < aBit).
        var cakisiyor = await _db.VeterinerMusaitlikleri.AnyAsync(m =>
            m.VeterinerId == veterinerId && m.Gun == gun &&
            baslangic < m.BitisSaati && m.BaslangicSaati < bitis);
        if (cakisiyor)
            return IslemSonucu.Basarisiz("Bu aralık hekimin aynı gündeki başka bir müsaitlik aralığıyla çakışıyor.");

        _db.VeterinerMusaitlikleri.Add(new VeterinerMusaitlik
        {
            VeterinerId = veterinerId, Gun = gun, BaslangicSaati = baslangic, BitisSaati = bitis
        });
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> MusaitlikSilAsync(int veterinerId, int musaitlikId)
    {
        var musaitlik = await _db.VeterinerMusaitlikleri
            .FirstOrDefaultAsync(m => m.Id == musaitlikId && m.VeterinerId == veterinerId);
        if (musaitlik is null)
            return IslemSonucu.KayitYok();
        _db.VeterinerMusaitlikleri.Remove(musaitlik);
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }
}
