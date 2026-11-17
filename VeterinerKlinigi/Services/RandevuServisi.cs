using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// Randevu iş kuralları — TEK YER.
///
/// ÖDEV 4.1 — Sahip hayvanını, hizmeti ve hekimi seçerek randevu alır (OlusturAsync).
/// ÖDEV 4.2 — Uygunluk ve çakışma kontrolü (MusaitlikServisi.RandevuUygunMuAsync).
/// ÖDEV 4.3 — Süre ve ücret randevuya kopyalanır.
/// ÖDEV 4.4 — Bekliyor ile oluşur; Admin veya ilgili veteriner onaylar/reddeder.
/// ÖDEV 3.4 — Tamamlanan randevuya hekim muayene notu girer.
/// ÖDEV 6.3 — RandevuController (MVC), RandevularApiController (REST) ve
///            KlinikAraclari.randevu_olustur (MCP) AYNI OlusturAsync metodunu çağırır.
/// ÖDEV Beklenen-CRUD — Randevu için oluştur, listele/detay, güncelle, iptal/sil.
/// </summary>
public interface IRandevuServisi
{
    Task<List<Randevu>> ListeleAsync(KullaniciBilgisi kb, RandevuDurumu? durum = null);
    Task<Randevu?> GetirAsync(int id, KullaniciBilgisi kb);
    Task<IslemSonucu<Randevu>> OlusturAsync(RandevuTalebi talep, KullaniciBilgisi kb);
    Task<IslemSonucu> GuncelleAsync(int id, RandevuTalebi talep, KullaniciBilgisi kb);
    Task<IslemSonucu> OnaylaAsync(int id, KullaniciBilgisi kb);
    Task<IslemSonucu> ReddetAsync(int id, string? neden, KullaniciBilgisi kb);
    Task<IslemSonucu> TamamlaAsync(int id, KullaniciBilgisi kb);
    Task<IslemSonucu> IptalEtAsync(int id, KullaniciBilgisi kb);
    Task<IslemSonucu> SilAsync(int id, KullaniciBilgisi kb);
    Task<IslemSonucu> MuayeneNotuKaydetAsync(int randevuId, string? teshis, string? tedavi, string notlar, KullaniciBilgisi kb);
}

/// <summary>Randevu oluşturma/güncelleme için gereken bilgiler (MVC, API ve MCP aynı nesneyi doldurur).</summary>
public record RandevuTalebi(int EvcilHayvanId, int HizmetId, int VeterinerId, DateTime Baslangic, string? SahipNotu);

public class RandevuServisi : IRandevuServisi
{
    private readonly UygulamaDbContext _db;
    private readonly IMusaitlikServisi _musaitlik;

    public RandevuServisi(UygulamaDbContext db, IMusaitlikServisi musaitlik)
    {
        _db = db;
        _musaitlik = musaitlik;
    }

    // Listeleme ve detay sorgularının ortak hâli (ilişkili tablolar dahil).
    private IQueryable<Randevu> DetayliSorgu() =>
        _db.Randevular
            .Include(r => r.EvcilHayvan).ThenInclude(h => h!.Sahip)
            .Include(r => r.Hizmet)
            .Include(r => r.Veteriner)
            .Include(r => r.MuayeneNotu);

    public async Task<List<Randevu>> ListeleAsync(KullaniciBilgisi kb, RandevuDurumu? durum = null) =>
        await DetayliSorgu()
            .ErisilebilirRandevular(kb)                       // ← yetki filtresi (IDOR koruması)
            .Where(r => durum == null || r.Durum == durum)
            .OrderByDescending(r => r.BaslangicZamani)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Randevu?> GetirAsync(int id, KullaniciBilgisi kb) =>
        await DetayliSorgu().ErisilebilirRandevular(kb).FirstOrDefaultAsync(r => r.Id == id);

    // ---------------------------------------------------------------
    // ÖDEV 4.1 / 4.2 / 4.3 / 4.4 — RANDEVU OLUŞTURMA
    // ---------------------------------------------------------------
    public async Task<IslemSonucu<Randevu>> OlusturAsync(RandevuTalebi talep, KullaniciBilgisi kb)
    {
        // Hayvan bu kullanıcının mı? (Admin herkes adına alabilir.)
        var hayvanBenim = await _db.EvcilHayvanlar
            .AnyAsync(h => h.Id == talep.EvcilHayvanId && (kb.AdminMi || h.SahipId == kb.Id));
        if (!hayvanBenim)
            // Bulunamadi → REST API 404 döner (başkasının hayvanının id'si denenirse "yok" gibi davranılır).
            return new IslemSonucu<Randevu> { Bulunamadi = true, Hata = "Seçilen hayvan bulunamadı." };

        // Saniye/salise kırp: 10:30:27 değil 10:30.
        var baslangic = SaniyesizYap(talep.Baslangic);

        // DİKKAT — Aynı anda iki kişi aynı boş saate tıklarsa ikisi de "boş" görüp
        // ikisi de kaydedebilir (race condition). Bunu önlemek için:
        //   transaction aç → bu hekim için PostgreSQL kilidi al (pg_advisory_xact_lock)
        //   → kontrol et → kaydet → commit (kilit otomatik bırakılır).
        // Aynı hekime gelen ikinci istek, birinci bitene kadar kilitte bekler; sonra
        // kontrol ettiğinde dolu saati görür ve "çakışıyor" cevabı alır.
        await using var islem = await _db.Database.BeginTransactionAsync();
        await KilitleriAlAsync(talep.VeterinerId, talep.EvcilHayvanId);

        // ÖDEV 4.2 — Uygun değilse sistem UYARIR (nedenini söyler).
        var uygunluk = await _musaitlik.RandevuUygunMuAsync(
            talep.VeterinerId, talep.HizmetId, baslangic, talep.EvcilHayvanId);
        if (!uygunluk.Basarili)
            return IslemSonucu<Randevu>.Basarisiz(uygunluk.Hata!);

        var hizmet = await _db.Hizmetler.FirstAsync(h => h.Id == talep.HizmetId);

        var randevu = new Randevu
        {
            EvcilHayvanId = talep.EvcilHayvanId,
            HizmetId = talep.HizmetId,
            VeterinerId = talep.VeterinerId,
            BaslangicZamani = baslangic,
            BitisZamani = baslangic.AddMinutes(hizmet.SureDakika),
            SureDakika = hizmet.SureDakika,     // ÖDEV 4.3 — o anki süre KOPYALANIR
            Ucret = hizmet.Ucret,               // ÖDEV 4.3 — o anki ücret KOPYALANIR
            Durum = RandevuDurumu.Bekliyor,     // ÖDEV 4.4 — her zaman Bekliyor ile başlar
            SahipNotu = string.IsNullOrWhiteSpace(talep.SahipNotu) ? null : talep.SahipNotu.Trim(),
            OlusturmaZamani = DateTime.Now
        };

        _db.Randevular.Add(randevu);
        await _db.SaveChangesAsync();
        await islem.CommitAsync();

        return IslemSonucu<Randevu>.Tamam(randevu);
    }

    // Sahip, henüz onaylanmamış randevusunun saatini/hekimini/hizmetini değiştirebilir.
    public async Task<IslemSonucu> GuncelleAsync(int id, RandevuTalebi talep, KullaniciBilgisi kb)
    {
        var randevu = await _db.Randevular
            .Where(r => kb.AdminMi || r.EvcilHayvan!.SahipId == kb.Id)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (randevu is null)
            return IslemSonucu.KayitYok();
        if (randevu.Durum != RandevuDurumu.Bekliyor)
            return IslemSonucu.Basarisiz("Sadece 'Bekliyor' durumundaki randevular değiştirilebilir.");

        var hayvanBenim = await _db.EvcilHayvanlar
            .AnyAsync(h => h.Id == talep.EvcilHayvanId && (kb.AdminMi || h.SahipId == kb.Id));
        if (!hayvanBenim)
            return IslemSonucu.Basarisiz("Seçilen hayvan bulunamadı.");

        var baslangic = SaniyesizYap(talep.Baslangic);

        await using var islem = await _db.Database.BeginTransactionAsync();
        await KilitleriAlAsync(talep.VeterinerId, talep.EvcilHayvanId);

        // Kendisiyle çakışmasın diye haricRandevuId = id
        var uygunluk = await _musaitlik.RandevuUygunMuAsync(
            talep.VeterinerId, talep.HizmetId, baslangic, talep.EvcilHayvanId, haricRandevuId: id);
        if (!uygunluk.Basarili)
            return uygunluk;

        var hizmet = await _db.Hizmetler.FirstAsync(h => h.Id == talep.HizmetId);
        randevu.EvcilHayvanId = talep.EvcilHayvanId;
        randevu.HizmetId = talep.HizmetId;
        randevu.VeterinerId = talep.VeterinerId;
        randevu.BaslangicZamani = baslangic;
        randevu.BitisZamani = baslangic.AddMinutes(hizmet.SureDakika);
        randevu.SureDakika = hizmet.SureDakika;
        randevu.Ucret = hizmet.Ucret;
        randevu.SahipNotu = string.IsNullOrWhiteSpace(talep.SahipNotu) ? null : talep.SahipNotu.Trim();

        await _db.SaveChangesAsync();
        await islem.CommitAsync();
        return IslemSonucu.Tamam();
    }

    // ---------------------------------------------------------------
    // ÖDEV 4.4 — ONAY MEKANİZMASI (durum makinesi)
    // ---------------------------------------------------------------

    public Task<IslemSonucu> OnaylaAsync(int id, KullaniciBilgisi kb) =>
        DurumDegistirAsync(id, RandevuDurumu.Onaylandi, kb, null);

    public Task<IslemSonucu> ReddetAsync(int id, string? neden, KullaniciBilgisi kb) =>
        DurumDegistirAsync(id, RandevuDurumu.Reddedildi, kb, neden);

    public Task<IslemSonucu> TamamlaAsync(int id, KullaniciBilgisi kb) =>
        DurumDegistirAsync(id, RandevuDurumu.Tamamlandi, kb, null);

    /// <summary>
    /// Onay / red / tamamlama: yalnızca Admin veya randevunun KENDİ hekimi.
    /// Başka hekimin randevusu sorgudan hiç dönmez → "bulunamadı".
    /// </summary>
    private async Task<IslemSonucu> DurumDegistirAsync(int id, RandevuDurumu yeniDurum, KullaniciBilgisi kb, string? redNedeni)
    {
        if (!kb.AdminMi && !kb.VeterinerMi)
            return IslemSonucu.KayitYok();

        var randevu = await _db.Randevular
            .Where(r => kb.AdminMi || r.Veteriner!.KullaniciId == kb.Id)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (randevu is null)
            return IslemSonucu.KayitYok();

        if (!GecisGecerliMi(randevu.Durum, yeniDurum))
            return IslemSonucu.Basarisiz(
                $"'{DurumAdi(randevu.Durum)}' durumundaki randevu '{DurumAdi(yeniDurum)}' yapılamaz.");

        // ÖDEV 3.4 — Muayene notu "tamamlanan" randevuya girilir. Henüz saati gelmemiş bir randevu
        // tamamlanmış sayılamaz; aksi hâlde yarınki randevuya bugünden not yazılır ve ciroya girerdi.
        // DİKKAT: "Şimdi" = sunucunun yerel saati (klinik saati, Europe/Istanbul kabul edilir).
        var simdi = DateTime.Now;
        if (yeniDurum == RandevuDurumu.Tamamlandi && randevu.BaslangicZamani > simdi)
            return IslemSonucu.Basarisiz("Henüz saati gelmemiş randevu tamamlanamaz.");

        // ÖDEV 4.4 — Saati geçmiş bir talep artık onaylanamaz (o saat geçti); yalnızca reddedilebilir.
        if (yeniDurum == RandevuDurumu.Onaylandi && randevu.BaslangicZamani <= simdi)
            return IslemSonucu.Basarisiz("Saati geçmiş randevu onaylanamaz; reddedebilirsiniz.");

        randevu.Durum = yeniDurum;
        if (yeniDurum == RandevuDurumu.Reddedildi)
            randevu.RedNedeni = string.IsNullOrWhiteSpace(redNedeni) ? null : redNedeni.Trim();

        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    // Sahip kendi randevusunu, Admin herhangi birini iptal edebilir.
    public async Task<IslemSonucu> IptalEtAsync(int id, KullaniciBilgisi kb)
    {
        var randevu = await _db.Randevular
            .Where(r => kb.AdminMi || r.EvcilHayvan!.SahipId == kb.Id)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (randevu is null)
            return IslemSonucu.KayitYok();

        if (!GecisGecerliMi(randevu.Durum, RandevuDurumu.IptalEdildi))
            return IslemSonucu.Basarisiz($"'{DurumAdi(randevu.Durum)}' durumundaki randevu iptal edilemez.");
        if (!kb.AdminMi && randevu.BaslangicZamani <= DateTime.Now)
            return IslemSonucu.Basarisiz("Saati geçmiş randevu iptal edilemez.");

        randevu.Durum = RandevuDurumu.IptalEdildi;
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    // Kalıcı silme: sadece Admin (CRUD'un D'si). Sahip "iptal" eder, kayıt geçmişte kalır.
    public async Task<IslemSonucu> SilAsync(int id, KullaniciBilgisi kb)
    {
        if (!kb.AdminMi)
            return IslemSonucu.KayitYok();
        var randevu = await _db.Randevular.FindAsync(id);
        if (randevu is null)
            return IslemSonucu.KayitYok();
        _db.Randevular.Remove(randevu);
        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    // ---------------------------------------------------------------
    // ÖDEV 3.4 — MUAYENE NOTU
    // ---------------------------------------------------------------
    public async Task<IslemSonucu> MuayeneNotuKaydetAsync(int randevuId, string? teshis, string? tedavi,
        string notlar, KullaniciBilgisi kb)
    {
        // Sadece randevunun KENDİ hekimi (Admin bile hekim adına not yazamaz).
        var randevu = await _db.Randevular
            .Include(r => r.MuayeneNotu)
            .Where(r => r.Veteriner!.KullaniciId == kb.Id)
            .FirstOrDefaultAsync(r => r.Id == randevuId);
        if (randevu is null)
            return IslemSonucu.KayitYok();
        if (randevu.Durum != RandevuDurumu.Tamamlandi)
            return IslemSonucu.Basarisiz("Muayene notu sadece 'Tamamlandı' durumundaki randevuya yazılabilir.");

        // Not varsa güncelle, yoksa oluştur.
        randevu.MuayeneNotu ??= new MuayeneNotu { RandevuId = randevu.Id };
        randevu.MuayeneNotu.Teshis = teshis;
        randevu.MuayeneNotu.Tedavi = tedavi;
        randevu.MuayeneNotu.Notlar = notlar;
        randevu.MuayeneNotu.YazilmaZamani = DateTime.Now;

        await _db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    // ---------------------------------------------------------------
    // Yardımcılar
    // ---------------------------------------------------------------

    /// <summary>
    /// Transaction içinde iki PostgreSQL "advisory" kilidi alır (transaction bitince kendiliğinden bırakılır):
    ///   (0, veterinerId)   → aynı HEKİME eşzamanlı iki randevu sıraya girer (hekim çakışması).
    ///   (1, evcilHayvanId) → aynı HAYVANA farklı hekimlerden eşzamanlı iki randevu da sıraya girer
    ///                        (hayvan aynı anda iki yerde olamaz kuralı).
    /// İki anahtarlı form kullanıyoruz ki "hekim 3" ile "hayvan 3" aynı kilit sayılmasın:
    /// ilk sayı kilidin TÜRÜ, ikinci sayı kaydın id'si.
    /// Kilitler her zaman aynı sırayla (önce hekim, sonra hayvan) alındığı için kilitlenme (deadlock) olmaz.
    /// </summary>
    private async Task KilitleriAlAsync(int veterinerId, int evcilHayvanId)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(0, {veterinerId})");
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(1, {evcilHayvanId})");
    }

    // ---------------------------------------------------------------

    /// <summary>Durum makinesi: hangi durumdan hangisine geçilebilir? (Models/RandevuDurumu.cs)</summary>
    public static bool GecisGecerliMi(RandevuDurumu mevcut, RandevuDurumu yeni) => (mevcut, yeni) switch
    {
        (RandevuDurumu.Bekliyor, RandevuDurumu.Onaylandi) => true,
        (RandevuDurumu.Bekliyor, RandevuDurumu.Reddedildi) => true,
        (RandevuDurumu.Bekliyor, RandevuDurumu.IptalEdildi) => true,
        (RandevuDurumu.Onaylandi, RandevuDurumu.Tamamlandi) => true,
        (RandevuDurumu.Onaylandi, RandevuDurumu.IptalEdildi) => true,
        _ => false
    };

    public static string DurumAdi(RandevuDurumu durum) => durum switch
    {
        RandevuDurumu.Bekliyor => "Bekliyor",
        RandevuDurumu.Onaylandi => "Onaylandı",
        RandevuDurumu.Reddedildi => "Reddedildi",
        RandevuDurumu.Tamamlandi => "Tamamlandı",
        _ => "İptal Edildi"
    };

    private static DateTime SaniyesizYap(DateTime t) =>
        new(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, DateTimeKind.Unspecified);
}
