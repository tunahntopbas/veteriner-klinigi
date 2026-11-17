using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VeterinerKlinigi.ViewModels;

// =====================================================================
// Admin paneli formlarının ViewModel'leri.
// ÖDEV Beklenen-Validation — Attribute'ler hem sunucuda (ModelState) hem
// istemcide (jquery.validate) çalışır.
// =====================================================================

/// <summary>ÖDEV 1.2 — Hizmet ekleme/düzenleme formu.</summary>
public class HizmetFormVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Hizmet adı zorunludur.")]
    [StringLength(100, ErrorMessage = "En fazla 100 karakter.")]
    [Display(Name = "Hizmet adı")]
    public string Ad { get; set; } = "";

    [StringLength(500)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }

    [Range(5, 480, ErrorMessage = "Süre 5 ile 480 dakika arasında olmalı.")]
    [Display(Name = "Süre (dakika)")]
    public int SureDakika { get; set; } = 30;

    [Range(0, 1_000_000, ErrorMessage = "Ücret 0 ile 1.000.000 TL arasında olmalı.")]
    [Display(Name = "Ücret (TL)")]
    public decimal Ucret { get; set; }

    [Display(Name = "Aktif (randevuda seçilebilir)")]
    public bool Aktif { get; set; } = true;
}

/// <summary>ÖDEV 2.2 — Uzmanlık ekleme/düzenleme formu.</summary>
public class UzmanlikFormVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Uzmanlık adı zorunludur.")]
    [StringLength(80, ErrorMessage = "En fazla 80 karakter.")]
    [Display(Name = "Uzmanlık alanı")]
    public string Ad { get; set; } = "";
}

/// <summary>
/// ÖDEV 2.1 / 2.2 — Hekim ekleme/düzenleme formu.
/// Eposta ve Sifre sadece EKLEMEDE kullanılır (hekimin giriş hesabı oluşturulur).
/// </summary>
public class VeterinerFormVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Ad Soyad")]
    public string AdSoyad { get; set; } = "";

    [StringLength(50)]
    [Display(Name = "Unvan")]
    public string? Unvan { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon girin.")]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    [Display(Name = "Aktif")]
    public bool Aktif { get; set; } = true;

    // Sadece eklemede doldurulur; düzenlemede boş gelir (controller'da kontrol edilir).
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "Giriş e-postası")]
    public string? Eposta { get; set; }

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Parola en az 6 karakter olmalı.")]
    [Display(Name = "Başlangıç parolası")]
    public string? Sifre { get; set; }

    // Çoklu seçim kutuları (checkbox) → seçilen id'ler.
    [Display(Name = "Uzmanlık alanları")]
    public int[] UzmanlikIdleri { get; set; } = [];

    [Display(Name = "Verebildiği hizmetler")]
    public int[] HizmetIdleri { get; set; } = [];

    // Formda gösterilecek seçenekler (controller doldurur; formdan geri GELMEZ).
    public List<SelectListItem> UzmanlikSecenekleri { get; set; } = [];
    public List<SelectListItem> HizmetSecenekleri { get; set; } = [];
}

/// <summary>ÖDEV 2.3 — Hekime müsaitlik aralığı ekleme formu.</summary>
public class MusaitlikEkleVM
{
    public int VeterinerId { get; set; }

    [Display(Name = "Gün")]
    public DayOfWeek Gun { get; set; } = DayOfWeek.Monday;

    [Required(ErrorMessage = "Başlangıç saati zorunludur.")]
    [DataType(DataType.Time)]
    [Display(Name = "Başlangıç")]
    public TimeOnly BaslangicSaati { get; set; } = new(9, 0);

    [Required(ErrorMessage = "Bitiş saati zorunludur.")]
    [DataType(DataType.Time)]
    [Display(Name = "Bitiş")]
    public TimeOnly BitisSaati { get; set; } = new(17, 0);
}

/// <summary>ÖDEV 1.1 / 1.2 — Klinik bilgileri ve haftalık çalışma saatleri formu.</summary>
public class KlinikFormVM
{
    [Required(ErrorMessage = "Klinik adı zorunludur.")]
    [StringLength(120)]
    [Display(Name = "Klinik adı")]
    public string Ad { get; set; } = "";

    [StringLength(300)]
    [Display(Name = "Adres")]
    public string? Adres { get; set; }

    [StringLength(30)]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    // Haftanın 7 günü için birer satır. Formda Saatler[0].AcilisSaati gibi isimlerle bağlanır.
    public List<CalismaSaatiSatirVM> Saatler { get; set; } = [];
}

public class CalismaSaatiSatirVM
{
    public DayOfWeek Gun { get; set; }

    [DataType(DataType.Time)]
    public TimeOnly AcilisSaati { get; set; }

    [DataType(DataType.Time)]
    public TimeOnly KapanisSaati { get; set; }

    public bool Kapali { get; set; }
}

/// <summary>ÖDEV 4.4 — Randevu reddederken neden yazma formu.</summary>
public class RandevuRedVM
{
    public int Id { get; set; }

    [StringLength(300, ErrorMessage = "En fazla 300 karakter.")]
    [Display(Name = "Red nedeni")]
    public string? Neden { get; set; }
}
