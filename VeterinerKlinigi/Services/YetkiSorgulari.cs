using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Services;

/// <summary>
/// "Bu kullanıcı HANGİ kayıtları görebilir?" kuralı — tek yerde.
///
/// ÖDEV Beklenen-Authorization — Üye adres çubuğunda id değiştirerek
/// (/EvcilHayvan/Detay/5 → /EvcilHayvan/Detay/6) başka üyenin hayvanını göremez.
///
/// NASIL? Kayıt önce Id ile çekilip SONRA kontrol edilmiyor; sorgunun KENDİSİ
/// filtreleniyor. Başkasının kaydı sorgudan hiç dönmez → servis null döner → 404.
/// Bu filtreler MVC, REST API ve MCP'nin hepsinde aynı servisler üzerinden çalışır.
/// (Bu açığın adı IDOR: Insecure Direct Object Reference.)
/// </summary>
public static class YetkiSorgulari
{
    /// <summary>
    /// Görülebilen hayvanlar:
    ///   Admin     → hepsi
    ///   Veteriner → kendisinden randevu almış hayvanlar
    ///   Üye       → sadece kendi hayvanları (SahipId = kendi Id'si)
    /// </summary>
    public static IQueryable<EvcilHayvan> ErisilebilirHayvanlar(this IQueryable<EvcilHayvan> sorgu, KullaniciBilgisi kb)
    {
        if (kb.AdminMi)
            return sorgu;
        if (kb.VeterinerMi)
            return sorgu.Where(h => h.Randevular.Any(r => r.Veteriner!.KullaniciId == kb.Id));
        return sorgu.Where(h => h.SahipId == kb.Id);
    }

    /// <summary>
    /// Görülebilen randevular:
    ///   Admin     → hepsi
    ///   Veteriner → sadece KENDİ randevuları
    ///   Üye       → sadece kendi hayvanlarının randevuları
    /// </summary>
    public static IQueryable<Randevu> ErisilebilirRandevular(this IQueryable<Randevu> sorgu, KullaniciBilgisi kb)
    {
        if (kb.AdminMi)
            return sorgu;
        if (kb.VeterinerMi)
            return sorgu.Where(r => r.Veteriner!.KullaniciId == kb.Id);
        return sorgu.Where(r => r.EvcilHayvan!.SahipId == kb.Id);
    }
}
