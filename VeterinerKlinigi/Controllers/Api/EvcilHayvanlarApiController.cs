using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Dtos;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Controllers.Api;

/// <summary>
/// REST API — giriş yapan kullanıcının hayvanları ve aşı takvimi.
///
/// ÖDEV 5.2 — "Bir hayvanın aşı geçmişi ve yaklaşan aşıları".
/// ÖDEV 5.3 — DTO, 200 / 401 / 404.
/// ÖDEV Beklenen-Authorization — Başkasının hayvanının id'si verilirse 404
///   (servis sorguyu kullanıcıya göre filtreler; kayıt hiç dönmez).
///
/// NEREDE TÜKETİLİYOR? Views/EvcilHayvan/Detay.cshtml → "Aşı takvimi" kutusu bu ucu
/// jQuery ile çağırıp yaklaşan aşıları ve kalan gün sayısını gösterir.
/// </summary>
[ApiController]
[Route("api/evcil-hayvanlar")]
[Authorize]                         // giriş yoksa 401 (Program.cs'teki OnRedirectToLogin ayarı)
[Produces("application/json")]
public class EvcilHayvanlarApiController : ControllerBase
{
    private readonly IEvcilHayvanServisi _hayvanlar;
    private readonly IAsiServisi _asilar;

    public EvcilHayvanlarApiController(IEvcilHayvanServisi hayvanlar, IAsiServisi asilar)
    {
        _hayvanlar = hayvanlar;
        _asilar = asilar;
    }

    // GET /api/evcil-hayvanlar
    /// <summary>Giriş yapan kullanıcının erişebildiği hayvanlar.</summary>
    [HttpGet]
    [ProducesResponseType<List<EvcilHayvanDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<EvcilHayvanDto>>> Listele()
    {
        var hayvanlar = await _hayvanlar.ListeleAsync(User.Bilgi());
        return Ok(hayvanlar.Select(h => h.ToDto()).ToList());
    }

    // GET /api/evcil-hayvanlar/5/asilar
    /// <summary>Hayvanın aşı geçmişi ve yaklaşan aşıları.</summary>
    [HttpGet("{id:int}/asilar")]
    [ProducesResponseType<AsiTakvimiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<HataDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsiTakvimiDto>> Asilar(int id)
    {
        var takvim = await _asilar.AsiTakvimiAsync(id, User.Bilgi());
        if (takvim is null)
            return NotFound(new HataDto("Hayvan bulunamadı."));     // 404 (yok VEYA başkasının)
        return Ok(takvim.ToDto());                                   // 200
    }
}
