using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Controllers;

/// <summary>
/// Aşı kayıtları.
/// ÖDEV 3.3 — Aşı adı, yapıldığı tarih, sonraki doz tarihi.
/// ÖDEV Beklenen-CRUD — Ekle · Duzenle · Sil (listeleme/detay hayvanın detay sayfasında).
/// Yetki AsiServisi'nde: hayvanın sahibi, o hayvana bakan hekim ve Admin.
/// </summary>
[Authorize]
public class AsiKaydiController : Controller
{
    private readonly IAsiServisi _asilar;
    private readonly IEvcilHayvanServisi _hayvanlar;

    public AsiKaydiController(IAsiServisi asilar, IEvcilHayvanServisi hayvanlar)
    {
        _asilar = asilar;
        _hayvanlar = hayvanlar;
    }

    // GET /AsiKaydi/Ekle?hayvanId=1
    [HttpGet]
    public async Task<IActionResult> Ekle(int hayvanId)
    {
        var hayvan = await _hayvanlar.GetirAsync(hayvanId, User.Bilgi());
        if (hayvan is null)
            return NotFound();
        ViewData["Title"] = $"{hayvan.Ad} — Aşı Ekle";
        return View("Form", new AsiKaydiVM { EvcilHayvanId = hayvan.Id, HayvanAdi = hayvan.Ad });
    }

    // POST /AsiKaydi/Ekle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(AsiKaydiVM girdi)
    {
        ViewData["Title"] = "Aşı Ekle";
        if (!ModelState.IsValid)
            return View("Form", girdi);

        var sonuc = await _asilar.EkleAsync(VmdenEntity(girdi), User.Bilgi());
        if (sonuc.Bulunamadi)
            return NotFound();      // hayvanId başkasının hayvanı → 404
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View("Form", girdi);
        }

        TempData["Mesaj"] = $"{girdi.AsiAdi} kaydedildi.";
        return RedirectToAction("Detay", "EvcilHayvan", new { id = girdi.EvcilHayvanId });
    }

    // GET /AsiKaydi/Duzenle/3
    [HttpGet]
    public async Task<IActionResult> Duzenle(int id)
    {
        var a = await _asilar.GetirAsync(id, User.Bilgi());
        if (a is null)
            return NotFound();
        ViewData["Title"] = "Aşı Kaydını Düzenle";
        return View("Form", new AsiKaydiVM
        {
            Id = a.Id, EvcilHayvanId = a.EvcilHayvanId, HayvanAdi = a.EvcilHayvan?.Ad,
            AsiAdi = a.AsiAdi, YapildigiTarih = a.YapildigiTarih, SonrakiDozTarihi = a.SonrakiDozTarihi, Notlar = a.Notlar
        });
    }

    // POST /AsiKaydi/Duzenle/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, AsiKaydiVM girdi)
    {
        ViewData["Title"] = "Aşı Kaydını Düzenle";
        if (!ModelState.IsValid)
            return View("Form", girdi);

        var sonuc = await _asilar.GuncelleAsync(id, VmdenEntity(girdi), User.Bilgi());
        if (sonuc.Bulunamadi)
            return NotFound();
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View("Form", girdi);
        }

        // Hayvan id'sini formdan değil veritabanından al (form değiştirilmiş olabilir).
        var asi = await _asilar.GetirAsync(id, User.Bilgi());
        TempData["Mesaj"] = "Aşı kaydı güncellendi.";
        return RedirectToAction("Detay", "EvcilHayvan", new { id = asi!.EvcilHayvanId });
    }

    // POST /AsiKaydi/Sil/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var asi = await _asilar.GetirAsync(id, User.Bilgi());
        if (asi is null)
            return NotFound();
        await _asilar.SilAsync(id, User.Bilgi());
        TempData["Mesaj"] = "Aşı kaydı silindi.";
        return RedirectToAction("Detay", "EvcilHayvan", new { id = asi.EvcilHayvanId });
    }

    private static AsiKaydi VmdenEntity(AsiKaydiVM vm) => new()
    {
        EvcilHayvanId = vm.EvcilHayvanId, AsiAdi = vm.AsiAdi.Trim(),
        YapildigiTarih = vm.YapildigiTarih, SonrakiDozTarihi = vm.SonrakiDozTarihi, Notlar = vm.Notlar
    };
}
