using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Dtos;

// =====================================================================
// ÖDEV 5.3 — REST API entity yerine DTO (Data Transfer Object) döndürür.
//
// NEDEN entity döndürmüyoruz?
//   1) Güvenlik: Kullanici entity'sini döndürseydik PasswordHash, SecurityStamp gibi
//      alanlar JSON'a çıkardı. DTO'da sadece göstermek istediğimiz alanlar var.
//   2) Döngü: Veteriner → Hizmetler → Veterinerler → ... sonsuz JSON döngüsü olurdu.
//   3) Sözleşme: Veritabanı tablosu değişse bile API'nin dış görünüşü sabit kalır.
//
// record → sadece veri taşıyan, değiştirilemez (immutable) küçük sınıflar için ideal.
// =====================================================================

/// <summary>Hizmetin kısa hâli (listelerde).</summary>
public record HizmetDto(int Id, string Ad, int SureDakika, decimal Ucret, string? Aciklama);

/// <summary>GET /api/veterinerler cevabındaki her hekim.</summary>
public record VeterinerDto(int Id, string AdSoyad, string? Unvan, List<string> Uzmanliklar, List<HizmetDto> Hizmetler);

/// <summary>GET /api/veterinerler/uygun cevabı: hekim + o günkü boş başlangıç saatleri.</summary>
public record UygunHekimDto(int VeterinerId, string AdSoyad, string? Unvan, List<string> Uzmanliklar, List<string> BosSaatler);

/// <summary>Bir aşı kaydı. KalanGun: sonraki doza kaç gün kaldı (yoksa null).</summary>
public record AsiDto(int Id, string AsiAdi, DateOnly YapildigiTarih, DateOnly? SonrakiDozTarihi, int? KalanGun, string? Notlar);

/// <summary>GET /api/evcil-hayvanlar/{id}/asilar cevabı.</summary>
public record AsiTakvimiDto(int EvcilHayvanId, string HayvanAdi, string Tur, List<AsiDto> Gecmis, List<AsiDto> Yaklasan);

/// <summary>Kullanıcının hayvanı (kısa hâl).</summary>
public record EvcilHayvanDto(int Id, string Ad, string Tur, string? Irk, DateOnly DogumTarihi, decimal? Kilo);

/// <summary>GET /api/raporlar/hizmet-ozeti cevabındaki her satır.</summary>
public record HizmetRaporDto(int HizmetId, string HizmetAdi, int RandevuSayisi, int TamamlananSayisi, decimal Ciro);

/// <summary>Randevu bilgisi (GET ve POST cevabı).</summary>
public record RandevuDto(
    int Id, int EvcilHayvanId, string HayvanAdi, string Hizmet, string Veteriner,
    DateTime Baslangic, DateTime Bitis, int SureDakika, decimal Ucret, string Durum, string? SahipNotu);

/// <summary>400 / 404 gibi hata cevaplarının gövdesi.</summary>
public record HataDto(string Hata);

/// <summary>
/// POST /api/randevular isteğinin gövdesi.
/// [ApiController] bu attribute'leri OTOMATİK kontrol eder; geçersizse action'a hiç
/// girmeden 400 (ValidationProblem) döner.
/// </summary>
public class RandevuOlusturDto
{
    [Range(1, int.MaxValue, ErrorMessage = "EvcilHayvanId zorunludur.")]
    public int EvcilHayvanId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "HizmetId zorunludur.")]
    public int HizmetId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "VeterinerId zorunludur.")]
    public int VeterinerId { get; set; }

    /// <summary>Klinik yerel saati, örn. "2026-12-01T10:30:00"</summary>
    [Required]
    public DateTime Baslangic { get; set; }

    [StringLength(500)]
    public string? SahipNotu { get; set; }
}
