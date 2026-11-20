using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.ViewModels;

/// <summary>
/// Ana sayfanın ihtiyaç duyduğu her şey tek modelde: klinik (adres + çalışma saatleri),
/// aktif hizmetler ve aktif hekimler. Veri controller'da hazırlanır, view sadece gösterir.
/// "Canlı boş saatler" kutusu ise burada DEĞİL: sayfa açıldıktan sonra jQuery ile
/// REST API'den (GET /api/veterinerler/uygun) çekilir.
/// </summary>
public class AnaSayfaVM
{
    public required Klinik Klinik { get; init; }
    public List<Hizmet> Hizmetler { get; init; } = [];
    public List<Veteriner> Hekimler { get; init; } = [];
}
