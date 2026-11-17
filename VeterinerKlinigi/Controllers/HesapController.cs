using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.ViewModels;

namespace VeterinerKlinigi.Controllers;

/// <summary>
/// Kayıt, giriş ve çıkış işlemleri.
///
/// NEDEN Identity'nin hazır sayfaları (Razor Pages UI) değil de kendi controller'ımız?
///   Projemiz MVC; hazır UI Razor Pages ile gelir ve İngilizce/karışık bir yapı getirir.
///   Kendi controller'ımızda her adım açık: UserManager kullanıcı oluşturur,
///   SignInManager parolayı kontrol edip giriş çerezini (cookie) yazar.
///
/// ÖDEV Beklenen-Kayit — Üye olma sayfası (Kayit action'ları).
/// </summary>
public class HesapController : Controller
{
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;
    private readonly SignInManager<Kullanici> _girisYoneticisi;

    // Constructor injection: ikisini de Program.cs'teki AddIdentity kaydeder.
    public HesapController(UserManager<Kullanici> kullaniciYoneticisi, SignInManager<Kullanici> girisYoneticisi)
    {
        _kullaniciYoneticisi = kullaniciYoneticisi;
        _girisYoneticisi = girisYoneticisi;
    }

    // ---------------------------------------------------------------
    // KAYIT
    // ---------------------------------------------------------------

    // GET /Hesap/Kayit → boş kayıt formu
    [HttpGet]
    public IActionResult Kayit()
    {
        ViewData["Title"] = "Üye Ol";
        return View(new KayitVM());
    }

    // POST /Hesap/Kayit
    // ÖDEV Beklenen-Kayit — Yeni kullanıcı HER ZAMAN "Uye" rolüyle oluşur.
    // Rol formdan gelmez; kimse kendini Admin yapamaz.
    [HttpPost]
    [ValidateAntiForgeryToken] // CSRF koruması: formdaki gizli token doğrulanır
    public async Task<IActionResult> Kayit(KayitVM girdi)
    {
        ViewData["Title"] = "Üye Ol";

        // ÖDEV Beklenen-Validation — sunucu tarafı doğrulama
        if (!ModelState.IsValid)
            return View(girdi);

        // Entity'yi biz oluşturuyoruz (over-posting yok).
        var kullanici = new Kullanici
        {
            UserName = girdi.Eposta,
            Email = girdi.Eposta,
            AdSoyad = girdi.AdSoyad,
            PhoneNumber = girdi.Telefon
        };

        // CreateAsync parolayı kurallara göre kontrol eder ve HASH'leyerek kaydeder.
        var sonuc = await _kullaniciYoneticisi.CreateAsync(kullanici, girdi.Sifre);
        if (!sonuc.Succeeded)
        {
            // Identity'nin hatalarını (parola zayıf, e-posta kullanılıyor...) forma taşı.
            foreach (var hata in sonuc.Errors)
                ModelState.AddModelError(string.Empty, IdentityHatasiniCevir(hata));
            return View(girdi);
        }

        await _kullaniciYoneticisi.AddToRoleAsync(kullanici, Roller.Uye);
        await _girisYoneticisi.SignInAsync(kullanici, isPersistent: false);

        // PRG: F5 ile ikinci kayıt denemesi olmasın.
        TempData["Mesaj"] = $"Hoş geldiniz, {kullanici.AdSoyad}! Şimdi evcil hayvanınızı ekleyebilirsiniz.";
        return RedirectToAction("Index", "EvcilHayvan");
    }

    // ---------------------------------------------------------------
    // GİRİŞ
    // ---------------------------------------------------------------

    // GET /Hesap/Giris?DonusAdresi=/Randevu
    [HttpGet]
    public IActionResult Giris(string? donusAdresi)
    {
        ViewData["Title"] = "Giriş Yap";
        return View(new GirisVM { DonusAdresi = donusAdresi });
    }

    // POST /Hesap/Giris
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(GirisVM girdi)
    {
        ViewData["Title"] = "Giriş Yap";
        if (!ModelState.IsValid)
            return View(girdi);

        // Parola hash'ini kontrol eder; doğruysa kimlik çerezini yazar.
        // lockoutOnFailure: true → art arda yanlış denemede hesap geçici kilitlenir.
        var sonuc = await _girisYoneticisi.PasswordSignInAsync(
            girdi.Eposta, girdi.Sifre, girdi.BeniHatirla, lockoutOnFailure: true);

        if (sonuc.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Çok fazla hatalı deneme. Lütfen birkaç dakika sonra tekrar deneyin.");
            return View(girdi);
        }
        if (!sonuc.Succeeded)
        {
            // DİKKAT: "e-posta yok" ile "parola yanlış" ayrımı yapılmaz; saldırgana bilgi verilmez.
            ModelState.AddModelError(string.Empty, "E-posta veya parola hatalı.");
            return View(girdi);
        }

        // DİKKAT: Sadece kendi sitemizin içindeki adreslere dön (open redirect koruması).
        if (!string.IsNullOrEmpty(girdi.DonusAdresi) && Url.IsLocalUrl(girdi.DonusAdresi))
            return LocalRedirect(girdi.DonusAdresi);

        return await RoleGoreYonlendirAsync(girdi.Eposta);
    }

    // POST /Hesap/Cikis — çıkış POST ile yapılır (bir resim/link ile kullanıcıyı çıkış yaptırmasınlar).
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Cikis()
    {
        await _girisYoneticisi.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // GET /Hesap/ErisimEngellendi — giriş yapmış ama rolü yetmeyen kullanıcı buraya düşer (403).
    public IActionResult ErisimEngellendi()
    {
        ViewData["Title"] = "Erişim Engellendi";
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    // ---------------------------------------------------------------
    // Yardımcılar
    // ---------------------------------------------------------------

    // Girişten sonra herkesi kendi ana sayfasına gönder.
    private async Task<IActionResult> RoleGoreYonlendirAsync(string eposta)
    {
        var kullanici = await _kullaniciYoneticisi.FindByEmailAsync(eposta);
        if (kullanici is null)
            return RedirectToAction("Index", "Home");

        if (await _kullaniciYoneticisi.IsInRoleAsync(kullanici, Roller.Admin))
            return RedirectToAction("Index", "Panel", new { area = "Admin" });
        if (await _kullaniciYoneticisi.IsInRoleAsync(kullanici, Roller.Veteriner))
            return RedirectToAction("Index", "Randevu", new { area = "Hekim" });

        return RedirectToAction("Index", "EvcilHayvan");
    }

    // Identity hata kodlarını Türkçe mesaja çevirir.
    private static string IdentityHatasiniCevir(IdentityError hata) => hata.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Bu e-posta adresiyle zaten bir hesap var.",
        "PasswordTooShort" => "Parola en az 6 karakter olmalı.",
        "PasswordRequiresDigit" => "Parola en az bir rakam içermeli.",
        "PasswordRequiresUpper" => "Parola en az bir büyük harf içermeli.",
        "PasswordRequiresLower" => "Parola en az bir küçük harf içermeli.",
        "PasswordRequiresNonAlphanumeric" => "Parola en az bir sembol (! . * gibi) içermeli.",
        "InvalidEmail" => "Geçersiz e-posta adresi.",
        _ => hata.Description
    };
}
