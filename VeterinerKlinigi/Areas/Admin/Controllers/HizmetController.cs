using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Areas.Admin.Controllers;

/// <summary>
/// ÖDEV 1.2 — Hizmetler (süre ve ücret).
/// ÖDEV Beklenen-CRUD — Hizmet: Listele (Index) · Detay · Ekle · Duzenle · Sil.
///
/// Kalıp (her CRUD controller'da aynı):
///   GET  → boş/dolu formu göster
///   POST → ModelState kontrol → servisi çağır → hata varsa formu hatalarla geri göster
///          → başarılıysa TempData mesajı + RedirectToAction (PRG: F5 tekrar kaydetmesin)
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class HizmetController : Controller
{
    private readonly IHizmetServisi _hizmetler;

    public HizmetController(IHizmetServisi hizmetler)
    {
        _hizmetler = hizmetler;
    }

    // GET /Admin/Hizmet
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Hizmetler";
        return View(await _hizmetler.ListeleAsync(sadeceAktifler: false));
    }

    // GET /Admin/Hizmet/Detay/3
    public async Task<IActionResult> Detay(int id)
    {
        var hizmet = await _hizmetler.GetirAsync(id);
        if (hizmet is null)
            return NotFound();
        ViewData["Title"] = hizmet.Ad;
        return View(hizmet);
    }

    // GET /Admin/Hizmet/Ekle
    [HttpGet]
    public IActionResult Ekle()
    {
        ViewData["Title"] = "Yeni Hizmet";
        return View("Form", new HizmetFormVM());
    }

    // POST /Admin/Hizmet/Ekle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(HizmetFormVM girdi)
    {
        ViewData["Title"] = "Yeni Hizmet";
        if (!ModelState.IsValid)
            return View("Form", girdi);

        var sonuc = await _hizmetler.EkleAsync(VmdenEntity(girdi));
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(nameof(girdi.Ad), sonuc.Hata!);
            return View("Form", girdi);
        }

        TempData["Mesaj"] = $"'{girdi.Ad}' hizmeti eklendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Admin/Hizmet/Duzenle/3
    [HttpGet]
    public async Task<IActionResult> Duzenle(int id)
    {
        var h = await _hizmetler.GetirAsync(id);
        if (h is null)
            return NotFound();

        ViewData["Title"] = "Hizmeti Düzenle";
        return View("Form", new HizmetFormVM
        {
            Id = h.Id, Ad = h.Ad, Aciklama = h.Aciklama, SureDakika = h.SureDakika, Ucret = h.Ucret, Aktif = h.Aktif
        });
    }

    // POST /Admin/Hizmet/Duzenle/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, HizmetFormVM girdi)
    {
        ViewData["Title"] = "Hizmeti Düzenle";
        if (!ModelState.IsValid)
            return View("Form", girdi);

        var sonuc = await _hizmetler.GuncelleAsync(id, VmdenEntity(girdi));
        if (sonuc.Bulunamadi)
            return NotFound();
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(nameof(girdi.Ad), sonuc.Hata!);
            return View("Form", girdi);
        }

        TempData["Mesaj"] = "Hizmet güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Admin/Hizmet/Sil/3 → onay sayfası
    [HttpGet]
    public async Task<IActionResult> Sil(int id)
    {
        var hizmet = await _hizmetler.GetirAsync(id);
        if (hizmet is null)
            return NotFound();
        ViewData["Title"] = "Hizmeti Sil";
        return View(hizmet);
    }

    // POST /Admin/Hizmet/Sil/3 → gerçekten sil
    // DİKKAT: Silme GET ile YAPILMAZ. Bir link veya resim etiketi (<img src="/Sil/3">) ile
    // istemeden silme yaptırılabilir. POST + antiforgery token bunu engeller.
    [HttpPost, ActionName("Sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SilOnayla(int id)
    {
        var sonuc = await _hizmetler.SilAsync(id);
        if (sonuc.Bulunamadi)
            return NotFound();

        if (sonuc.Basarili)
            TempData["Mesaj"] = "Hizmet silindi.";
        else
            TempData["Hata"] = sonuc.Hata;
        return RedirectToAction(nameof(Index));
    }

    // ViewModel → Entity. Entity'yi biz oluşturuyoruz (over-posting yok).
    private static Hizmet VmdenEntity(HizmetFormVM vm) => new()
    {
        Ad = vm.Ad.Trim(), Aciklama = vm.Aciklama, SureDakika = vm.SureDakika, Ucret = vm.Ucret, Aktif = vm.Aktif
    };
}
