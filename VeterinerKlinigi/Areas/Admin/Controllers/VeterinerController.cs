using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Areas.Admin.Controllers;

/// <summary>
/// ÖDEV 2.1 — Hekim tanımlama (giriş hesabıyla birlikte).
/// ÖDEV 2.2 — Uzmanlık alanları ve verebildiği hizmetler (çoklu seçim).
/// ÖDEV 2.3 — Müsaitlik aralıkları (Detay sayfasından ekle/sil).
/// ÖDEV Beklenen-CRUD — Veteriner: Listele · Detay · Ekle · Duzenle · Sil.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class VeterinerController : Controller
{
    private readonly IVeterinerServisi _veterinerler;
    private readonly IUzmanlikServisi _uzmanliklar;
    private readonly IHizmetServisi _hizmetler;

    public VeterinerController(IVeterinerServisi veterinerler, IUzmanlikServisi uzmanliklar, IHizmetServisi hizmetler)
    {
        _veterinerler = veterinerler;
        _uzmanliklar = uzmanliklar;
        _hizmetler = hizmetler;
    }

    // GET /Admin/Veteriner
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Veteriner Hekimler";
        return View(await _veterinerler.ListeleAsync(sadeceAktifler: false));
    }

    // GET /Admin/Veteriner/Detay/1 → bilgiler + müsaitlik tablosu + müsaitlik ekleme formu
    public async Task<IActionResult> Detay(int id)
    {
        var hekim = await _veterinerler.GetirAsync(id);
        if (hekim is null)
            return NotFound();

        ViewData["Title"] = hekim.AdSoyad;
        ViewData["MusaitlikFormu"] = new MusaitlikEkleVM { VeterinerId = id };
        return View(hekim);
    }

    // GET /Admin/Veteriner/Ekle
    [HttpGet]
    public async Task<IActionResult> Ekle()
    {
        ViewData["Title"] = "Yeni Hekim";
        var vm = new VeterinerFormVM();
        await SecenekleriDoldurAsync(vm);
        return View("Form", vm);
    }

    // POST /Admin/Veteriner/Ekle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(VeterinerFormVM girdi)
    {
        ViewData["Title"] = "Yeni Hekim";

        // Eklemede e-posta ve parola ZORUNLU (düzenlemede yok) → koşullu doğrulama burada.
        if (string.IsNullOrWhiteSpace(girdi.Eposta))
            ModelState.AddModelError(nameof(girdi.Eposta), "Giriş e-postası zorunludur.");
        if (string.IsNullOrWhiteSpace(girdi.Sifre))
            ModelState.AddModelError(nameof(girdi.Sifre), "Başlangıç parolası zorunludur.");

        if (!ModelState.IsValid)
        {
            await SecenekleriDoldurAsync(girdi);
            return View("Form", girdi);
        }

        var sonuc = await _veterinerler.EkleAsync(VmdenEntity(girdi), girdi.Eposta!.Trim(), girdi.Sifre!,
            girdi.UzmanlikIdleri, girdi.HizmetIdleri);
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            await SecenekleriDoldurAsync(girdi);
            return View("Form", girdi);
        }

        TempData["Mesaj"] = $"{girdi.AdSoyad} eklendi. Şimdi müsaitlik saatlerini tanımlayın.";
        return RedirectToAction(nameof(Detay), new { id = sonuc.Veri!.Id });
    }

    // GET /Admin/Veteriner/Duzenle/1
    [HttpGet]
    public async Task<IActionResult> Duzenle(int id)
    {
        var v = await _veterinerler.GetirAsync(id);
        if (v is null)
            return NotFound();

        ViewData["Title"] = "Hekimi Düzenle";
        var vm = new VeterinerFormVM
        {
            Id = v.Id, AdSoyad = v.AdSoyad, Unvan = v.Unvan, Telefon = v.Telefon, Aktif = v.Aktif,
            UzmanlikIdleri = v.Uzmanliklar.Select(u => u.Id).ToArray(),
            HizmetIdleri = v.Hizmetler.Select(h => h.Id).ToArray()
        };
        await SecenekleriDoldurAsync(vm);
        return View("Form", vm);
    }

    // POST /Admin/Veteriner/Duzenle/1
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, VeterinerFormVM girdi)
    {
        ViewData["Title"] = "Hekimi Düzenle";
        // Düzenlemede e-posta/parola formda yok; onların doğrulama hatalarını yok say.
        ModelState.Remove(nameof(girdi.Eposta));
        ModelState.Remove(nameof(girdi.Sifre));

        if (!ModelState.IsValid)
        {
            await SecenekleriDoldurAsync(girdi);
            return View("Form", girdi);
        }

        var sonuc = await _veterinerler.GuncelleAsync(id, VmdenEntity(girdi), girdi.UzmanlikIdleri, girdi.HizmetIdleri);
        if (sonuc.Bulunamadi)
            return NotFound();

        TempData["Mesaj"] = "Hekim bilgileri güncellendi.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    // GET /Admin/Veteriner/Sil/1 → onay sayfası
    [HttpGet]
    public async Task<IActionResult> Sil(int id)
    {
        var hekim = await _veterinerler.GetirAsync(id);
        if (hekim is null)
            return NotFound();
        ViewData["Title"] = "Hekimi Sil";
        return View(hekim);
    }

    // POST /Admin/Veteriner/Sil/1
    [HttpPost, ActionName("Sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SilOnayla(int id)
    {
        var sonuc = await _veterinerler.SilAsync(id);
        if (sonuc.Bulunamadi)
            return NotFound();
        if (sonuc.Basarili)
        {
            TempData["Mesaj"] = "Hekim silindi.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Hata"] = sonuc.Hata;
        return RedirectToAction(nameof(Detay), new { id });
    }

    // ---------------------------------------------------------------
    // ÖDEV 2.3 — Müsaitlik aralıkları
    // ---------------------------------------------------------------

    // POST /Admin/Veteriner/MusaitlikEkle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MusaitlikEkle(MusaitlikEkleVM girdi)
    {
        if (!ModelState.IsValid)
        {
            TempData["Hata"] = "Müsaitlik bilgileri geçersiz.";
            return RedirectToAction(nameof(Detay), new { id = girdi.VeterinerId });
        }

        var sonuc = await _veterinerler.MusaitlikEkleAsync(girdi.VeterinerId, girdi.Gun, girdi.BaslangicSaati, girdi.BitisSaati);
        if (sonuc.Bulunamadi)
            return NotFound();

        if (sonuc.Basarili)
            TempData["Mesaj"] = $"{GunAdlari.Ad(girdi.Gun)} {girdi.BaslangicSaati:HH\\:mm}–{girdi.BitisSaati:HH\\:mm} eklendi.";
        else
            TempData["Hata"] = sonuc.Hata;
        return RedirectToAction(nameof(Detay), new { id = girdi.VeterinerId });
    }

    // POST /Admin/Veteriner/MusaitlikSil?veterinerId=1&musaitlikId=4
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MusaitlikSil(int veterinerId, int musaitlikId)
    {
        var sonuc = await _veterinerler.MusaitlikSilAsync(veterinerId, musaitlikId);
        if (sonuc.Bulunamadi)
            return NotFound();
        TempData["Mesaj"] = "Müsaitlik aralığı silindi.";
        return RedirectToAction(nameof(Detay), new { id = veterinerId });
    }

    // ---------------------------------------------------------------

    // Checkbox listeleri için seçenekler (formdan geri gelmez, her seferinde yeniden doldurulur).
    private async Task SecenekleriDoldurAsync(VeterinerFormVM vm)
    {
        vm.UzmanlikSecenekleri = (await _uzmanliklar.ListeleAsync())
            .Select(u => new SelectListItem(u.Ad, u.Id.ToString(), vm.UzmanlikIdleri.Contains(u.Id))).ToList();
        vm.HizmetSecenekleri = (await _hizmetler.ListeleAsync(sadeceAktifler: false))
            .Select(h => new SelectListItem($"{h.Ad} ({h.SureDakika} dk)", h.Id.ToString(), vm.HizmetIdleri.Contains(h.Id))).ToList();
    }

    private static Veteriner VmdenEntity(VeterinerFormVM vm) => new()
    {
        AdSoyad = vm.AdSoyad.Trim(), Unvan = vm.Unvan, Telefon = vm.Telefon, Aktif = vm.Aktif
    };
}
