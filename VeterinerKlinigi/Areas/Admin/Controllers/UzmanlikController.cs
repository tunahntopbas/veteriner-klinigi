using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Areas.Admin.Controllers;

/// <summary>
/// ÖDEV 2.2 — Uzmanlık alanları (küçük hayvan, egzotik, cerrahi, dermatoloji...).
/// Listele · Ekle · Duzenle · Sil.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class UzmanlikController : Controller
{
    private readonly IUzmanlikServisi _uzmanliklar;

    public UzmanlikController(IUzmanlikServisi uzmanliklar)
    {
        _uzmanliklar = uzmanliklar;
    }

    // GET /Admin/Uzmanlik
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Uzmanlık Alanları";
        return View(await _uzmanliklar.ListeleAsync());
    }

    // GET /Admin/Uzmanlik/Ekle
    [HttpGet]
    public IActionResult Ekle()
    {
        ViewData["Title"] = "Yeni Uzmanlık";
        return View("Form", new UzmanlikFormVM());
    }

    // POST /Admin/Uzmanlik/Ekle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(UzmanlikFormVM girdi)
    {
        ViewData["Title"] = "Yeni Uzmanlık";
        if (!ModelState.IsValid)
            return View("Form", girdi);

        var sonuc = await _uzmanliklar.EkleAsync(girdi.Ad.Trim());
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(nameof(girdi.Ad), sonuc.Hata!);
            return View("Form", girdi);
        }
        TempData["Mesaj"] = "Uzmanlık eklendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Admin/Uzmanlik/Duzenle/2
    [HttpGet]
    public async Task<IActionResult> Duzenle(int id)
    {
        var u = await _uzmanliklar.GetirAsync(id);
        if (u is null)
            return NotFound();
        ViewData["Title"] = "Uzmanlığı Düzenle";
        return View("Form", new UzmanlikFormVM { Id = u.Id, Ad = u.Ad });
    }

    // POST /Admin/Uzmanlik/Duzenle/2
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, UzmanlikFormVM girdi)
    {
        ViewData["Title"] = "Uzmanlığı Düzenle";
        if (!ModelState.IsValid)
            return View("Form", girdi);

        var sonuc = await _uzmanliklar.GuncelleAsync(id, girdi.Ad.Trim());
        if (sonuc.Bulunamadi)
            return NotFound();
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(nameof(girdi.Ad), sonuc.Hata!);
            return View("Form", girdi);
        }
        TempData["Mesaj"] = "Uzmanlık güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/Uzmanlik/Sil/2 (listedeki "Sil" düğmesi, tarayıcı onay kutusuyla)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var sonuc = await _uzmanliklar.SilAsync(id);
        if (sonuc.Bulunamadi)
            return NotFound();
        TempData["Mesaj"] = "Uzmanlık silindi.";
        return RedirectToAction(nameof(Index));
    }
}
