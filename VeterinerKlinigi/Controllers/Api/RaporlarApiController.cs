using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Dtos;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Controllers.Api;

/// <summary>
/// REST API — raporlar (sadece Admin).
///
/// ÖDEV 5.2 — "Hizmet türüne göre randevu sayısı ve ciro raporu" (LINQ GroupBy → RaporServisi).
/// ÖDEV 5.3 — DTO, 200 / 400 / 401 / 403.
///
/// NEREDE TÜKETİLİYOR? Areas/Admin/Views/Rapor/Index.cshtml → tarih aralığı seçilince
/// jQuery bu ucu çağırır ve tabloyu + toplamları sayfayı yenilemeden çizer.
/// </summary>
[ApiController]
[Route("api/raporlar")]
[Authorize(Roles = Roller.Admin)]   // giriş yok → 401, rol Admin değil → 403
[Produces("application/json")]
public class RaporlarApiController : ControllerBase
{
    private readonly IRaporServisi _raporlar;

    public RaporlarApiController(IRaporServisi raporlar)
    {
        _raporlar = raporlar;
    }

    // GET /api/raporlar/hizmet-ozeti?baslangic=2026-11-01&bitis=2026-11-30
    /// <summary>Tarih aralığında hizmet bazında randevu sayısı, tamamlanan sayısı ve ciro.</summary>
    [HttpGet("hizmet-ozeti")]
    [ProducesResponseType<List<HizmetRaporDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<HataDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<HizmetRaporDto>>> HizmetOzeti([FromQuery] DateOnly baslangic, [FromQuery] DateOnly bitis)
    {
        if (baslangic == default || bitis == default)
            return BadRequest(new HataDto("baslangic ve bitis tarihleri zorunludur (yyyy-MM-dd)."));
        if (baslangic > bitis)
            return BadRequest(new HataDto("Başlangıç tarihi bitiş tarihinden sonra olamaz."));
        if (bitis.DayNumber - baslangic.DayNumber > 366)
            return BadRequest(new HataDto("En fazla 1 yıllık aralık sorgulanabilir."));

        var ozet = await _raporlar.HizmetOzetiAsync(baslangic, bitis);
        return Ok(ozet.Select(o => o.ToDto()).ToList());
    }
}
