using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

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
    // 25.11: sayılar ve 7 günlük grafik ana sayfadaki admin özetiyle AYNI (OzetServisi + _AdminOzeti partial'ı);
    // iki yerde ayrı hesaplanıp birbirini tutmayan sayılar olmasın.
    public async Task<IActionResult> Index([FromServices] IOzetServisi ozet)
    {
        ViewData["Title"] = "Yönetim Paneli";
        ViewData["HayvanSayisi"] = await _db.EvcilHayvanlar.CountAsync();
        ViewData["HizmetSayisi"] = await _db.Hizmetler.CountAsync(h => h.Aktif);
        var model = new AnaSayfaVM
        {
            Selam = "Yönetim paneli",
            KlinikDurumu = "",
            KlinikAcik = false,
            Admin = await ozet.AdminOzetiAsync()
        };
        return View(model);
    }
}
