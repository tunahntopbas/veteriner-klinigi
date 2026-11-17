using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Controllers;

/// <summary>
/// Evcil hayvanlar.
/// ÖDEV 3.1 / 3.2 — Sahip birden fazla hayvan kaydeder (tür, ırk, doğum tarihi, kilo, fotoğraf).
/// ÖDEV Beklenen-CRUD — Listele · Detay · Ekle · Duzenle · Sil.
/// ÖDEV Beklenen-Authorization — /EvcilHayvan/Detay/6 gibi başkasının hayvanının id'si
///   yazılırsa servis null döner → 404. (Kontrol: Services/YetkiSorgulari.cs)
///
/// [Authorize] → giriş yapmamış kullanıcı giriş sayfasına yönlenir.
/// Ekleme sadece Üye'ye açık; görüntüleme Üye (kendi), Veteriner (hastası), Admin (hepsi).
/// </summary>
[Authorize]
public class EvcilHayvanController : Controller
{
    private readonly IEvcilHayvanServisi _hayvanlar;
    private readonly IFotografServisi _fotograflar;

    public EvcilHayvanController(IEvcilHayvanServisi hayvanlar, IFotografServisi fotograflar)
    {
        _hayvanlar = hayvanlar;
        _fotograflar = fotograflar;
    }

    // GET /EvcilHayvan
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = User.IsInRole(Roller.Uye) ? "Hayvanlarım" : "Evcil Hayvanlar";
        return View(await _hayvanlar.ListeleAsync(User.Bilgi()));
    }

    // GET /EvcilHayvan/Detay/1 — bilgiler, aşılar (API'den jQuery ile), randevu geçmişi
    public async Task<IActionResult> Detay(int id)
    {
        var hayvan = await _hayvanlar.GetirAsync(id, User.Bilgi());
        if (hayvan is null)
            return NotFound();          // yok VEYA başkasının → ikisi de 404
        ViewData["Title"] = hayvan.Ad;
        return View(hayvan);
    }

    // GET /EvcilHayvan/Fotograf/1
    // FOTOĞRAF NASIL GÖSTERİLİYOR? <img src="/EvcilHayvan/Fotograf/1"> → bu action.
    // Önce yetki (servis sorgusu), sonra diskteki dosya PhysicalFile ile akıtılır.
    public async Task<IActionResult> Fotograf(int id)
    {
        var hayvan = await _hayvanlar.GetirAsync(id, User.Bilgi());
        var yol = _fotograflar.TamYol(hayvan?.FotografDosyaAdi);
        if (hayvan is null || yol is null || !System.IO.File.Exists(yol))
            return NotFound();
        return PhysicalFile(yol, _fotograflar.IcerikTuru(hayvan.FotografDosyaAdi!));
    }

    // GET /EvcilHayvan/Ekle
    [HttpGet]
    [Authorize(Roles = Roller.Uye)]
    public IActionResult Ekle()
    {
        ViewData["Title"] = "Yeni Hayvan";
        return View("Form", new EvcilHayvanVM());
    }

    // POST /EvcilHayvan/Ekle (multipart/form-data: fotoğraf dosyası da gelir)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roller.Uye)]
    [RequestSizeLimit(3 * 1024 * 1024)]   // istek gövdesi en fazla 3 MB (fotoğraf sınırı 2 MB + form)
    public async Task<IActionResult> Ekle(EvcilHayvanVM girdi)
    {
        ViewData["Title"] = "Yeni Hayvan";
        if (!ModelState.IsValid)
            return View("Form", girdi);

        var sonuc = await _hayvanlar.EkleAsync(VmdenEntity(girdi), girdi.Fotograf, User.Bilgi());
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View("Form", girdi);
        }

        TempData["Mesaj"] = $"{girdi.Ad} kaydedildi.";
        return RedirectToAction(nameof(Detay), new { id = sonuc.Veri!.Id });
    }

    // GET /EvcilHayvan/Duzenle/1
    [HttpGet]
    public async Task<IActionResult> Duzenle(int id)
    {
        var h = await _hayvanlar.GetirAsync(id, User.Bilgi());
        if (h is null || !DuzenleyebilirMi(h))
            return NotFound();

        ViewData["Title"] = $"{h.Ad} — Düzenle";
        return View("Form", new EvcilHayvanVM
        {
            Id = h.Id, Ad = h.Ad, Tur = h.Tur, Irk = h.Irk, DogumTarihi = h.DogumTarihi, Kilo = h.Kilo,
            FotografVar = h.FotografDosyaAdi is not null
        });
    }

    // POST /EvcilHayvan/Duzenle/1
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Duzenle(int id, EvcilHayvanVM girdi)
    {
        ViewData["Title"] = "Düzenle";
        if (!ModelState.IsValid)
            return View("Form", girdi);

        var sonuc = await _hayvanlar.GuncelleAsync(id, VmdenEntity(girdi), girdi.Fotograf, girdi.FotografiKaldir, User.Bilgi());
        if (sonuc.Bulunamadi)
            return NotFound();
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View("Form", girdi);
        }

        TempData["Mesaj"] = "Bilgiler güncellendi.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    // GET /EvcilHayvan/Sil/1 → onay sayfası
    [HttpGet]
    public async Task<IActionResult> Sil(int id)
    {
        var h = await _hayvanlar.GetirAsync(id, User.Bilgi());
        if (h is null || !DuzenleyebilirMi(h))
            return NotFound();
        ViewData["Title"] = $"{h.Ad} — Sil";
        return View(h);
    }

    // POST /EvcilHayvan/Sil/1
    [HttpPost, ActionName("Sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SilOnayla(int id)
    {
        var sonuc = await _hayvanlar.SilAsync(id, User.Bilgi());
        if (sonuc.Bulunamadi)
            return NotFound();
        if (!sonuc.Basarili)
        {
            TempData["Hata"] = sonuc.Hata;
            return RedirectToAction(nameof(Detay), new { id });
        }
        TempData["Mesaj"] = "Hayvan kaydı silindi.";
        return RedirectToAction(nameof(Index));
    }

    // Veteriner hayvanı görebilir ama sahibin bilgilerini değiştiremez.
    private bool DuzenleyebilirMi(EvcilHayvan h) => User.IsInRole(Roller.Admin) || h.SahipId == User.Bilgi().Id;

    private static EvcilHayvan VmdenEntity(EvcilHayvanVM vm) => new()
    {
        Ad = vm.Ad.Trim(), Tur = vm.Tur, Irk = vm.Irk, DogumTarihi = vm.DogumTarihi, Kilo = vm.Kilo
    };
}
