using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// "Bu hekim bu saatte bu hizmeti verebilir mi?" ve "o gün kimin hangi saatleri boş?"
/// sorularının cevabı. Randevu sisteminin kalbi.
///
/// ÖDEV 2.3 — Sahipler YALNIZCA hekimin müsaitlik aralıklarına randevu alabilir.
/// ÖDEV 4.2 — Randevu saati hekimin müsaitliği ve önceki randevular dikkate alınarak
///            kontrol edilir; hizmet SÜRESİ çakışma hesabına dahildir.
///
/// Kullananlar: RandevuServisi (oluşturma/güncelleme), REST API (/api/veterinerler/uygun),
/// MCP aracı (uygun_saatler). Hepsi AYNI kuralı kullanır (ÖDEV 6.3).
/// </summary>
public interface IMusaitlikServisi
{
    Task<IslemSonucu> RandevuUygunMuAsync(int veterinerId, int hizmetId, DateTime baslangic,
        int? evcilHayvanId = null, int? haricRandevuId = null);

    Task<List<UygunHekim>> UygunHekimleriGetirAsync(int hizmetId, DateOnly tarih);
}

/// <summary>Bir hekimin o gün için boş başlangıç saatleri.</summary>
public record UygunHekim(int VeterinerId, string AdSoyad, string? Unvan, List<string> Uzmanliklar, List<TimeOnly> BosSaatler);

public class MusaitlikServisi : IMusaitlikServisi
{
    // Boş saat listesi 15 dakikalık adımlarla üretilir: 09:00, 09:15, 09:30 ...
    private const int AdimDakika = 15;

    // Çakışmaya SAYILAN durumlar: Bekliyor, Onaylandı ve Tamamlandı.
    // Tamamlanan randevu da o saati kullanmıştır (hekim muayeneyi erken bitirip işaretlemiş olabilir).
    // Reddedilen ve iptal edilen randevu ise saati BOŞALTIR.
    private static readonly RandevuDurumu[] DoluSayilanDurumlar =
        [RandevuDurumu.Bekliyor, RandevuDurumu.Onaylandi, RandevuDurumu.Tamamlandi];

    private readonly UygulamaDbContext _db;

    public MusaitlikServisi(UygulamaDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Tek bir randevu talebini kontrol eder. Uygun değilse NEDENİNİ söyler.
    /// Kontroller sırayla:
    ///   1) Geçmiş bir saat mi?
    ///   2) Hizmet aktif mi, hekim aktif mi, hekim bu hizmeti veriyor mu? (ÖDEV 2.2)
    ///   3) Klinik o gün ve saatte açık mı? (ÖDEV 1.2)
    ///   4) Hekim o saatte müsait mi? Başlangıç VE bitiş aynı aralığın içinde olmalı (ÖDEV 2.3)
    ///   5) Hekimin başka randevusuyla çakışıyor mu? (ÖDEV 4.2)
    ///   6) Aynı hayvanın aynı saatte başka randevusu var mı?
    /// </summary>
    public async Task<IslemSonucu> RandevuUygunMuAsync(int veterinerId, int hizmetId, DateTime baslangic,
        int? evcilHayvanId = null, int? haricRandevuId = null)
    {
        // 1) Geçmiş
        if (baslangic <= DateTime.Now)
            return IslemSonucu.Basarisiz("Geçmiş bir tarih/saate randevu alınamaz.");

        // 2) Hizmet ve hekim
        var hizmet = await _db.Hizmetler.AsNoTracking().FirstOrDefaultAsync(h => h.Id == hizmetId);
        if (hizmet is null || !hizmet.Aktif)
            return IslemSonucu.Basarisiz("Seçilen hizmet bulunamadı veya şu an verilmiyor.");

        var hekim = await _db.Veterinerler.Include(v => v.Hizmetler).Include(v => v.Musaitlikler)
            .AsNoTracking().FirstOrDefaultAsync(v => v.Id == veterinerId);
        if (hekim is null || !hekim.Aktif)
            return IslemSonucu.Basarisiz("Seçilen hekim bulunamadı veya aktif değil.");
        if (hekim.Hizmetler.All(h => h.Id != hizmetId))
            return IslemSonucu.Basarisiz($"{hekim.AdSoyad} '{hizmet.Ad}' hizmetini vermiyor.");

        // ÖDEV 4.2 — Bitiş saati HİZMET SÜRESİYLE hesaplanır.
        var bitis = baslangic.AddMinutes(hizmet.SureDakika);
        if (bitis.Date != baslangic.Date)
            return IslemSonucu.Basarisiz("Randevu gece yarısını geçemez.");

        var gun = baslangic.DayOfWeek;
        var basSaat = TimeOnly.FromDateTime(baslangic);
        var bitSaat = TimeOnly.FromDateTime(bitis);

        // 3) Klinik açık mı?
        var calisma = await _db.CalismaSaatleri.AsNoTracking().FirstOrDefaultAsync(c => c.Gun == gun);
        if (calisma is null || calisma.Kapali)
            return IslemSonucu.Basarisiz($"Klinik {GunAdlari.Ad(gun)} günleri kapalı.");
        if (basSaat < calisma.AcilisSaati || bitSaat > calisma.KapanisSaati)
            return IslemSonucu.Basarisiz(
                $"Klinik {GunAdlari.Ad(gun)} günü {calisma.AcilisSaati:HH\\:mm}–{calisma.KapanisSaati:HH\\:mm} arası açık. " +
                $"{hizmet.Ad} {hizmet.SureDakika} dakika sürdüğü için {basSaat:HH\\:mm}–{bitSaat:HH\\:mm} bu aralığa sığmıyor.");

        // 4) ÖDEV 2.3 — Hekimin müsaitlik aralığı: [basSaat, bitSaat] TAMAMEN bir aralığın içinde olmalı.
        var gununAraliklari = hekim.Musaitlikler.Where(m => m.Gun == gun).OrderBy(m => m.BaslangicSaati).ToList();
        if (gununAraliklari.Count == 0)
            return IslemSonucu.Basarisiz($"{hekim.AdSoyad} {GunAdlari.Ad(gun)} günleri çalışmıyor.");
        if (!gununAraliklari.Any(m => m.BaslangicSaati <= basSaat && bitSaat <= m.BitisSaati))
        {
            var araliklar = string.Join(", ", gununAraliklari.Select(m => $"{m.BaslangicSaati:HH\\:mm}–{m.BitisSaati:HH\\:mm}"));
            return IslemSonucu.Basarisiz(
                $"{hekim.AdSoyad} {GunAdlari.Ad(gun)} günü sadece {araliklar} arası müsait. " +
                $"{basSaat:HH\\:mm}–{bitSaat:HH\\:mm} ({hizmet.SureDakika} dk) bu aralıklardan birine tam sığmalı.");
        }

        // 5) ÖDEV 4.2 — ÇAKIŞMA KONTROLÜ
        // İki zaman aralığı [yeniBas, yeniBit) ve [mevcutBas, mevcutBit) ÇAKIŞIR ⇔
        //      yeniBas < mevcutBit  VE  mevcutBas < yeniBit
        // Örnek: mevcut 10:00–10:30, yeni 10:15–10:45 → 10:15 < 10:30 ve 10:00 < 10:45 → çakışır.
        //        mevcut 10:00–10:30, yeni 10:30–11:00 → 10:30 < 10:30 YANLIŞ → çakışmaz (uç uca).
        var cakisan = await _db.Randevular
            .Include(r => r.Hizmet)
            .AsNoTracking()
            .Where(r => r.VeterinerId == veterinerId
                        && DoluSayilanDurumlar.Contains(r.Durum)
                        && r.Id != haricRandevuId                        // güncellemede kendisi hariç
                        && baslangic < r.BitisZamani && r.BaslangicZamani < bitis)
            .OrderBy(r => r.BaslangicZamani)
            .FirstOrDefaultAsync();
        if (cakisan is not null)
            return IslemSonucu.Basarisiz(
                $"{hekim.AdSoyad} {cakisan.BaslangicZamani:dd.MM.yyyy HH:mm}–{cakisan.BitisZamani:HH:mm} arası " +
                $"başka bir randevuda ({cakisan.Hizmet?.Ad}). Seçtiğiniz {basSaat:HH\\:mm}–{bitSaat:HH\\:mm} aralığı bununla çakışıyor.");

        // 6) Aynı hayvan aynı anda iki yerde olamaz.
        if (evcilHayvanId is not null)
        {
            var hayvanCakisiyor = await _db.Randevular.AnyAsync(r =>
                r.EvcilHayvanId == evcilHayvanId
                && DoluSayilanDurumlar.Contains(r.Durum)
                && r.Id != haricRandevuId
                && baslangic < r.BitisZamani && r.BaslangicZamani < bitis);
            if (hayvanCakisiyor)
                return IslemSonucu.Basarisiz("Bu hayvanın aynı saatlerde başka bir randevusu var.");
        }

        return IslemSonucu.Tamam();
    }

    /// <summary>
    /// ÖDEV 5.2 — "Belirli bir tarihte seçilen hizmet için uygun hekimler" (LINQ ile filtreleme).
    /// Hizmeti veren aktif hekimlerin o günkü BOŞ başlangıç saatlerini üretir.
    /// Hem REST API (GET /api/veterinerler/uygun) hem MCP (uygun_saatler) bunu kullanır.
    /// </summary>
    public async Task<List<UygunHekim>> UygunHekimleriGetirAsync(int hizmetId, DateOnly tarih)
    {
        var hizmet = await _db.Hizmetler.AsNoTracking().FirstOrDefaultAsync(h => h.Id == hizmetId && h.Aktif);
        if (hizmet is null)
            return [];

        var gun = tarih.DayOfWeek;
        var calisma = await _db.CalismaSaatleri.AsNoTracking().FirstOrDefaultAsync(c => c.Gun == gun);
        if (calisma is null || calisma.Kapali)
            return [];

        // LINQ: aktif + bu hizmeti veren + o gün müsaitliği olan hekimler (filtre veritabanında çalışır).
        var hekimler = await _db.Veterinerler
            .Where(v => v.Aktif
                        && v.Hizmetler.Any(h => h.Id == hizmetId)
                        && v.Musaitlikler.Any(m => m.Gun == gun))
            .Include(v => v.Uzmanliklar)
            .Include(v => v.Musaitlikler.Where(m => m.Gun == gun))
            .OrderBy(v => v.AdSoyad)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();

        // O günün dolu randevuları (tek sorguda, tüm hekimler için).
        var gunBasi = tarih.ToDateTime(TimeOnly.MinValue);
        var gunSonu = gunBasi.AddDays(1);
        var hekimIdleri = hekimler.Select(h => h.Id).ToList();
        var doluRandevular = await _db.Randevular
            .Where(r => hekimIdleri.Contains(r.VeterinerId)
                        && DoluSayilanDurumlar.Contains(r.Durum)
                        && r.BaslangicZamani < gunSonu && r.BitisZamani > gunBasi)
            .Select(r => new { r.VeterinerId, r.BaslangicZamani, r.BitisZamani })
            .AsNoTracking()
            .ToListAsync();

        var simdi = DateTime.Now;
        var sonuc = new List<UygunHekim>();

        foreach (var hekim in hekimler)
        {
            var bosSaatler = new List<TimeOnly>();
            foreach (var aralik in hekim.Musaitlikler.OrderBy(m => m.BaslangicSaati))
            {
                // Hekimin aralığı ile kliniğin açık olduğu saatlerin KESİŞİMİ
                var bas = aralik.BaslangicSaati > calisma.AcilisSaati ? aralik.BaslangicSaati : calisma.AcilisSaati;
                var bit = aralik.BitisSaati < calisma.KapanisSaati ? aralik.BitisSaati : calisma.KapanisSaati;

                // Saatleri "gün başından itibaren dakika" olarak sayıyoruz (09:30 → 570).
                // TimeOnly ile toplama gece yarısında 00:00'a sarar; tamsayı ile bu sorun olmaz.
                var basDk = bas.Hour * 60 + bas.Minute;
                var bitDk = bit.Hour * 60 + bit.Minute;

                for (var dk = basDk; dk + hizmet.SureDakika <= bitDk; dk += AdimDakika)
                {
                    var t = new TimeOnly(dk / 60, dk % 60);
                    var adayBas = tarih.ToDateTime(t);
                    var adayBit = adayBas.AddMinutes(hizmet.SureDakika);
                    if (adayBas <= simdi)
                        continue; // geçmiş saat

                    // Aynı çakışma formülü (ÖDEV 4.2)
                    var dolu = doluRandevular.Any(r => r.VeterinerId == hekim.Id
                                                       && adayBas < r.BitisZamani && r.BaslangicZamani < adayBit);
                    if (!dolu)
                        bosSaatler.Add(t);
                }
            }

            if (bosSaatler.Count > 0)
                sonuc.Add(new UygunHekim(hekim.Id, hekim.AdSoyad, hekim.Unvan,
                    hekim.Uzmanliklar.Select(u => u.Ad).OrderBy(a => a).ToList(), bosSaatler));
        }

        return sonuc;
    }
}
