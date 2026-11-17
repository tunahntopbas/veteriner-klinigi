namespace VeterinerKlinigi.Services;

/// <summary>
/// Evcil hayvan fotoğraflarını diske kaydeder, siler ve yolunu verir.
///
/// ÖDEV 3.2 — İsteğe bağlı fotoğraf.
///
/// FOTOĞRAFLAR NEREDE TUTULUYOR? (sunumda sorulan soru)
///   - Dosyanın KENDİSİ: proje klasöründeki Yuklemeler/hayvanlar/ altında, diskte.
///   - Veritabanında: sadece dosya ADI (EvcilHayvan.FotografDosyaAdi).
///   - NEDEN wwwroot DIŞINDA? wwwroot'taki her dosya adresini bilen herkese açıktır.
///     Biz fotoğrafı /EvcilHayvan/Fotograf/5 action'ı üzerinden veriyoruz; o action
///     önce "bu hayvanı görmeye yetkin var mı?" diye bakıyor.
///   - NEDEN veritabanında (byte[]) değil? Veritabanı şişer, yedekleme yavaşlar;
///     dosya sistemi büyük dosyalar için daha uygun.
///   - Dosya adı GUID: kullanıcının verdiği ad ("../../Program.cs" gibi) hiç kullanılmaz.
///   - Yuklemeler/ klasörü .gitignore'da → kullanıcı fotoğrafları depoya girmez.
/// </summary>
public interface IFotografServisi
{
    Task<IslemSonucu<string>> KaydetAsync(IFormFile dosya);
    void Sil(string? dosyaAdi);
    string? TamYol(string? dosyaAdi);
    string IcerikTuru(string dosyaAdi);
}

public class FotografServisi : IFotografServisi
{
    public const long EnBuyukBoyut = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] IzinliUzantilar = [".jpg", ".jpeg", ".png"];
    private static readonly string[] IzinliIcerikTurleri = ["image/jpeg", "image/png"];

    private readonly string _klasor;

    // IWebHostEnvironment: uygulamanın çalıştığı klasörü (ContentRootPath) verir.
    public FotografServisi(IWebHostEnvironment ortam)
    {
        _klasor = Path.Combine(ortam.ContentRootPath, "Yuklemeler", "hayvanlar");
        Directory.CreateDirectory(_klasor); // yoksa oluştur
    }

    public async Task<IslemSonucu<string>> KaydetAsync(IFormFile dosya)
    {
        // 1) Boyut
        if (dosya.Length == 0)
            return IslemSonucu<string>.Basarisiz("Fotoğraf dosyası boş.");
        if (dosya.Length > EnBuyukBoyut)
            return IslemSonucu<string>.Basarisiz("Fotoğraf en fazla 2 MB olabilir.");

        // 2) Uzantı ve tarayıcının bildirdiği içerik türü
        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        if (!IzinliUzantilar.Contains(uzanti) || !IzinliIcerikTurleri.Contains(dosya.ContentType))
            return IslemSonucu<string>.Basarisiz("Sadece JPG veya PNG yükleyebilirsiniz.");

        // 3) DİKKAT: Uzantı ve ContentType'ı istemci istediği gibi yazabilir.
        //    Dosyanın İLK BAYTLARINA ("sihirli sayı") bakıp gerçekten resim mi kontrol ediyoruz.
        if (!await GercektenResimMiAsync(dosya))
            return IslemSonucu<string>.Basarisiz("Dosya geçerli bir JPG/PNG resmi değil.");

        // 4) Yeni, tahmin edilemez ad: GUID + uzantı
        var yeniAd = $"{Guid.NewGuid():N}{(uzanti == ".jpeg" ? ".jpg" : uzanti)}";
        await using var akis = File.Create(Path.Combine(_klasor, yeniAd));
        await dosya.CopyToAsync(akis);

        return IslemSonucu<string>.Tamam(yeniAd);
    }

    public void Sil(string? dosyaAdi)
    {
        var yol = TamYol(dosyaAdi);
        if (yol is not null && File.Exists(yol))
            File.Delete(yol);
    }

    // Path.GetFileName → dosya adındaki klasör kısımlarını atar (dizin dışına çıkma saldırısına karşı).
    public string? TamYol(string? dosyaAdi) =>
        string.IsNullOrWhiteSpace(dosyaAdi) ? null : Path.Combine(_klasor, Path.GetFileName(dosyaAdi));

    public string IcerikTuru(string dosyaAdi) =>
        dosyaAdi.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";

    // JPEG: FF D8 FF   ·   PNG: 89 50 4E 47
    private static async Task<bool> GercektenResimMiAsync(IFormFile dosya)
    {
        var bas = new byte[4];
        await using var akis = dosya.OpenReadStream();
        var okunan = await akis.ReadAsync(bas);
        if (okunan < 4) return false;

        var jpeg = bas[0] == 0xFF && bas[1] == 0xD8 && bas[2] == 0xFF;
        var png = bas[0] == 0x89 && bas[1] == 0x50 && bas[2] == 0x4E && bas[3] == 0x47;
        return jpeg || png;
    }
}
