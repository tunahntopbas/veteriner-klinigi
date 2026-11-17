using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Areas.Admin.Controllers;

/// <summary>
/// ÖDEV 1.1 — Klinik tanımlama ("yetki çerçevesinde" → sadece Admin).
/// ÖDEV 1.2 — Kliniğin çalışma saatleri.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class KlinikController : Controller
{
    private readonly IKlinikServisi _klinik;

    public KlinikController(IKlinikServisi klinik)
    {
        _klinik = klinik;
    }

    // GET /Admin/Klinik
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Klinik ve Çalışma Saatleri";
        var klinik = await _klinik.GetirAsync();

        // Entity → ViewModel. Haftanın 7 günü Pazartesiden başlayarak, kaydı olmayan gün "kapalı".
        var vm = new KlinikFormVM
        {
            Ad = klinik.Ad,
            Adres = klinik.Adres,
            Telefon = klinik.Telefon,
            Saatler = GunAdlari.HaftaSirasi.Select(gun =>
            {
                var c = klinik.CalismaSaatleri.FirstOrDefault(x => x.Gun == gun);
                return new CalismaSaatiSatirVM
                {
                    Gun = gun,
                    AcilisSaati = c?.AcilisSaati ?? new TimeOnly(9, 0),
                    KapanisSaati = c?.KapanisSaati ?? new TimeOnly(18, 0),
                    Kapali = c?.Kapali ?? true
                };
            }).ToList()
        };
        return View(vm);
    }

    // POST /Admin/Klinik
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(KlinikFormVM girdi)
    {
        ViewData["Title"] = "Klinik ve Çalışma Saatleri";
        if (!ModelState.IsValid)
            return View(girdi);

        var saatler = girdi.Saatler.Select(s => new CalismaSaati
        {
            Gun = s.Gun, AcilisSaati = s.AcilisSaati, KapanisSaati = s.KapanisSaati, Kapali = s.Kapali
        }).ToList();

        var sonuc = await _klinik.GuncelleAsync(girdi.Ad, girdi.Adres, girdi.Telefon, saatler);
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View(girdi);
        }

        TempData["Mesaj"] = "Klinik bilgileri kaydedildi.";
        return RedirectToAction(nameof(Index));   // PRG
    }
}
