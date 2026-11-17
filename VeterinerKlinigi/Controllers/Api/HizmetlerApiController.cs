using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Dtos;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Controllers.Api;

/// <summary>
/// REST API — kliniğin aktif hizmetleri (herkese açık).
/// ÖDEV 5.1 / 5.3 — DTO + 200.
/// </summary>
[ApiController]
[Route("api/hizmetler")]
[Produces("application/json")]
public class HizmetlerApiController : ControllerBase
{
    private readonly IHizmetServisi _hizmetler;

    public HizmetlerApiController(IHizmetServisi hizmetler)
    {
        _hizmetler = hizmetler;
    }

    // GET /api/hizmetler
    /// <summary>Aktif hizmetleri süre ve ücretleriyle listeler.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<HizmetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<HizmetDto>>> Listele()
    {
        var hizmetler = await _hizmetler.ListeleAsync(sadeceAktifler: true);
        return Ok(hizmetler.Select(h => h.ToDto()).ToList());
    }
}
