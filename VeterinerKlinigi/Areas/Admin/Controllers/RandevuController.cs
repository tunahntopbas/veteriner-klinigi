using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Areas.Admin.Controllers;

/// <summary>
/// Admin için TÜM randevular.
/// ÖDEV 4.4 — Admin bekleyen randevuları onaylar veya reddeder.
/// ÖDEV Beklenen-CRUD — Randevu: listele, detay, iptal, kalıcı sil (oluşturma ve düzenleme üye tarafında).
///
/// İş kuralları (hangi durumdan hangisine geçilir) RandevuServisi'nde; burada sadece çağırıyoruz.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class RandevuController : Controller
{
    private readonly IRandevuServisi _randevular;

    public RandevuController(IRandevuServisi randevular)
    {
        _randevular = randevular;
    }

    // GET /Admin/Randevu?durum=Bekliyor
    public async Task<IActionResult> Index(RandevuDurumu? durum)
    {
        ViewData["Title"] = "Tüm Randevular";
        ViewData["Durum"] = durum;
        return View(await _randevular.ListeleAsync(User.Bilgi(), durum));
    }

    // GET /Admin/Randevu/Detay/5
    public async Task<IActionResult> Detay(int id)
    {
        var randevu = await _randevular.GetirAsync(id, User.Bilgi());
        if (randevu is null)
            return NotFound();
        ViewData["Title"] = $"Randevu #{id}";
        return View(randevu);
    }

    // POST /Admin/Randevu/Onayla/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onayla(int id)
    {
        var sonuc = await _randevular.OnaylaAsync(id, User.Bilgi());
        return SonucaGoreYonlendir(sonuc, id, "Randevu onaylandı.");
    }

    // POST /Admin/Randevu/Reddet
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reddet(RandevuRedVM girdi)
    {
        var sonuc = await _randevular.ReddetAsync(girdi.Id, girdi.Neden, User.Bilgi());
        return SonucaGoreYonlendir(sonuc, girdi.Id, "Randevu reddedildi.");
    }

    // POST /Admin/Randevu/Tamamla/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tamamla(int id)
    {
        var sonuc = await _randevular.TamamlaAsync(id, User.Bilgi());
        return SonucaGoreYonlendir(sonuc, id, "Randevu tamamlandı olarak işaretlendi.");
    }

    // POST /Admin/Randevu/Iptal/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iptal(int id)
    {
        var sonuc = await _randevular.IptalEtAsync(id, User.Bilgi());
        return SonucaGoreYonlendir(sonuc, id, "Randevu iptal edildi.");
    }

    // POST /Admin/Randevu/Sil/5 — kalıcı silme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var sonuc = await _randevular.SilAsync(id, User.Bilgi());
        if (sonuc.Bulunamadi)
            return NotFound();
        TempData["Mesaj"] = $"Randevu #{id} kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult SonucaGoreYonlendir(IslemSonucu sonuc, int id, string basariMesaji)
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
