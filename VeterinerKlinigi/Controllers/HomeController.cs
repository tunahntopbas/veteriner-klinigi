using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Controllers;

/// <summary>
/// Herkese açık sayfalar: ana sayfa, hizmetler, hekimler.
/// </summary>
public class HomeController : Controller
{
    private readonly IKlinikServisi _klinik;
    private readonly IHizmetServisi _hizmetler;
    private readonly IVeterinerServisi _veterinerler;

    public HomeController(IKlinikServisi klinik, IHizmetServisi hizmetler, IVeterinerServisi veterinerler)
    {
        _klinik = klinik;
        _hizmetler = hizmetler;
        _veterinerler = veterinerler;
    }

    // GET /
    // GİRİŞ SAHNESİ: 3D köpek modeli + kaydırmayla ilerleyen tanıtım (Views/Home/Index.cshtml,
    // ayrı sade layout: _GirisLayout). Sonundaki "Kliniğe gir" butonu AnaSayfa'ya götürür.
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
    // Ana sayfa: klinik bilgisi + hizmetler + hekimler (sunucuda hazırlanır).
    // Boş saatler kutusu tarayıcıda REST API'den doldurulur (ÖDEV 5.1).
    public async Task<IActionResult> AnaSayfa()
    {
        ViewData["Title"] = "Ana Sayfa";
        ViewData["TamGenislik"] = true;   // Layout: bu sayfa kendi tam genişlik bölümlerini çizer
        var model = new AnaSayfaVM
        {
            Klinik = await _klinik.GetirAsync(),
            Hizmetler = await _hizmetler.ListeleAsync(sadeceAktifler: true),
            Hekimler = await _veterinerler.ListeleAsync(sadeceAktifler: true)
        };
        return View(model);
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
