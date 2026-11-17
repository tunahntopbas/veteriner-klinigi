namespace VeterinerKlinigi.Services;

/// <summary>
/// Servis metotlarının ortak dönüş tipi: işlem başarılı mı, değilse NEDEN?
///
/// NEDEN exception fırlatmak yerine bu nesne?
///   "Hekim o saatte dolu" bir HATA değil, beklenen bir iş kuralı sonucu.
///   Aynı sonucu üç farklı yer üç farklı şekilde gösterir (ÖDEV 6.3):
///     - MVC controller   → ModelState.AddModelError(...) ile formda kırmızı yazı
///     - REST API         → 400 Bad Request (Bulunamadi ise 404) + açıklama
///     - MCP aracı        → yapay zekâya okunabilir açıklama metni
/// </summary>
public class IslemSonucu
{
    public bool Basarili { get; init; }

    // Başarısızsa kullanıcıya gösterilecek açıklama.
    public string? Hata { get; init; }

    // Kayıt yok YA DA kullanıcının bu kayda erişim yetkisi yok.
    // İkisini bilerek ayırmıyoruz: başkasının hayvanı için "yetkin yok" demek,
    // o id'de bir kayıt OLDUĞUNU sızdırır. İkisi de 404.
    public bool Bulunamadi { get; init; }

    public static IslemSonucu Tamam() => new() { Basarili = true };
    public static IslemSonucu Basarisiz(string hata) => new() { Hata = hata };
    public static IslemSonucu KayitYok() => new() { Bulunamadi = true, Hata = "Kayıt bulunamadı." };
}

/// <summary>Başarılıysa bir veri de taşıyan sürüm (ör. oluşturulan randevu).</summary>
public class IslemSonucu<T> : IslemSonucu
{
    public T? Veri { get; init; }

    public static IslemSonucu<T> Tamam(T veri) => new() { Basarili = true, Veri = veri };
    public new static IslemSonucu<T> Basarisiz(string hata) => new() { Hata = hata };
    public new static IslemSonucu<T> KayitYok() => new() { Bulunamadi = true, Hata = "Kayıt bulunamadı." };
}
