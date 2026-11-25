using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Controllers;

/// <summary>
/// Üyenin (hayvan sahibinin) randevuları.
///
/// ÖDEV 4.1 — Hayvan, hizmet ve uygun hekim seçerek randevu alma (Olustur).
/// ÖDEV 4.2 — Uygun değilse sistem uyarır: servis hatası ModelState'e eklenip formda gösterilir.
/// ÖDEV 4.3 — Detay sayfası saklanan tüm bilgileri gösterir.
/// ÖDEV Beklenen-CRUD — Oluştur · Listele · Detay · Düzenle (Bekliyor iken) · İptal.
/// ÖDEV Beklenen-Authorization — /Randevu/Detay/5 başkasınınsa 404.
/// ÖDEV 6.3 — REST API (POST /api/randevular) ve MCP (randevu_olustur) ile AYNI servis metodu.
/// </summary>
[Authorize(Roles = Roller.Uye)]
public class RandevuController : Controller
{
    private readonly IRandevuServisi _randevular;
    private readonly IEvcilHayvanServisi _hayvanlar;
    private readonly IHizmetServisi _hizmetler;

    public RandevuController(IRandevuServisi randevular, IEvcilHayvanServisi hayvanlar, IHizmetServisi hizmetler)
    {
        _randevular = randevular;
        _hayvanlar = hayvanlar;
        _hizmetler = hizmetler;
    }

    // GET /Randevu
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Randevularım";
        return View(await _randevular.ListeleAsync(User.Bilgi()));
    }

    // GET /Randevu/Detay/5
    public async Task<IActionResult> Detay(int id)
    {
        var randevu = await _randevular.GetirAsync(id, User.Bilgi());
        if (randevu is null)
            return NotFound();
        ViewData["Title"] = $"Randevu #{id}";
        return View(randevu);
    }

    // GET /Randevu/Olustur?hayvanId=1
    [HttpGet]
    // Ana sayfadaki hızlı randevu kartından gelinirse hizmet, gün, hekim ve saat önceden seçili gelir:
    // /Randevu/Olustur?hizmetId=1&tarih=2026-12-07&veterinerId=1&saat=10:00 (son karar yine POST'ta servisin)
    public async Task<IActionResult> Olustur(int? hayvanId, int? hizmetId, DateOnly? tarih, int? veterinerId, TimeOnly? saat)
    {
        ViewData["Title"] = "Randevu Al";
        var vm = new RandevuFormVM { EvcilHayvanId = hayvanId ?? 0, HizmetId = hizmetId ?? 0, VeterinerId = veterinerId ?? 0, Saat = saat };
        if (tarih is not null) vm.Tarih = tarih.Value;
        await SecenekleriDoldurAsync(vm);

        if (vm.HayvanSecenekleri.Count == 0)
        {
            TempData["Hata"] = "Randevu almak için önce bir evcil hayvan ekleyin.";
            return RedirectToAction("Ekle", "EvcilHayvan");
        }
        return View("Form", vm);
    }

    // POST /Randevu/Olustur
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Olustur(RandevuFormVM girdi)
    {
        ViewData["Title"] = "Randevu Al";
        if (!ModelState.IsValid)
        {
            await SecenekleriDoldurAsync(girdi);
            return View("Form", girdi);
        }

        // DİKKAT: Sayfadaki boş saat listesi sadece KOLAYLIK. Kullanıcı gizli alanları elle
        // değiştirebilir; asıl kontrol (müsaitlik + çakışma) servis içinde YENİDEN yapılır.
        var sonuc = await _randevular.OlusturAsync(TalepOlustur(girdi), User.Bilgi());
        if (!sonuc.Basarili)
        {
            // ÖDEV 4.2 — "Sistem uyarmalı": nedeni formun üstünde gösterilir.
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            await SecenekleriDoldurAsync(girdi);
            return View("Form", girdi);
        }

        TempData["Mesaj"] = "Randevu talebiniz alındı. Durum: Bekliyor — klinik onaylayınca kesinleşecek.";
        return RedirectToAction(nameof(Detay), new { id = sonuc.Veri!.Id });
    }

    // GET /Randevu/Duzenle/5 — sadece Bekliyor durumunda
    [HttpGet]
    public async Task<IActionResult> Duzenle(int id)
    {
        var r = await _randevular.GetirAsync(id, User.Bilgi());
        if (r is null)
            return NotFound();
        if (r.Durum != RandevuDurumu.Bekliyor)
        {
            TempData["Hata"] = "Sadece onay bekleyen randevular değiştirilebilir.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        ViewData["Title"] = $"Randevu #{id} — Değiştir";
        var vm = new RandevuFormVM
        {
            Id = r.Id, EvcilHayvanId = r.EvcilHayvanId, HizmetId = r.HizmetId, VeterinerId = r.VeterinerId,
            Tarih = DateOnly.FromDateTime(r.BaslangicZamani), Saat = TimeOnly.FromDateTime(r.BaslangicZamani),
            SahipNotu = r.SahipNotu
        };
        await SecenekleriDoldurAsync(vm);
        return View("Form", vm);
    }

    // POST /Randevu/Duzenle/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, RandevuFormVM girdi)
    {
        ViewData["Title"] = $"Randevu #{id} — Değiştir";
        if (!ModelState.IsValid)
        {
            await SecenekleriDoldurAsync(girdi);
            return View("Form", girdi);
        }

        var sonuc = await _randevular.GuncelleAsync(id, TalepOlustur(girdi), User.Bilgi());
        if (sonuc.Bulunamadi)
            return NotFound();
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            await SecenekleriDoldurAsync(girdi);
            return View("Form", girdi);
        }

        TempData["Mesaj"] = "Randevu güncellendi.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    // POST /Randevu/Iptal/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iptal(int id)
    {
        var sonuc = await _randevular.IptalEtAsync(id, User.Bilgi());
        if (sonuc.Bulunamadi)
            return NotFound();
        if (sonuc.Basarili)
            TempData["Mesaj"] = "Randevu iptal edildi.";
        else
            TempData["Hata"] = sonuc.Hata;
        return RedirectToAction(nameof(Detay), new { id });
    }

    // ---------------------------------------------------------------

    private static RandevuTalebi TalepOlustur(RandevuFormVM vm) =>
        new(vm.EvcilHayvanId, vm.HizmetId, vm.VeterinerId, vm.Tarih.ToDateTime(vm.Saat!.Value), vm.SahipNotu);

    // Açılır listeler: kullanıcının KENDİ hayvanları ve aktif hizmetler.
    private async Task SecenekleriDoldurAsync(RandevuFormVM vm)
    {
        vm.HayvanSecenekleri = (await _hayvanlar.ListeleAsync(User.Bilgi()))
            .Select(h => new SelectListItem($"{h.Ad} ({h.Tur})", h.Id.ToString(), h.Id == vm.EvcilHayvanId)).ToList();
        vm.HizmetSecenekleri = (await _hizmetler.ListeleAsync(sadeceAktifler: true))
            .Select(h => new SelectListItem($"{h.Ad} — {h.SureDakika} dk — {h.Ucret:N2} TL", h.Id.ToString(), h.Id == vm.HizmetId)).ToList();
    }
}
