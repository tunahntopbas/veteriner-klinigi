using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Areas.Admin.Controllers;

/// <summary>
/// ÖDEV Beklenen-AdminPaneli — Admin panelinin ana sayfası (özet sayılar + kısayollar).
///
/// [Area("Admin")] → adresler /Admin/... ile başlar ve görünümler Areas/Admin/Views altında aranır.
/// [Authorize(Roles = Admin)] → Admin olmayan biri girerse 403 (ErisimEngellendi sayfası).
/// Bu attribute sınıfın üstünde olduğu için İÇİNDEKİ TÜM action'lar korunur.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class PanelController : Controller
{
    private readonly UygulamaDbContext _db;

    public PanelController(UygulamaDbContext db)
    {
        _db = db;
    }

    // GET /Admin  veya  /Admin/Panel
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Admin Paneli";

        // Özet sayılar — veri controller'da hazırlanır, görünüm sadece gösterir.
        ViewData["HizmetSayisi"] = await _db.Hizmetler.CountAsync(h => h.Aktif);
        ViewData["HekimSayisi"] = await _db.Veterinerler.CountAsync(v => v.Aktif);
        ViewData["HayvanSayisi"] = await _db.EvcilHayvanlar.CountAsync();
        ViewData["BekleyenSayisi"] = await _db.Randevular.CountAsync(r => r.Durum == RandevuDurumu.Bekliyor);
        var bugun = DateTime.Today;
        ViewData["BugunSayisi"] = await _db.Randevular.CountAsync(r =>
            r.BaslangicZamani >= bugun && r.BaslangicZamani < bugun.AddDays(1) && r.Durum == RandevuDurumu.Onaylandi);

        return View();
    }
}
