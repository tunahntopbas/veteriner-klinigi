using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Dtos;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Controllers.Api;

/// <summary>
/// REST API — hekimler.
///
/// ÖDEV 5.1 — REST API ile veritabanı iletişimi.
/// ÖDEV 5.2 — LINQ ile filtreleme: "tüm hekimleri listeleme" ve
///            "belirli tarihte seçilen hizmet için uygun hekimler".
/// ÖDEV 5.3 — DTO döner, doğru durum kodları (200 / 400 / 404).
///
/// NEREDE TÜKETİLİYOR? (sunumda sorulan soru)
///   GET /api/veterinerler/uygun → Views/Randevu/Olustur.cshtml içindeki jQuery
///   ($.getJSON) hizmet ve tarih seçilince bu ucu çağırıp hekim + boş saat listesini çizer.
///
/// [ApiController]: model doğrulamasını otomatik yapar (geçersizse 400),
/// parametreleri sorgu dizesinden/gövdeden akıllıca bağlar.
/// MVC controller'dan farkı: View() değil, JSON (Ok(...)) döner → ControllerBase yeterli.
/// </summary>
[ApiController]
[Route("api/veterinerler")]
[Produces("application/json")]
public class VeterinerlerApiController : ControllerBase
{
    private readonly IVeterinerServisi _veterinerler;
    private readonly IMusaitlikServisi _musaitlik;
    private readonly IHizmetServisi _hizmetler;

    public VeterinerlerApiController(IVeterinerServisi veterinerler, IMusaitlikServisi musaitlik, IHizmetServisi hizmetler)
    {
        _veterinerler = veterinerler;
        _musaitlik = musaitlik;
        _hizmetler = hizmetler;
    }

    // GET /api/veterinerler
    // GET /api/veterinerler?uzmanlik=cerrahi
    // ÖDEV 5.2 — Tüm hekimleri listeleme (+ isteğe bağlı uzmanlığa göre LINQ filtresi).
    /// <summary>Aktif hekimleri listeler. İsteğe bağlı: uzmanlık adına göre filtre.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<VeterinerDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VeterinerDto>>> Listele([FromQuery] string? uzmanlik)
    {
        var hekimler = await _veterinerler.ListeleAsync(sadeceAktifler: true);

        var sonuc = hekimler
            .Where(v => string.IsNullOrWhiteSpace(uzmanlik)
                        || v.Uzmanliklar.Any(u => u.Ad.Contains(uzmanlik, StringComparison.OrdinalIgnoreCase)))
            .Select(v => v.ToDto())
            .ToList();

        return Ok(sonuc);                                   // 200
    }

    // GET /api/veterinerler/3
    /// <summary>Tek bir hekimin bilgileri.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<VeterinerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VeterinerDto>> Getir(int id)
    {
        var hekim = await _veterinerler.GetirAsync(id);
        if (hekim is null || !hekim.Aktif)
            return NotFound(new HataDto("Hekim bulunamadı."));   // 404
        return Ok(hekim.ToDto());                                 // 200
    }

    // GET /api/veterinerler/uygun?hizmetId=1&tarih=2026-12-01
    // ÖDEV 5.2 — "Belirli bir tarihte seçilen hizmet için uygun hekimler".
    /// <summary>Seçilen gün ve hizmet için uygun hekimler ve boş başlangıç saatleri.</summary>
    [HttpGet("uygun")]
    [AllowAnonymous]
    [ProducesResponseType<List<UygunHekimDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<HataDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<UygunHekimDto>>> Uygun([FromQuery] int hizmetId, [FromQuery] DateOnly tarih)
    {
        if (tarih < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new HataDto("Geçmiş bir tarih için uygunluk sorgulanamaz."));   // 400

        var hizmet = await _hizmetler.GetirAsync(hizmetId);
        if (hizmet is null || !hizmet.Aktif)
            return NotFound(new HataDto("Hizmet bulunamadı."));                               // 404

        var uygunlar = await _musaitlik.UygunHekimleriGetirAsync(hizmetId, tarih);
        return Ok(uygunlar.Select(u => u.ToDto()).ToList());                                 // 200 (boş liste de olabilir)
    }
}
