using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;

namespace VeterinerKlinigi.Areas.Admin.Controllers;

/// <summary>
/// Rapor sayfası.
///
/// DİKKAT: Bu action veritabanına HİÇ gitmez, sadece boş sayfayı döner.
/// Veri, sayfa açıldıktan sonra tarayıcıdaki jQuery tarafından REST API'den çekilir:
///   GET /api/raporlar/hizmet-ozeti?baslangic=...&bitis=...
/// ÖDEV 5.1 / 5.2 — REST API'nin site içinden TÜKETİLDİĞİ yerlerden biri.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class RaporController : Controller
{
    // GET /Admin/Rapor
    public IActionResult Index()
    {
        ViewData["Title"] = "Hizmet Raporu";
        return View();
    }
}
