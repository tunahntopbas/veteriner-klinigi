using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Areas.Hekim.Controllers;

/// <summary>
/// Hekim paneli — veteriner sadece KENDİ randevularını görür ve yönetir.
///
/// ÖDEV Beklenen-Roller — "Veteriner: kendi randevularını onaylar/reddeder, muayene notu girer".
/// ÖDEV 4.4 — İlgili veteriner onaylar ya da reddeder.
/// ÖDEV 3.4 — Tamamlanan randevu için muayene notu.
///
/// "Kendi randevusu" kontrolü nerede? Services/YetkiSorgulari.ErisilebilirRandevular ve
/// RandevuServisi.DurumDegistirAsync: sorgu r.Veteriner.KullaniciId == giriş yapan kullanıcı.
/// Başka hekimin randevu id'si yazılırsa → 404.
/// </summary>
[Area("Hekim")]
[Authorize(Roles = Roller.Veteriner)]
public class RandevuController : Controller
{
    private readonly IRandevuServisi _randevular;

    public RandevuController(IRandevuServisi randevular)
    {
        _randevular = randevular;
    }

    // GET /Hekim/Randevu?durum=Bekliyor
    public async Task<IActionResult> Index(RandevuDurumu? durum)
    {
        ViewData["Title"] = "Hekim Paneli — Randevularım";
        ViewData["Durum"] = durum;
        var liste = await _randevular.ListeleAsync(User.Bilgi(), durum);
        return View(liste);
    }

    // GET /Hekim/Randevu/Detay/5
    public async Task<IActionResult> Detay(int id)
    {
        var randevu = await _randevular.GetirAsync(id, User.Bilgi());
        if (randevu is null)
            return NotFound();
        ViewData["Title"] = $"Randevu #{id}";
        return View(randevu);
    }

    // POST /Hekim/Randevu/Onayla/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onayla(int id) =>
        Yonlendir(await _randevular.OnaylaAsync(id, User.Bilgi()), id, "Randevu onaylandı.");

    // POST /Hekim/Randevu/Reddet
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reddet(RandevuRedVM girdi) =>
        Yonlendir(await _randevular.ReddetAsync(girdi.Id, girdi.Neden, User.Bilgi()), girdi.Id, "Randevu reddedildi.");

    // POST /Hekim/Randevu/Tamamla/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tamamla(int id) =>
        Yonlendir(await _randevular.TamamlaAsync(id, User.Bilgi()), id, "Randevu tamamlandı. Şimdi muayene notunu yazabilirsiniz.");

    // GET /Hekim/Randevu/MuayeneNotu/5
    [HttpGet]
    public async Task<IActionResult> MuayeneNotu(int id)
    {
        var randevu = await _randevular.GetirAsync(id, User.Bilgi());
        if (randevu is null)
            return NotFound();
        if (randevu.Durum != RandevuDurumu.Tamamlandi)
        {
            TempData["Hata"] = "Muayene notu sadece tamamlanan randevuya yazılabilir.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        ViewData["Title"] = "Muayene Notu";
        ViewData["Randevu"] = randevu;
        return View(new MuayeneNotuVM
        {
            RandevuId = id,
            Teshis = randevu.MuayeneNotu?.Teshis,
            Tedavi = randevu.MuayeneNotu?.Tedavi,
            Notlar = randevu.MuayeneNotu?.Notlar ?? ""
        });
    }

    // POST /Hekim/Randevu/MuayeneNotu/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MuayeneNotu(int id, MuayeneNotuVM girdi)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Muayene Notu";
            ViewData["Randevu"] = await _randevular.GetirAsync(id, User.Bilgi());
            return View(girdi);
        }

        var sonuc = await _randevular.MuayeneNotuKaydetAsync(id, girdi.Teshis, girdi.Tedavi, girdi.Notlar, User.Bilgi());
        return Yonlendir(sonuc, id, "Muayene notu kaydedildi.");
    }

    private IActionResult Yonlendir(IslemSonucu sonuc, int id, string basariMesaji)
    {
        if (sonuc.Bulunamadi)
            return NotFound();
        if (sonuc.Basarili)
            TempData["Mesaj"] = basariMesaji;
        else
            TempData["Hata"] = sonuc.Hata;
        return RedirectToAction(nameof(Detay), new { id });
    }
}
