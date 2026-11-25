using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Controllers;

/// <summary>
/// Giriş sahnesi ("/"), kişisel ana sayfa (giriş gerekir), hizmetler ve hekimler (herkese açık).
/// </summary>
public class HomeController : Controller
{
    private readonly IKlinikServisi _klinik;
    private readonly IHizmetServisi _hizmetler;
    private readonly IVeterinerServisi _veterinerler;
    private readonly IOzetServisi _ozet;
    private readonly UserManager<Kullanici> _kullanicilar;

    public HomeController(IKlinikServisi klinik, IHizmetServisi hizmetler, IVeterinerServisi veterinerler,
                          IOzetServisi ozet, UserManager<Kullanici> kullanicilar)
    {
        _klinik = klinik;
        _hizmetler = hizmetler;
        _veterinerler = veterinerler;
        _ozet = ozet;
        _kullanicilar = kullanicilar;
    }

    // GET /
    // GİRİŞ SAHNESİ: 3D köpek modeli + kaydırmayla ilerleyen tanıtım (Views/Home/Index.cshtml,
    // ayrı sade layout: _GirisLayout). Sonundaki "Kliniğe gir" giriş sayfasına, "Üye ol" kayıt sayfasına götürür.
    // Sayılar veritabanından gelir; sahnede uydurma rakam olmasın.
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Hoş geldiniz";
        var klinik = await _klinik.GetirAsync();
        ViewData["KlinikAdi"] = klinik.Ad;
        ViewData["KlinikAdres"] = klinik.Adres;
        ViewData["HizmetSayisi"] = (await _hizmetler.ListeleAsync(sadeceAktifler: true)).Count;
        ViewData["HekimSayisi"] = (await _veterinerler.ListeleAsync(sadeceAktifler: true)).Count;
        return View();
    }

    // GET /Home/AnaSayfa
    // KİŞİSEL ÖZET (25.11): giriş yapan kişinin rolüne göre üye, hekim veya admin özeti.
    // Giriş gerekir: "/" → Kliniğe gir → Giriş → buraya. Liste sayfalarının (Hayvanlarım, Randevularım...)
    // tekrarı değil; "şu an benim için ne önemli?" sorusunun cevabı. Veri: OzetServisi.
    [Authorize]
    public async Task<IActionResult> AnaSayfa()
    {
        ViewData["Title"] = "Ana Sayfa";
        var kb = User.Bilgi();
        var klinik = await _klinik.GetirAsync();
        var simdi = DateTime.Now;

        // Selamlama: saate göre, yalnızca ad (soyad değil)
        var kullanici = await _kullanicilar.GetUserAsync(User);
        // Admin hesabının adı bir unvan ("Klinik Yöneticisi"); ilk kelimesi tek başına anlamsız, tamamı yazılır.
        var ad = kb.AdminMi ? kullanici?.AdSoyad ?? ""
                            : kullanici?.AdSoyad.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var selam = simdi.Hour switch { >= 5 and < 12 => "Günaydın", >= 12 and < 18 => "İyi günler", _ => "İyi akşamlar" };

        var bugunSaati = klinik.CalismaSaatleri.FirstOrDefault(c => c.Gun == simdi.DayOfWeek && !c.Kapali);
        var (durum, acik) = KlinikDurumu(klinik, simdi);

        var model = new AnaSayfaVM
        {
            Selam = ad.Length > 0 ? $"{selam}, {ad}" : selam,
            KlinikDurumu = durum,
            KlinikAcik = acik,
            BugunAcilis = bugunSaati?.AcilisSaati,
            BugunKapanis = bugunSaati?.KapanisSaati,
            Uye = !kb.AdminMi && !kb.VeterinerMi ? await _ozet.UyeOzetiAsync(kb) : null,
            Hekim = kb.VeterinerMi ? await _ozet.HekimOzetiAsync(kb) : null,
            Admin = kb.AdminMi ? await _ozet.AdminOzetiAsync() : null,
            Hizmetler = !kb.AdminMi && !kb.VeterinerMi ? await _hizmetler.ListeleAsync(sadeceAktifler: true) : [],
            AcikGunler = AcikGunler(klinik, simdi, 3)
        };
        return View(model);
    }

    // "Klinik şu an açık, kapanış saati 18:00." / "Klinik şu an kapalı, açılış yarın 09:00."
    // (Saatin arkasına ek getirmiyoruz: 18:00'de ama 09:00'da; ses uyumu saate göre değişir.)
    // Saatler veritabanından (admin değiştirirse burası da değişir).
    private static (string Metin, bool Acik) KlinikDurumu(Klinik klinik, DateTime simdi)
    {
        var saat = TimeOnly.FromDateTime(simdi);
        var bugun = klinik.CalismaSaatleri.FirstOrDefault(c => c.Gun == simdi.DayOfWeek && !c.Kapali);
        if (bugun is not null && saat >= bugun.AcilisSaati && saat < bugun.KapanisSaati)
            return ($"Klinik şu an açık, kapanış saati {bugun.KapanisSaati.ToString("HH:mm")}.", true);

        // Sonraki açılış: bugün henüz açılmadıysa bugün, değilse sonraki açık gün
        for (var i = 0; i < 8; i++)
        {
            var gun = simdi.Date.AddDays(i);
            var c = klinik.CalismaSaatleri.FirstOrDefault(x => x.Gun == gun.DayOfWeek && !x.Kapali);
            if (c is null || (i == 0 && saat >= c.AcilisSaati)) continue;
            var ne = i == 0 ? "bugün" : i == 1 ? "yarın" : GunAdlari.Ad(gun.DayOfWeek);
            return ($"Klinik şu an kapalı, açılış {ne} {c.AcilisSaati.ToString("HH:mm")}.", false);
        }
        return ("Klinik şu an kapalı.", false);
    }

    // Hızlı randevu kartındaki gün çipleri: bugünden itibaren ilk N açık gün.
    // Bugün, kapanışa 30 dakikadan az kaldıysa atlanır (zaten boş saat çıkmaz).
    private static List<DateOnly> AcikGunler(Klinik klinik, DateTime simdi, int adet)
    {
        var sonuc = new List<DateOnly>();
        for (var i = 0; i < 14 && sonuc.Count < adet; i++)
        {
            var gun = simdi.Date.AddDays(i);
            var c = klinik.CalismaSaatleri.FirstOrDefault(x => x.Gun == gun.DayOfWeek && !x.Kapali);
            if (c is null) continue;
            if (i == 0 && TimeOnly.FromDateTime(simdi).AddMinutes(30) > c.KapanisSaati) continue;
            sonuc.Add(DateOnly.FromDateTime(gun));
        }
        return sonuc;
    }

    // GET /Home/Hizmetler — ÖDEV 1.2: hizmetler, süre ve ücretleriyle
    public async Task<IActionResult> Hizmetler()
    {
        ViewData["Title"] = "Hizmetlerimiz";
        return View(await _hizmetler.ListeleAsync(sadeceAktifler: true));
    }

    // GET /Home/Hekimler
    // DİKKAT: Bu action veri GÖNDERMEZ. Liste tarayıcıda jQuery ile REST API'den
    // (GET /api/veterinerler?uzmanlik=...) çekilir — ÖDEV 5.1 / 5.2 tüketim örneği.
    public IActionResult Hekimler()
    {
        ViewData["Title"] = "Hekimlerimiz";
        return View();
    }

    // GET /Home/Durum/404 — Program.cs'teki UseStatusCodePagesWithReExecute buraya yönlendirir.
    // DİKKAT: Durum kodu korunur (404 hâlâ 404); sadece gövdeye anlaşılır bir sayfa yazılır.
    public IActionResult Durum(int id)
    {
        ViewData["Title"] = id == 404 ? "Bulunamadı" : "Hata";
        Response.StatusCode = id;
        return View(id);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
