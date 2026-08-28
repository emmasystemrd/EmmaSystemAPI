using EmmaSystem.Application.DTOs.ECF;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ← Usa el mismo JWT que AuthController
    public class EcfRecibidoController : ControllerBase
    {
        private readonly IEcfRecibidoRepository _repository;

        public EcfRecibidoController(IEcfRecibidoRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Obtiene todos los comprobantes recibidos para un RNC Comprador específico.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<EcfRecibidoDto>>> ObtenerComprobantes(
            [FromQuery] string rncComprador,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rncComprador))
                return BadRequest(new { error = "El parámetro 'rncComprador' es obligatorio." });

            // Normalizar RNC (quitar guiones)
            rncComprador = rncComprador.Replace("-", "").Trim();

            var comprobantes = await _repository.ObtenerPorRncCompradorAsync(rncComprador, ct);
            return Ok(comprobantes);
        }

        /// <summary>
        /// Obtiene un comprobante recibido por su ID.
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EcfRecibidoDto>> ObtenerPorId(
            long id, CancellationToken ct = default)
        {
            var comprobante = await _repository.ObtenerPorIdAsync(id, ct);
            if (comprobante == null)
                return NotFound(new { error = $"No se encontró el comprobante con Id={id}" });

            return Ok(comprobante);
        }
    }
}