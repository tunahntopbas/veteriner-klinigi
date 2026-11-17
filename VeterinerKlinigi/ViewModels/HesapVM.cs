using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.ViewModels;

/// <summary>
/// Kayıt formunun alabileceği alanlar.
///
/// ÖDEV Beklenen-Kayit — Üye olma sayfası.
/// ÖDEV Beklenen-Validation — Buradaki attribute'ler İKİ yerde çalışır:
///   - Sunucu: model binding sonrası ModelState.IsValid
///   - İstemci: jquery.validate, Tag Helper'ların ürettiği data-val-* attribute'lerini okur
///     (sayfada _ValidationScriptsPartial yüklü olmalı)
///
/// Entity (Kullanici) yerine ViewModel: kullanıcı "rol=Admin" gibi formda olmayan
/// bir alan gönderse bile bağlanacak özellik yok (over-posting koruması).
/// </summary>
public class KayitVM
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100, ErrorMessage = "En fazla 100 karakter.")]
    [Display(Name = "Ad Soyad")]
    public string AdSoyad { get; set; } = "";

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Eposta { get; set; } = "";

    [Phone(ErrorMessage = "Geçerli bir telefon girin.")]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    [Required(ErrorMessage = "Parola zorunludur.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Parola en az 6 karakter olmalı.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Sifre { get; set; } = "";

    // [Compare] → iki alan aynı değilse hata. İstemci tarafında da çalışır.
    [Required(ErrorMessage = "Parola tekrarı zorunludur.")]
    [Compare(nameof(Sifre), ErrorMessage = "Parolalar eşleşmiyor.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola (tekrar)")]
    public string SifreTekrar { get; set; } = "";
}

/// <summary>Giriş formunun alanları.</summary>
public class GirisVM
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Eposta { get; set; } = "";

    [Required(ErrorMessage = "Parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Sifre { get; set; } = "";

    [Display(Name = "Beni hatırla")]
    public bool BeniHatirla { get; set; }

    // Giriş sonrası dönülecek adres (korumalı bir sayfaya girmeye çalışınca gelir).
    public string? DonusAdresi { get; set; }
}
