using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Controllers;

/// <summary>
/// Profil sayfası ve MCP erişim anahtarları.
///
/// ÖDEV 6.4 — MCP ucu yetkilendirme ile korunur: kullanıcı burada kendine özel bir
/// anahtar üretir, AI istemcisine (Claude Desktop / VS Code / MCP Inspector) yapıştırır.
/// Araçlar o anahtarın sahibi adına ve SADECE onun verisiyle çalışır.
/// </summary>
[Authorize]
public class ProfilController : Controller
{
    private readonly IMcpAnahtarServisi _anahtarlar;
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;

    public ProfilController(IMcpAnahtarServisi anahtarlar, UserManager<Kullanici> kullaniciYoneticisi)
    {
        _anahtarlar = anahtarlar;
        _kullaniciYoneticisi = kullaniciYoneticisi;
    }

    // GET /Profil
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Profil";
        var kullanici = await _kullaniciYoneticisi.GetUserAsync(User);
        ViewData["AdSoyad"] = kullanici?.AdSoyad;
        ViewData["Roller"] = kullanici is null ? "" : string.Join(", ", await _kullaniciYoneticisi.GetRolesAsync(kullanici));
        ViewData["McpAdresi"] = $"{Request.Scheme}://{Request.Host}/mcp";
        return View(await _anahtarlar.ListeleAsync(User.Bilgi().Id));
    }

    // POST /Profil/AnahtarUret
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnahtarUret(AnahtarUretVM girdi)
    {
        if (!ModelState.IsValid)
        {
            TempData["Hata"] = "Anahtara bir ad verin.";
            return RedirectToAction(nameof(Index));
        }

        var (_, acikAnahtar) = await _anahtarlar.UretAsync(User.Bilgi().Id, girdi.Ad);

        // DİKKAT: Anahtar yalnızca BU yönlendirmeden sonraki sayfada bir kez görünür (TempData).
        // Veritabanında sadece hash'i var; kaybedilirse yenisi üretilir.
        TempData["YeniAnahtar"] = acikAnahtar;
        return RedirectToAction(nameof(Index));
    }

    // POST /Profil/AnahtarIptal/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnahtarIptal(int id)
    {
        var sonuc = await _anahtarlar.IptalEtAsync(id, User.Bilgi().Id);
        if (sonuc.Bulunamadi)
            return NotFound();
        TempData["Mesaj"] = "Anahtar iptal edildi; artık MCP'ye bağlanamaz.";
        return RedirectToAction(nameof(Index));
    }
}
