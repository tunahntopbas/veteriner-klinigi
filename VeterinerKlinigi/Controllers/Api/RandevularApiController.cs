using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Dtos;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Controllers.Api;

/// <summary>
/// REST API — randevular.
///
/// ÖDEV 5.1 — REST API üzerinden veritabanına YAZMA (POST).
/// ÖDEV 5.3 — 201 Created (+ Location başlığı) / 400 (çakışma nedeniyle) / 401 / 404.
/// ÖDEV 6.3 — MVC sayfası ve MCP aracıyla AYNI RandevuServisi.OlusturAsync çağrılır.
///
/// CSRF NOTU: Bu uç çerezle kimlik doğrular. Çerez SameSite=Lax olduğu için başka bir
/// siteden gönderilen POST isteğine tarayıcı çerezi EKLEMEZ; ayrıca JSON gövdesi
/// (application/json) başka siteden "preflight" izni olmadan gönderilemez.
/// </summary>
[ApiController]
[Route("api/randevular")]
[Authorize]
[Produces("application/json")]
public class RandevularApiController : ControllerBase
{
    private readonly IRandevuServisi _randevular;

    public RandevularApiController(IRandevuServisi randevular)
    {
        _randevular = randevular;
    }

    // GET /api/randevular
    /// <summary>Giriş yapan kullanıcının görebildiği randevular.</summary>
    [HttpGet]
    [ProducesResponseType<List<RandevuDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<RandevuDto>>> Listele()
    {
        var liste = await _randevular.ListeleAsync(User.Bilgi());
        return Ok(liste.Select(r => r.ToDto()).ToList());
    }

    // GET /api/randevular/7
    /// <summary>Tek randevu.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<RandevuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RandevuDto>> Getir(int id)
    {
        var randevu = await _randevular.GetirAsync(id, User.Bilgi());
        if (randevu is null)
            return NotFound(new HataDto("Randevu bulunamadı."));
        return Ok(randevu.ToDto());
    }

    // POST /api/randevular
    // Gövde: { "evcilHayvanId": 1, "hizmetId": 1, "veterinerId": 1, "baslangic": "2026-12-01T10:30:00" }
    /// <summary>Yeni randevu oluşturur ("Bekliyor"). Uygun değilse 400 ve nedenini döner.</summary>
    [HttpPost]
    [ProducesResponseType<RandevuDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<HataDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RandevuDto>> Olustur([FromBody] RandevuOlusturDto girdi)
    {
        // [ApiController] sayesinde girdi geçersizse buraya hiç gelinmez (otomatik 400).
        var kb = User.Bilgi();
        var sonuc = await _randevular.OlusturAsync(
            new RandevuTalebi(girdi.EvcilHayvanId, girdi.HizmetId, girdi.VeterinerId, girdi.Baslangic, girdi.SahipNotu), kb);

        if (!sonuc.Basarili)
            return BadRequest(new HataDto(sonuc.Hata!));     // 400 — ör. "Hekim 10:00–10:30 arası başka randevuda"

        var olusan = await _randevular.GetirAsync(sonuc.Veri!.Id, kb);

        // 201 Created + Location: /api/randevular/{id} başlığı (yeni kaynağın adresi).
        return CreatedAtAction(nameof(Getir), new { id = olusan!.Id }, olusan.ToDto());
    }
}
