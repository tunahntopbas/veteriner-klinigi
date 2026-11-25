using VeterinerKlinigi.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VeterinerKlinigi.ViewModels;

// =====================================================================
// Üye (hayvan sahibi) ve Hekim sayfalarının ViewModel'leri.
// ÖDEV Beklenen-Validation — istemci + sunucu doğrulama attribute'leri.
// =====================================================================

/// <summary>
/// ÖDEV 3.2 — Evcil hayvan formu (tür, ırk, doğum tarihi, kilo, isteğe bağlı fotoğraf).
/// DİKKAT: SahipId burada YOK. Sahip, giriş yapan kullanıcıdır; servis kendisi atar.
/// Kullanıcı isteğe "SahipId=başkası" eklese bile bağlanacak alan olmadığı için düşer (over-posting koruması).
/// </summary>
public class EvcilHayvanVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Hayvanın adı zorunludur.")]
    [StringLength(50, ErrorMessage = "En fazla 50 karakter.")]
    [Display(Name = "Adı")]
    public string Ad { get; set; } = "";

    [Required(ErrorMessage = "Tür zorunludur.")]
    [StringLength(40)]
    [Display(Name = "Tür")]
    public string Tur { get; set; } = "";

    [StringLength(60)]
    [Display(Name = "Irk")]
    public string? Irk { get; set; }

    [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Doğum tarihi")]
    public DateOnly DogumTarihi { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));

    [Range(0.01, 200, ErrorMessage = "Kilo 0,01 ile 200 kg arasında olmalı.")]
    [Display(Name = "Kilo (kg)")]
    public decimal? Kilo { get; set; }

    // Dosya yükleme: form enctype="multipart/form-data" olmalı.
    [Display(Name = "Fotoğraf (isteğe bağlı, JPG/PNG, en fazla 2 MB)")]
    public IFormFile? Fotograf { get; set; }

    [Display(Name = "Mevcut fotoğrafı kaldır")]
    public bool FotografiKaldir { get; set; }

    // Düzenlemede mevcut fotoğrafı göstermek için (formdan güvenilir veri olarak okunmaz).
    public bool FotografVar { get; set; }

    // Tür seçenekleri (açılır liste)
    public static readonly string[] Turler = ["Kedi", "Köpek", "Kuş", "Tavşan", "Hamster", "Sürüngen", "Diğer"];
}

/// <summary>ÖDEV 3.3 — Aşı kaydı formu.</summary>
public class AsiKaydiVM
{
    public int Id { get; set; }
    public int EvcilHayvanId { get; set; }
    public string? HayvanAdi { get; set; }

    [Required(ErrorMessage = "Aşı adı zorunludur.")]
    [StringLength(80)]
    [Display(Name = "Aşı adı")]
    public string AsiAdi { get; set; } = "";

    [Required(ErrorMessage = "Yapıldığı tarih zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Yapıldığı tarih")]
    public DateOnly YapildigiTarih { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    [Display(Name = "Sonraki doz tarihi")]
    public DateOnly? SonrakiDozTarihi { get; set; }

    [StringLength(300)]
    [Display(Name = "Notlar")]
    public string? Notlar { get; set; }
}

/// <summary>
/// ÖDEV 4.1 — Randevu formu: hayvan → hizmet → tarih → hekim + saat.
/// Hekim ve saat, sayfadaki jQuery'nin REST API'den (GET /api/veterinerler/uygun) getirdiği
/// listeden seçilir ve gizli alanlara yazılır.
/// </summary>
public class RandevuFormVM
{
    public int Id { get; set; }

    // Required: kartla (radio) seçimde hiçbiri işaretli değilse alan hiç gönderilmez; mesaj Türkçe olsun
    [Required(ErrorMessage = "Hayvan seçin.")]
    [Range(1, int.MaxValue, ErrorMessage = "Hayvan seçin.")]
    [Display(Name = "Hayvan")]
    public int EvcilHayvanId { get; set; }

    // Required: kartla (radio) seçimde hiçbiri işaretli değilse alan hiç gönderilmez; mesaj Türkçe olsun
    [Required(ErrorMessage = "Hizmet seçin.")]
    [Range(1, int.MaxValue, ErrorMessage = "Hizmet seçin.")]
    [Display(Name = "Hizmet")]
    public int HizmetId { get; set; }

    [Required(ErrorMessage = "Tarih seçin.")]
    [DataType(DataType.Date)]
    [Display(Name = "Tarih")]
    public DateOnly Tarih { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Range(1, int.MaxValue, ErrorMessage = "Listeden bir hekim ve saat seçin.")]
    public int VeterinerId { get; set; }

    [Required(ErrorMessage = "Listeden bir saat seçin.")]
    [Display(Name = "Saat")]
    public TimeOnly? Saat { get; set; }

    [StringLength(500)]
    [Display(Name = "Not (şikâyet, özel durum)")]
    public string? SahipNotu { get; set; }

    // Form seçenekleri (POST'ta gelmez, controller her seferinde yeniden doldurur).
    // 25.11: açılır liste yerine kartlarla seçim yapıldığı için entity listesi taşınıyor
    // (hayvanın fotoğrafı/türü, hizmetin süresi ve ücreti kartta görünsün diye).
    public List<EvcilHayvan> Hayvanlar { get; set; } = [];
    public List<Hizmet> HizmetListesi { get; set; } = [];
}

/// <summary>ÖDEV 3.4 — Hekimin muayene notu formu.</summary>
public class MuayeneNotuVM
{
    public int RandevuId { get; set; }

    [StringLength(300)]
    [Display(Name = "Teşhis")]
    public string? Teshis { get; set; }

    [StringLength(500)]
    [Display(Name = "Tedavi")]
    public string? Tedavi { get; set; }

    [Required(ErrorMessage = "Muayene notu zorunludur.")]
    [StringLength(2000)]
    [Display(Name = "Muayene notu")]
    public string Notlar { get; set; } = "";
}

/// <summary>ÖDEV 6.4 — MCP erişim anahtarı üretme formu.</summary>
public class AnahtarUretVM
{
    [Required(ErrorMessage = "Anahtara bir ad verin.")]
    [StringLength(60)]
    [Display(Name = "Anahtar adı (ör. Claude Desktop)")]
    public string Ad { get; set; } = "";
}
