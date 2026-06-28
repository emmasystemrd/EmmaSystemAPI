using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EmmaSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VersionController : ControllerBase
    {
        private readonly IVersionService _versionService;

        public VersionController(IVersionService versionService)
        {
            _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
        }

        /// <summary>
        /// Verifica si existe una nueva versión disponible
        /// </summary>
        /// <param name="versionActual">Versión actual del cliente (ej: 5.6.26)</param>
        [HttpGet("verificar-actualizacion")]
        public async Task<IActionResult> VerificarActualizacion(string versionActual)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(versionActual))
                {
                    return BadRequest(new { message = "La versión actual es requerida" });
                }

                var resultado = await _versionService.VerificarActualizacionAsync(versionActual);

                if (resultado.HayActualizacion)
                {
                    return Ok(new
                    {
                        hayActualizacion = true,
                        versionNueva = resultado.VersionNueva,
                        urlInstalador = resultado.URL_Instalador,
                        urlScriptSQL = resultado.URL_ScriptSQL,
                        descripcion = resultado.Descripcion,
                        esObligatorio = resultado.EsObligatorio,
                        mensaje = $"Nueva versión disponible: {resultado.VersionNueva}"
                    });
                }

                return Ok(new
                {
                    hayActualizacion = false,
                    mensaje = "Su sistema está actualizado"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al verificar actualización: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene la última versión disponible
        /// </summary>
        [HttpGet("ultima-version")]
        public async Task<IActionResult> ObtenerUltimaVersion()
        {
            try
            {
                var version = await _versionService.ObtenerUltimaVersionAsync();
                return Ok(version);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}