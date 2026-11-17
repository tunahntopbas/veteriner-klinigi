using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Dtos;

/// <summary>
/// Entity → DTO dönüşümleri. Tek yerde durduğu için REST API ve MCP araçları
/// aynı dönüşümü kullanır. (AutoMapper gibi bir kütüphane yerine elle, açık açık.)
/// </summary>
public static class DtoDonusturucu
{
    public static HizmetDto ToDto(this Hizmet h) => new(h.Id, h.Ad, h.SureDakika, h.Ucret, h.Aciklama);

    public static VeterinerDto ToDto(this Veteriner v) => new(
        v.Id, v.AdSoyad, v.Unvan,
        v.Uzmanliklar.Select(u => u.Ad).OrderBy(a => a).ToList(),
        v.Hizmetler.Where(h => h.Aktif).OrderBy(h => h.Ad).Select(h => h.ToDto()).ToList());

    public static UygunHekimDto ToDto(this UygunHekim u) => new(
        u.VeterinerId, u.AdSoyad, u.Unvan, u.Uzmanliklar,
        u.BosSaatler.Select(s => s.ToString("HH:mm")).ToList());

    public static AsiDto ToDto(this AsiKaydi a, DateOnly bugun) => new(
        a.Id, a.AsiAdi, a.YapildigiTarih, a.SonrakiDozTarihi,
        a.SonrakiDozTarihi is { } sonraki ? sonraki.DayNumber - bugun.DayNumber : null,
        a.Notlar);

    public static AsiTakvimiDto ToDto(this AsiTakvimi t)
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today);
        return new AsiTakvimiDto(t.Hayvan.Id, t.Hayvan.Ad, t.Hayvan.Tur,
            t.Gecmis.Select(a => a.ToDto(bugun)).ToList(),
            t.Yaklasan.Select(a => a.ToDto(bugun)).ToList());
    }

    public static EvcilHayvanDto ToDto(this EvcilHayvan h) => new(h.Id, h.Ad, h.Tur, h.Irk, h.DogumTarihi, h.Kilo);

    public static HizmetRaporDto ToDto(this HizmetOzeti o) =>
        new(o.HizmetId, o.HizmetAdi, o.RandevuSayisi, o.TamamlananSayisi, o.Ciro);

    public static RandevuDto ToDto(this Randevu r) => new(
        r.Id, r.EvcilHayvanId, r.EvcilHayvan?.Ad ?? "", r.Hizmet?.Ad ?? "", r.Veteriner?.AdSoyad ?? "",
        r.BaslangicZamani, r.BitisZamani, r.SureDakika, r.Ucret,
        RandevuServisi.DurumAdi(r.Durum), r.SahipNotu);
}
