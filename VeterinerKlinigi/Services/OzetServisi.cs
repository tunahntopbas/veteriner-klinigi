using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// ANA SAYFA ÖZETLERİ — giriş yapan kişiye "şu an benim için ne önemli?" sorusunun cevabı.
/// Diğer sayfalar liste gösterir (Hayvanlarım, Randevularım...); ana sayfa ise ÖZET ve SIRADAKİ ADIM gösterir.
///
///   Üye     → sıradaki randevu, tüm hayvanların yaklaşan aşıları (tek çizelge), hayvan şeridi, son muayene notu
///   Hekim   → bugünün programı (saat çizelgesi), onay bekleyenler, sıradaki hasta
///   Admin   → bugünkü sayılar, bu ayın cirosu, önümüzdeki 7 günün doluluğu
///
/// Yetki: üye ve hekim sorguları YetkiSorgulari filtreleriyle yazıldı (başkasının kaydı hiç dönmez).
/// Bu servis yalnızca OKUR; hiçbir kaydı değiştirmez.
/// </summary>
public interface IOzetServisi
{
    Task<UyeOzeti> UyeOzetiAsync(KullaniciBilgisi kb);
    Task<HekimOzeti?> HekimOzetiAsync(KullaniciBilgisi kb);
    Task<AdminOzeti> AdminOzetiAsync();
}

/// <summary>Aşı çizelgesindeki bir satır: hangi hayvanın hangi aşısı, kaç gün sonra.</summary>
public record YaklasanAsi(int HayvanId, string HayvanAdi, string AsiAdi, DateOnly Tarih, int KalanGun);

/// <summary>Hayvan şeridindeki bir hayvan: fotoğraf var mı, sıradaki aşısına kaç gün var.</summary>
public record HayvanOzeti(int Id, string Ad, string Tur, bool FotografVar, int? SiradakiAsiyaKalanGun);

public record UyeOzeti(
    Randevu? SiradakiRandevu,          // bugünden sonraki ilk bekleyen/onaylı randevu
    int YaklasanRandevuSayisi,         // sıradaki dahil
    List<HayvanOzeti> Hayvanlar,
    List<YaklasanAsi> YaklasanAsilar,  // önümüzdeki 60 gün, tarih sırasıyla
    Randevu? SonMuayene);              // muayene notu yazılmış en son randevu (MuayeneNotu dolu)

public record HekimOzeti(
    Veteriner Hekim,
    List<Randevu> BugunkuRandevular,   // bugün, reddedilen/iptal hariç, saat sırasıyla
    int OnayBekleyenSayisi,            // bugünden sonraki bekleyen randevular
    Randevu? SiradakiHasta);           // bugün, şu andan sonra başlayan ilk onaylı randevu

/// <summary>Admin grafiğindeki bir gün: tarih ve o günün (red/iptal hariç) randevu sayısı.</summary>
public record GunlukDoluluk(DateOnly Gun, int RandevuSayisi);

public record AdminOzeti(
    int BugunkuRandevuSayisi,
    int OnayBekleyenSayisi,
    decimal BuAyCiro,                  // bu ay TAMAMLANAN randevuların ücretleri (rapor ile aynı kural)
    int BuAyTamamlanan,
    int AktifHekimSayisi,
    List<GunlukDoluluk> Haftalik);     // bugünden başlayarak 7 gün

public class OzetServisi : IOzetServisi
{
    private readonly UygulamaDbContext _db;

    public OzetServisi(UygulamaDbContext db)
    {
        _db = db;
    }

    // Reddedilen ve iptal edilen randevular "gerçekleşmeyecek" sayılır; özetlerde gösterilmez.
    private static bool Gecerli(RandevuDurumu d) => d != RandevuDurumu.Reddedildi && d != RandevuDurumu.IptalEdildi;

    // ---------------------------------------------------------------
    // ÜYE
    // ---------------------------------------------------------------
    public async Task<UyeOzeti> UyeOzetiAsync(KullaniciBilgisi kb)
    {
        var simdi = DateTime.Now;
        var bugun = DateOnly.FromDateTime(simdi);

        // Sıradaki randevular: şu andan sonra başlayan, bekleyen veya onaylı
        var yaklasanlar = await _db.Randevular
            .ErisilebilirRandevular(kb)                        // ← yalnızca kendi hayvanlarının randevuları
            .Include(r => r.EvcilHayvan).Include(r => r.Hizmet).Include(r => r.Veteriner)
            .Where(r => r.BaslangicZamani > simdi
                        && (r.Durum == RandevuDurumu.Bekliyor || r.Durum == RandevuDurumu.Onaylandi))
            .OrderBy(r => r.BaslangicZamani)
            .AsNoTracking()
            .ToListAsync();

        // Hayvanlar ve aşı kayıtları (tek sorgu)
        var hayvanlar = await _db.EvcilHayvanlar
            .ErisilebilirHayvanlar(kb)
            .Include(h => h.AsiKayitlari)
            .OrderBy(h => h.Ad)
            .AsNoTracking()
            .ToListAsync();

        // Tüm hayvanların önümüzdeki 60 gündeki dozları → tek çizelge
        var sinir = bugun.AddDays(60);
        var asilar = hayvanlar
            .SelectMany(h => h.AsiKayitlari
                .Where(a => a.SonrakiDozTarihi >= bugun && a.SonrakiDozTarihi <= sinir)
                .Select(a => new YaklasanAsi(h.Id, h.Ad, a.AsiAdi, a.SonrakiDozTarihi!.Value,
                                             a.SonrakiDozTarihi!.Value.DayNumber - bugun.DayNumber)))
            .OrderBy(a => a.Tarih)
            .ToList();

        var hayvanOzetleri = hayvanlar.Select(h =>
        {
            var siradaki = h.AsiKayitlari
                .Where(a => a.SonrakiDozTarihi >= bugun)
                .Select(a => (int?)(a.SonrakiDozTarihi!.Value.DayNumber - bugun.DayNumber))
                .Min();
            return new HayvanOzeti(h.Id, h.Ad, h.Tur, h.FotografDosyaAdi is not null, siradaki);
        }).ToList();

        // Son muayene notu: notu yazılmış en son randevu
        var sonMuayene = await _db.Randevular
            .ErisilebilirRandevular(kb)
            .Include(r => r.EvcilHayvan).Include(r => r.Hizmet).Include(r => r.Veteriner).Include(r => r.MuayeneNotu)
            .Where(r => r.MuayeneNotu != null)
            .OrderByDescending(r => r.MuayeneNotu!.YazilmaZamani)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return new UyeOzeti(yaklasanlar.FirstOrDefault(), yaklasanlar.Count, hayvanOzetleri, asilar, sonMuayene);
    }

    // ---------------------------------------------------------------
    // HEKİM
    // ---------------------------------------------------------------
    public async Task<HekimOzeti?> HekimOzetiAsync(KullaniciBilgisi kb)
    {
        var hekim = await _db.Veterinerler.AsNoTracking().FirstOrDefaultAsync(v => v.KullaniciId == kb.Id);
        if (hekim is null)
            return null;

        var simdi = DateTime.Now;
        var bugun = simdi.Date;

        var bugunku = await _db.Randevular
            .ErisilebilirRandevular(kb)                        // ← yalnızca kendi randevuları
            .Include(r => r.EvcilHayvan).Include(r => r.Hizmet)
            .Where(r => r.BaslangicZamani >= bugun && r.BaslangicZamani < bugun.AddDays(1))
            .OrderBy(r => r.BaslangicZamani)
            .AsNoTracking()
            .ToListAsync();
        bugunku = bugunku.Where(r => Gecerli(r.Durum)).ToList();

        var bekleyen = await _db.Randevular
            .ErisilebilirRandevular(kb)
            .CountAsync(r => r.Durum == RandevuDurumu.Bekliyor && r.BaslangicZamani > simdi);

        var siradaki = bugunku.FirstOrDefault(r => r.BaslangicZamani > simdi && r.Durum == RandevuDurumu.Onaylandi);

        return new HekimOzeti(hekim, bugunku, bekleyen, siradaki);
    }

    // ---------------------------------------------------------------
    // ADMİN
    // ---------------------------------------------------------------
    public async Task<AdminOzeti> AdminOzetiAsync()
    {
        var simdi = DateTime.Now;
        var bugun = simdi.Date;
        var ayBasi = new DateTime(bugun.Year, bugun.Month, 1);

        var bugunkuSayi = await _db.Randevular.CountAsync(r =>
            r.BaslangicZamani >= bugun && r.BaslangicZamani < bugun.AddDays(1)
            && r.Durum != RandevuDurumu.Reddedildi && r.Durum != RandevuDurumu.IptalEdildi);

        var bekleyen = await _db.Randevular.CountAsync(r => r.Durum == RandevuDurumu.Bekliyor && r.BaslangicZamani > simdi);

        // Ciro kuralı hizmet raporuyla aynı: yalnızca TAMAMLANAN randevuların ücreti
        var buAy = _db.Randevular.Where(r => r.Durum == RandevuDurumu.Tamamlandi
                                            && r.BaslangicZamani >= ayBasi && r.BaslangicZamani < ayBasi.AddMonths(1));
        var ciro = await buAy.SumAsync(r => (decimal?)r.Ucret) ?? 0;
        var tamamlanan = await buAy.CountAsync();

        var hekimSayisi = await _db.Veterinerler.CountAsync(v => v.Aktif);

        // Önümüzdeki 7 gün: veritabanında GROUP BY ile sayılır, boş günler 0 olarak doldurulur
        var son = bugun.AddDays(7);
        var gunSayilari = await _db.Randevular
            .Where(r => r.BaslangicZamani >= bugun && r.BaslangicZamani < son
                        && r.Durum != RandevuDurumu.Reddedildi && r.Durum != RandevuDurumu.IptalEdildi)
            .GroupBy(r => r.BaslangicZamani.Date)
            .Select(g => new { Gun = g.Key, Sayi = g.Count() })
            .ToListAsync();
        var haftalik = Enumerable.Range(0, 7)
            .Select(i => bugun.AddDays(i))
            .Select(g => new GunlukDoluluk(DateOnly.FromDateTime(g), gunSayilari.FirstOrDefault(x => x.Gun == g)?.Sayi ?? 0))
            .ToList();

        return new AdminOzeti(bugunkuSayi, bekleyen, ciro, tamamlanan, hekimSayisi, haftalik);
    }
}
