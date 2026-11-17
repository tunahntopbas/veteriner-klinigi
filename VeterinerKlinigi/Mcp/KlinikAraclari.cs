using System.ComponentModel;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using ModelContextProtocol.Server;
using VeterinerKlinigi.Dtos;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Mcp;

/// <summary>
/// MCP SUNUCUSUNUN ARAÇLARI — yapay zekâ asistanının çağırabileceği fonksiyonlar.
///
/// ÖDEV 6.1 — AI asistanının (Claude Desktop, VS Code, MCP Inspector) çağırabileceği MCP sunucusu.
/// ÖDEV 6.2 — En az üç araç, en az biri veri YAZAN:
///   uygun_saatler  (okur)  · asi_takvimi (okur) · randevu_olustur (YAZAR)
///   + hizmetler, hayvanlarim, randevularim (okur, asistanın id'leri öğrenmesi için)
/// ÖDEV 6.3 — Araçlar MVC ve REST API ile AYNI servisleri çağırır; iş kuralı burada YAZILMAZ.
///   uygun_saatler  → MusaitlikServisi.UygunHekimleriGetirAsync   (= GET /api/veterinerler/uygun)
///   asi_takvimi    → AsiServisi.AsiTakvimiAsync                  (= GET /api/evcil-hayvanlar/{id}/asilar)
///   randevu_olustur→ RandevuServisi.OlusturAsync                 (= POST /api/randevular, = Randevu/Olustur formu)
/// ÖDEV 6.4 — Her araç HttpContext.User'dan (Bearer anahtarın sahibi) KullaniciBilgisi alır;
///   servisler sorguyu bu kullanıcıya göre filtreler → başkasının verisi hiç dönmez.
///
/// NASIL ÇALIŞIR?
///   [McpServerToolType] → bu sınıfta araç var. [McpServerTool(Name = ...)] → metot bir araç.
///   [Description] metinleri yapay zekâya gönderilir; asistan aracı NE ZAMAN ve NASIL
///   çağıracağına bu açıklamalara bakarak karar verir. Parametre tipleri JSON şemasına çevrilir.
///   Program.cs: AddMcpServer().WithHttpTransport().WithTools&lt;KlinikAraclari&gt;() ve MapMcp("/mcp").
///   Sınıf her araç çağrısında DI ile oluşturulur → constructor injection çalışır.
/// </summary>
[McpServerToolType]
public class KlinikAraclari
{
    // Türkçe karakterler (ş, ı, ğ) ş gibi kaçış dizisine dönüşmesin, okunur kalsın.
    private static readonly JsonSerializerOptions JsonAyar = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IHttpContextAccessor _http;
    private readonly IMusaitlikServisi _musaitlik;
    private readonly IAsiServisi _asilar;
    private readonly IEvcilHayvanServisi _hayvanlar;
    private readonly IRandevuServisi _randevular;
    private readonly IHizmetServisi _hizmetler;

    public KlinikAraclari(IHttpContextAccessor http, IMusaitlikServisi musaitlik, IAsiServisi asilar,
        IEvcilHayvanServisi hayvanlar, IRandevuServisi randevular, IHizmetServisi hizmetler)
    {
        _http = http;
        _musaitlik = musaitlik;
        _asilar = asilar;
        _hayvanlar = hayvanlar;
        _randevular = randevular;
        _hizmetler = hizmetler;
    }

    // Aracı çağıran kişi: /mcp isteğindeki Bearer anahtarın sahibi.
    private KullaniciBilgisi Kullanici =>
        _http.HttpContext?.User.Bilgi() ?? throw new InvalidOperationException("İstek bağlamı yok.");

    // ---------------------------------------------------------------
    // Yardımcı (okuyan) araçlar
    // ---------------------------------------------------------------

    [McpServerTool(Name = "hizmetler", ReadOnly = true, Title = "Klinik hizmetleri")]
    [Description("Kliniğin verdiği hizmetleri id, süre (dakika) ve ücret (TL) ile listeler. " +
                 "uygun_saatler ve randevu_olustur araçları için gereken hizmetId buradan öğrenilir.")]
    public async Task<string> Hizmetler()
    {
        var liste = await _hizmetler.ListeleAsync(sadeceAktifler: true);
        return JsonSerializer.Serialize(liste.Select(h => h.ToDto()), JsonAyar);
    }

    [McpServerTool(Name = "hayvanlarim", ReadOnly = true, Title = "Hayvanlarım")]
    [Description("Giriş yapan kullanıcının kayıtlı evcil hayvanlarını (ad, tür, ırk, doğum tarihi, kilo) listeler.")]
    public async Task<string> Hayvanlarim()
    {
        var liste = await _hayvanlar.ListeleAsync(Kullanici);
        if (liste.Count == 0)
            return "Kayıtlı evcil hayvanınız yok. Web sitesinden 'Hayvanlarım > Yeni Hayvan' ile ekleyebilirsiniz.";
        return JsonSerializer.Serialize(liste.Select(h => h.ToDto()), JsonAyar);
    }

    [McpServerTool(Name = "randevularim", ReadOnly = true, Title = "Randevularım")]
    [Description("Giriş yapan kullanıcının randevularını (hizmet, hekim, tarih/saat, süre, ücret, durum) listeler.")]
    public async Task<string> Randevularim()
    {
        var liste = await _randevular.ListeleAsync(Kullanici);
        if (liste.Count == 0)
            return "Henüz randevunuz yok.";
        return JsonSerializer.Serialize(liste.Select(r => r.ToDto()), JsonAyar);
    }

    // ---------------------------------------------------------------
    // ÖDEV 6.2 — Örnek araç 1: uygun_saatler(hizmetId, tarih)
    // ---------------------------------------------------------------
    [McpServerTool(Name = "uygun_saatler", ReadOnly = true, Title = "Uygun hekim ve saatler")]
    [Description("Verilen gün ve hizmet için boş hekimleri ve boş başlangıç saatlerini döner. " +
                 "Hizmet süresi, kliniğin çalışma saatleri, hekim müsaitliği ve mevcut randevular hesaba katılır.")]
    public async Task<string> UygunSaatler(
        [Description("Hizmetin id'si (hizmetler aracından öğrenilebilir)")] int hizmetId,
        [Description("Tarih, yyyy-MM-dd biçiminde (örn. 2026-12-01)")] string tarih)
    {
        if (!DateOnly.TryParseExact(tarih, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var gun))
            return "Tarih yyyy-MM-dd biçiminde olmalı (örn. 2026-12-01).";
        if (gun < DateOnly.FromDateTime(DateTime.Today))
            return "Geçmiş bir tarih için boş saat sorgulanamaz.";

        var hizmet = await _hizmetler.GetirAsync(hizmetId);
        if (hizmet is null || !hizmet.Aktif)
            return $"{hizmetId} numaralı aktif bir hizmet yok. 'hizmetler' aracıyla listeyi görebilirsiniz.";

        var uygunlar = await _musaitlik.UygunHekimleriGetirAsync(hizmetId, gun);
        if (uygunlar.Count == 0)
            return $"{gun:dd.MM.yyyy} ({GunAdlari.Ad(gun.DayOfWeek)}) günü '{hizmet.Ad}' için boş hekim/saat yok. " +
                   "Klinik kapalı olabilir veya tüm saatler dolu. Başka bir gün deneyin.";

        return $"{gun:dd.MM.yyyy} {GunAdlari.Ad(gun.DayOfWeek)} — {hizmet.Ad} ({hizmet.SureDakika} dk, {hizmet.Ucret:0.##} TL):\n" +
               JsonSerializer.Serialize(uygunlar.Select(u => u.ToDto()), JsonAyar);
    }

    // ---------------------------------------------------------------
    // ÖDEV 6.2 — Örnek araç 2: asi_takvimi(hayvanAdi)
    // "Pamuk'un bir sonraki aşısı ne zaman?" sorusunu cevaplar.
    // ---------------------------------------------------------------
    [McpServerTool(Name = "asi_takvimi", ReadOnly = true, Title = "Aşı takvimi")]
    [Description("Kullanıcının hayvanlarından adı verilenin geçmiş aşılarını ve yaklaşan (sonraki doz) aşılarını, " +
                 "kalan gün sayısıyla birlikte döner. Örnek soru: 'Pamuk'un bir sonraki aşısı ne zaman?'")]
    public async Task<string> AsiTakvimi(
        [Description("Evcil hayvanın adı (örn. Pamuk)")] string hayvanAdi)
    {
        // DİKKAT: İsimle arama da kullanıcıya göre filtrelenir; başka üyenin "Pamuk"u bulunmaz.
        var hayvanlar = await _hayvanlar.AdIleBulAsync(hayvanAdi, Kullanici);
        if (hayvanlar.Count == 0)
            return $"'{hayvanAdi}' adında bir hayvanınız bulunamadı. 'hayvanlarim' aracıyla kayıtlı hayvanlarınızı görebilirsiniz.";

        var cevaplar = new List<AsiTakvimiDto>();
        foreach (var hayvan in hayvanlar)
        {
            var takvim = await _asilar.AsiTakvimiAsync(hayvan.Id, Kullanici);
            if (takvim is not null)
                cevaplar.Add(takvim.ToDto());
        }

        var ozet = string.Join("\n", cevaplar.Select(c =>
            c.Yaklasan.Count == 0
                ? $"{c.HayvanAdi}: yaklaşan aşı yok."
                : $"{c.HayvanAdi}: bir sonraki aşı {c.Yaklasan[0].AsiAdi}, {c.Yaklasan[0].SonrakiDozTarihi:dd.MM.yyyy} ({c.Yaklasan[0].KalanGun} gün sonra)."));

        return ozet + "\n\nAyrıntı:\n" + JsonSerializer.Serialize(cevaplar, JsonAyar);
    }

    // ---------------------------------------------------------------
    // ÖDEV 6.2 — Örnek araç 3 (VERİ YAZAN): randevu_olustur(...)
    // "Bekliyor" durumunda randevu oluşturur; çakışma varsa NEDENİNİ açıklar.
    // ---------------------------------------------------------------
    [McpServerTool(Name = "randevu_olustur", Destructive = false, Idempotent = false, Title = "Randevu oluştur")]
    [Description("Kullanıcının hayvanı için 'Bekliyor' durumunda yeni randevu oluşturur (veritabanına yazar). " +
                 "Saat uygun değilse (klinik kapalı, hekim müsait değil, başka randevuyla çakışma) nedenini açıklar. " +
                 "Önce uygun_saatler ile boş saatleri kontrol etmek önerilir.")]
    public async Task<string> RandevuOlustur(
        [Description("Evcil hayvanın adı (örn. Pamuk)")] string hayvanAdi,
        [Description("Hizmet id'si")] int hizmetId,
        [Description("Veteriner hekim id'si (uygun_saatler cevabındaki veterinerId)")] int veterinerId,
        [Description("Tarih, yyyy-MM-dd")] string tarih,
        [Description("Başlangıç saati, HH:mm (örn. 10:30)")] string saat,
        [Description("İsteğe bağlı not (şikâyet vb.)")] string? not = null)
    {
        if (!DateOnly.TryParseExact(tarih, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var gun))
            return "Tarih yyyy-MM-dd biçiminde olmalı.";
        if (!TimeOnly.TryParseExact(saat, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var baslangicSaati))
            return "Saat HH:mm biçiminde olmalı (örn. 10:30).";

        var hayvanlar = await _hayvanlar.AdIleBulAsync(hayvanAdi, Kullanici);
        // Randevu sadece hayvanın SAHİBİ adına alınır (veteriner başkasının hayvanına randevu açamaz).
        var kb = Kullanici;
        var hayvan = hayvanlar.FirstOrDefault(h => kb.AdminMi || h.SahipId == kb.Id);
        if (hayvan is null)
            return $"'{hayvanAdi}' adında bir hayvanınız bulunamadı.";

        // ÖDEV 6.3 — MVC formu ve REST API ile AYNI metot.
        var sonuc = await _randevular.OlusturAsync(
            new RandevuTalebi(hayvan.Id, hizmetId, veterinerId, gun.ToDateTime(baslangicSaati), not), kb);

        if (!sonuc.Basarili)
            return "Randevu oluşturulamadı: " + sonuc.Hata;   // ör. "Elif Kaya 10:00–10:30 arası başka randevuda..."

        var randevu = await _randevular.GetirAsync(sonuc.Veri!.Id, kb);
        var dto = randevu!.ToDto();
        return $"Randevu oluşturuldu (#{dto.Id}). Durum: {dto.Durum} — klinik onayından sonra kesinleşir.\n" +
               $"{dto.HayvanAdi} · {dto.Hizmet} · {dto.Veteriner} · {dto.Baslangic:dd.MM.yyyy HH:mm}–{dto.Bitis:HH:mm} · " +
               $"{dto.SureDakika} dk · {dto.Ucret:0.##} TL";
    }
}
