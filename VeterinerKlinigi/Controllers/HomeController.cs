using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Controllers;

/// <summary>
/// Herkese açık sayfalar: ana sayfa, hizmetler, hekimler.
/// </summary>
public class HomeController : Controller
{
    private readonly IKlinikServisi _klinik;
    private readonly IHizmetServisi _hizmetler;

    public HomeController(IKlinikServisi klinik, IHizmetServisi hizmetler)
    {
        _klinik = klinik;
        _hizmetler = hizmetler;
    }

    // GET /
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Ana Sayfa";
        return View(await _klinik.GetirAsync());   // klinik adı + çalışma saatleri
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
