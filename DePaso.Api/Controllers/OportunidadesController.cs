using DePaso.Logica.dtos;
using DePaso.Logica.servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DePaso.Api.Controllers;

[ApiController]
[Route("api/oportunidades")]
[Tags("Oportunidades")]
[Produces("application/json")]
public class OportunidadesController : ControllerBase
{
    private readonly IOportunidadesServicio _oportunidades;

    public OportunidadesController(IOportunidadesServicio oportunidades)
    {
        _oportunidades = oportunidades ?? throw new ArgumentNullException(nameof(oportunidades));
    }

    [HttpGet]
    [EndpointSummary("Próximas salidas con envíos compatibles")]
    [EndpointDescription("Salidas de los viajes activos del conductor en los próximos 14 días, con la cantidad de envíos que coinciden.")]
    [ProducesResponseType<IReadOnlyList<OportunidadSalidaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery, BindRequired] int usuarioId, CancellationToken ct) =>
        Ok(await _oportunidades.ListarSalidasAsync(usuarioId, ct));
}
