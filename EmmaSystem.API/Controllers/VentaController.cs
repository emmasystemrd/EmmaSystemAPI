using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Configuracion;
using EmmaSystem.Application.DTOs.Ecf;
using EmmaSystem.Application.DTOs.Venta;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/venta")]
[Authorize]
public sealed class VentaController : ControllerBase
{
    private readonly IVentaRepository _repo;
    private readonly IEcfXmlRepository _ecfRepo;
    private readonly IFacturacionElectronicaService _ecfService;
    private readonly ITokenDgiiService _tokenService;

    public VentaController(
        IVentaRepository repo,
        IEcfXmlRepository ecfRepo,
        IFacturacionElectronicaService ecfService,
        ITokenDgiiService tokenService)
    {
        _repo = repo;
        _ecfRepo = ecfRepo;
        _ecfService = ecfService;
        _tokenService = tokenService;
    }

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")?.Value ?? "1");
    private int GetIdLogin() => int.Parse(User.FindFirst("Idusuario")?.Value ?? "1");

    // === LISTADOS ===
    [HttpGet("listado")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<VentaListadoDto>>> GetVentasActivas(CancellationToken ct)
    {
        var result = await _repo.GetVentasActivasAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    [HttpGet("pendientes")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<VentaPendienteDto>>> GetVentasPendientes(CancellationToken ct)
    {
        var result = await _repo.GetVentasPendientesAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    [HttpGet("pendientes/buscar")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<VentaPendienteDto>>> SearchPendientes(
        [FromQuery] string texto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest("El parámetro 'texto' es requerido.");
        var result = await _repo.SearchVentasPendientesAsync(GetIdEmpresa(), texto.Trim(), ct);
        return Ok(result);
    }

    // === CARGA DE DETALLE ===
    [HttpGet("ncf/{ncf}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<VentaDetalleDto>> GetByNcf(string ncf, CancellationToken ct)
    {
        var result = await _repo.GetByNcfAsync(GetIdEmpresa(), ncf, ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró la venta con NCF {ncf}." });
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<VentaDetalleDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _repo.GetByIdAsync(GetIdEmpresa(), id, ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró la venta con ID {id}." });
        return Ok(result);
    }

    [HttpGet("{ncf}/pago")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<VentaPagoDto>> GetPagoInfo(string ncf, CancellationToken ct)
    {
        var result = await _repo.GetPagoInfoAsync(GetIdEmpresa(), ncf, ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró información de pago para NCF {ncf}." });
        return Ok(result);
    }

    // === CRUD ===
    [HttpPost]
    [Permission(Modules.Cliente, Operations.Crear)]
    public async Task<ActionResult<int>> Create([FromBody] VentaSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var idVenta = await _repo.InsertAsync(dto, GetIdEmpresa(), ct);
        return CreatedAtAction(nameof(GetById), new { id = idVenta }, idVenta);
    }

    [HttpPut("{id:int}")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] VentaSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        await _repo.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Permission(Modules.Cliente, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, GetIdLogin(), ct);
        return NoContent();
    }

    // === BÚSQUEDA AVANZADA ===
    [HttpGet("buscar")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<object>> Search(
        [FromQuery] DateTime? fecha1,
        [FromQuery] DateTime? fecha2,
        [FromQuery] bool? isFecha,
        [FromQuery] string? texto,
        [FromQuery] string? columna,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        try
        {
            // Valores por defecto
            var f1 = fecha1 ?? DateTime.Today;
            var f2 = fecha2 ?? DateTime.Today;
            var isF = isFecha ?? false;
            var txt = texto?.Trim() ?? "";
            var col = string.IsNullOrWhiteSpace(columna) ? "v.Nombre_Cliente" : columna;

            var result = await _repo.SearchByColumnAsync(
                f1, f2, isF, txt, col, GetIdEmpresa(), ct);

            // Aplicar paginación
            var totalItems = result.Count;
            var items = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                items,
                total = totalItems,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error al buscar: {ex.Message}" });
        }
    }

    [HttpPost("generar-ncf")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<ActionResult<NcfSecuenciaDto>> GenerarNcf(
        [FromQuery] string tipo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return BadRequest(new { message = "El tipo de comprobante es requerido." });
        var result = await _repo.GenerarSecuenciaNcfAsync(tipo.Trim(), ct);
        if (result is null || result.Desde == 0)
        {
            return BadRequest(new
            {
                message = $"No puede facturar Comprobante {tipo}. Solicítelo en la oficina virtual de la DGII."
            });
        }
        if (result.Vencimiento.HasValue && result.Vencimiento.Value < DateTime.Today)
        {
            return BadRequest(new
            {
                message = $"La secuencia de comprobantes {tipo} está vencida desde {result.Vencimiento.Value:dd/MM/yyyy}. Renueve en la DGII."
            });
        }
        var porcentajeUsado = (double)(result.Secuencia - result.Desde) / (double)(result.Hasta - result.Desde) * 100;
        if (porcentajeUsado >= 80 && !string.IsNullOrEmpty(result.Aviso))
        {
            result.Aviso = $"⚠️ {result.Aviso} (Uso: {porcentajeUsado:F1}%)";
        }
        return Ok(result);
    }

    // === DETALLE DE VENTA ===
    [HttpGet("{idVenta:int}/detalles")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<VentaDetalleItemDto>>> GetDetalles(
        int idVenta,
        CancellationToken ct)
    {
        var result = await _repo.GetDetallesByVentaAsync(idVenta, ct);
        return Ok(result);
    }

    [HttpPost("{idVenta:int}/detalles")]
    [Permission(Modules.Cliente, Operations.Crear)]
    public async Task<ActionResult> InsertarDetalle(
        int idVenta,
        [FromBody] VentaDetalleItemDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        await _repo.InsertarDetalleAsync(dto, idVenta, ct);
        return Ok(new { message = "Detalle agregado correctamente." });
    }

    [HttpPut("detalles/{idDetalle:int}")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<ActionResult> UpdateDetalle(
        int idDetalle,
        [FromBody] VentaDetalleItemDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        dto.Iddetalle = idDetalle;
        await _repo.UpdateDetalleAsync(dto, ct);
        return Ok(new { message = "Detalle actualizado correctamente." });
    }

    [HttpDelete("detalles/{idDetalle:int}")]
    [Permission(Modules.Cliente, Operations.Eliminar)]
    public async Task<ActionResult> DeleteDetalle(
        int idDetalle,
        CancellationToken ct)
    {
        await _repo.DeleteDetalleAsync(idDetalle, ct);
        return Ok(new { message = "Detalle eliminado correctamente." });
    }

    [HttpDelete("{idVenta:int}/detalles")]
    [Permission(Modules.Cliente, Operations.Eliminar)]
    public async Task<ActionResult> DeleteDetallesByVenta(
        int idVenta,
        CancellationToken ct)
    {
        await _repo.DeleteDetallesByVentaAsync(idVenta, ct);
        return Ok(new { message = "Todos los detalles de la venta fueron eliminados." });
    }

    // === REPORTES DE IMPRESIÓN ===
    [HttpGet("reporte/{noFactura}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<FacturaReporteDto>> GetFacturaReporte(
        string noFactura,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(noFactura))
            return BadRequest(new { message = "El NCF es requerido." });
        var result = await _repo.GetFacturaReporteAsync(noFactura.Trim(), GetIdEmpresa(), ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró la factura con NCF: {noFactura}" });
        return Ok(result);
    }

    [HttpGet("reporte/{idVenta:int}/detalle")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<FacturaDetalleReporteDto>>> GetFacturaDetalleReporte(
        int idVenta,
        CancellationToken ct)
    {
        var result = await _repo.GetFacturaDetalleReporteAsync(idVenta, ct);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // ENDPOINTS DE FACTURACIÓN ELECTRÓNICA
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("ecf/configuracion")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<ConfCertificadoDto>> GetConfiguracionCertificado(CancellationToken ct)
    {
        var config = await _ecfRepo.ObtenerConfiguracionCertificadoAsync(ct);
        if (config == null)
            return NotFound(new { message = "No hay configuración de facturación electrónica." });
        return Ok(config);
    }

    [HttpGet("ecf/listado")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<EcfEstadoDto>>> ListarEcf(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] string? estado,
        [FromQuery] string? tipoComprobante,
        CancellationToken ct)
    {
        var result = await _ecfRepo.ListarAsync(fechaInicio, fechaFin, estado, tipoComprobante, ct);
        return Ok(result);
    }

    [HttpGet("ecf/estado-por-ncf")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult> ConsultarEstadoPorNcf([FromQuery] string ncf, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ncf))
            return BadRequest(new { message = "El NCF es requerido." });
        var estado = await _ecfRepo.ObtenerEstadoAsync(ncf.Trim(), ct);
        return Ok(new { estadoEcf = estado });
    }

    [HttpGet("ecf/{idEcf:long}/estado-dgii")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<RespuestaConsultaDgiiDto>> ConsultarEstadoDgii(
        long idEcf, CancellationToken ct)
    {
        var (xmlCompleto, rncEmisor, secuencia) = await _ecfRepo.ObtenerXmlCompletoAsync(idEcf, ct);
        if (xmlCompleto == null)
            return NotFound(new { message = "ECF no encontrado." });

        var config = await _ecfRepo.ObtenerConfiguracionCertificadoAsync(ct);
        if (config == null)
            return BadRequest(new { message = "No hay configuración de certificado." });

        string token = await _tokenService.ObtenerTokenAsync(config.Ambiente,
            (await _ecfRepo.ObtenerCertificadoDigitalAsync(ct)).Certificado!,
            (await _ecfRepo.ObtenerCertificadoDigitalAsync(ct)).Clave!);

        var result = await _ecfService.ConsultarEstadoDGIIAsync(secuencia, token, config.Ambiente);
        return Ok(result);
    }

    [HttpGet("ecf/{idEcf:long}/xml")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult> ObtenerXmlEcf(long idEcf, CancellationToken ct)
    {
        var (xmlCompleto, rncEmisor, secuencia) = await _ecfRepo.ObtenerXmlCompletoAsync(idEcf, ct);
        if (xmlCompleto == null)
            return NotFound(new { message = "ECF no encontrado." });
        return Content(xmlCompleto, "application/xml", System.Text.Encoding.UTF8);
    }

    [HttpPost("{idVenta:int}/firmar-enviar")]
    [Permission(Modules.Cliente, Operations.Crear)]
    public async Task<ActionResult<EcfEnvioResultadoDto>> FirmarYEnviarEcf(
        int idVenta,
        [FromQuery] int ambiente,
        CancellationToken ct)
    {
        try
        {
            var venta = await _repo.GetByIdAsync(GetIdEmpresa(), idVenta, ct);
            if (venta == null)
                return NotFound(new { message = $"No se encontró la venta con ID {idVenta}" });

            var config = await _ecfRepo.ObtenerConfiguracionCertificadoAsync(ct);
            if (config == null || !config.TieneCertificado || !config.TieneClave)
                return BadRequest(new { message = "No hay certificado digital configurado. Ve a Configuración → Facturación Electrónica." });

            var (certBytes, claveCert) = await _ecfRepo.ObtenerCertificadoDigitalAsync(ct);
            if (certBytes == null || certBytes.Length == 0)
                return BadRequest(new { message = "No se pudo obtener el certificado digital." });

            var datosEmpresa = await _ecfRepo.ObtenerDatosEmpresaAsync(GetIdEmpresa(), ct);
            var datosCliente = await _ecfRepo.ObtenerDatosClienteAsync(venta.Idcliente, ct);

            // ✅ CONSULTAR EL COBRO PARA OBTENER RETENCIONES Y FORMAS DE PAGO
            var pago = await _repo.GetPagoInfoAsync(GetIdEmpresa(), venta.NCF, ct);

            // ✅ Variables locales para evitar warnings de null del compilador
            decimal? retITBIS = pago?.Retencion_ITBIS;
            decimal? retISR = pago?.Retencion_ISR;

            var datosEcf = new DatosFacturaElectronicaDto
            {
                TipoComprobante = venta.Tipo,
                ENCF = venta.NCF,
                RNCEmisor = datosEmpresa.RNC,
                RazonSocialEmisor = datosEmpresa.Nombre,
                DireccionEmisor = datosEmpresa.Direccion,
                CorreoEmisor = datosEmpresa.Email,
                WebSite = datosEmpresa.Url,
                FechaEmision = venta.Fecha.ToString("dd-MM-yyyy"),
                // ✅ Comprobante Consumo (32) no lleva FechaVencimientoSecuencia; el resto sí.
                FechaVencimientoSecuencia = venta.Tipo != "32" && venta.Vencimiento.HasValue
                    ? venta.Vencimiento.Value.ToString("dd-MM-yyyy")
                    : null,
                RNCComprador = datosCliente.NumDocumento,
                RazonSocialComprador = datosCliente.RazonSocial,
                CorreoComprador = datosCliente.Email,
                DireccionComprador = datosCliente.Direccion,
                TipoPago = venta.Contado == "Y" ? "1" : "2",
                TipoIngresos = "01",
                IndicadorEnvioDiferido = "1",
                IndicadorMontoGravado = "0",
                IndicadorServicioTodoIncluido = "1",
                MontoTotal = venta.Subtotal + venta.ITBIS,
                ValorPagar = venta.Subtotal + venta.ITBIS,
                
                // ✅ AGREGAR RETENCIONES TOTALES
                // ✅ CORRECTO: usar 0m cuando no hay retención
                TotalITBISRetenido = retITBIS > 0 ? retITBIS.Value : 0m,
                TotalISRRetencion = retISR > 0 ? retISR.Value : 0m,
                Items = new List<ItemEcfDto>(),
                FormasPago = new List<FormaPagoEcfDto>()
            };

            // ✅ CONSTRUIR FORMAS DE PAGO
            if (pago != null)
            {
                if (pago.Efectivo > 0)
                    datosEcf.FormasPago.Add(new FormaPagoEcfDto { FormaPago = "1", MontoPago = pago.Efectivo });
                if (pago.Cheque > 0 || pago.Transferencia > 0)
                    datosEcf.FormasPago.Add(new FormaPagoEcfDto { FormaPago = "2", MontoPago = pago.Cheque + pago.Transferencia });
                if (pago.Tarjeta > 0)
                    datosEcf.FormasPago.Add(new FormaPagoEcfDto { FormaPago = "3", MontoPago = pago.Tarjeta });

                // Si no hay ningún pago real (solo retención), usar FormaPago = 4
                // ✅ CORREGIDO: usar variables locales con ?? para convertir nullable a decimal
                if (!datosEcf.FormasPago.Any())
                {
                    decimal montoCredito = datosEcf.ValorPagar - ((retITBIS ?? 0m) + (retISR ?? 0m));
                    if (montoCredito > 0)
                        datosEcf.FormasPago.Add(new FormaPagoEcfDto { FormaPago = "4", MontoPago = montoCredito });
                }
            }

            // ✅ CARGAR DETALLES (BIENES) — clasificando por tasa real de ITBIS
            var detalles = await _repo.GetDetallesByVentaAsync(idVenta, ct);

            decimal montoGravado18 = 0m, montoGravado16 = 0m, montoExento = 0m;
            decimal totalItbis18 = 0m, totalItbis16 = 0m;

            foreach (var det in detalles)
            {
                decimal tasa = Math.Round(det.P_Itbis, 4);
                int indicador;

                if (tasa >= 0.17m)          // 18%
                {
                    indicador = 1;
                    montoGravado18 += det.Subtotal;
                    totalItbis18 += det.ITBIS;
                }
                else if (tasa > 0m)         // 16%
                {
                    indicador = 2;
                    montoGravado16 += det.Subtotal;
                    totalItbis16 += det.ITBIS;
                }
                else                        // Exento
                {
                    indicador = 4;
                    montoExento += det.Subtotal;
                }

                datosEcf.Items.Add(new ItemEcfDto
                {
                    IndicadorFacturacion = indicador,
                    NombreItem = det.Producto,
                    DescripcionItem = det.Producto,
                    IndicadorBienoServicio = 1, // Bien
                    CantidadItem = det.Cantidad,
                    UnidadMedida = 43,
                    PrecioUnitarioItem = det.Precio_Venta1,
                    MontoItem = det.Subtotal
                });
            }

            // ✅ SI HAY MONTO EN SERVICIOS, CREAR ITEM (sin importar si también hay bienes)
            if (venta.Monto_Servicios > 0)
            {
                int indicadorServicio = venta.ITBIS_Servicios > 0 ? 1 : 4;

                if (indicadorServicio == 1)
                {
                    montoGravado18 += venta.Monto_Servicios;
                    totalItbis18 += venta.ITBIS_Servicios;
                }
                else
                {
                    montoExento += venta.Monto_Servicios;
                }

                datosEcf.Items.Add(new ItemEcfDto
                {
                    IndicadorFacturacion = indicadorServicio,
                    NombreItem = "SERVICIO",
                    DescripcionItem = venta.Descripcion ?? "Servicio profesional",
                    IndicadorBienoServicio = 2, // Servicio
                    CantidadItem = 1.00m,
                    UnidadMedida = 43,
                    PrecioUnitarioItem = venta.Monto_Servicios,
                    MontoItem = venta.Monto_Servicios
                });
            }

            // ✅ VALIDAR QUE HAYA AL MENOS UN ITEM
            if (!datosEcf.Items.Any())
            {
                return BadRequest(new { message = "La venta no tiene detalles (productos) ni servicios. No se puede generar el XML." });
            }

            // ✅ TOTALES REALES A PARTIR DE LOS ITEMS (18% + 16% + exento)
            datosEcf.MontoGravadoTotal = (montoGravado18 + montoGravado16) > 0 ? montoGravado18 + montoGravado16 : null;
            datosEcf.MontoGravadoI1 = montoGravado18 > 0 ? montoGravado18 : null;
            datosEcf.MontoGravadoI2 = montoGravado16 > 0 ? montoGravado16 : null;
            datosEcf.MontoExento = montoExento > 0 ? montoExento : null;
            datosEcf.TotalITBIS1 = totalItbis18 > 0 ? totalItbis18 : null;
            datosEcf.TotalITBIS2 = totalItbis16 > 0 ? totalItbis16 : null;
            datosEcf.TotalITBIS = (totalItbis18 + totalItbis16) > 0 ? totalItbis18 + totalItbis16 : null;
            

            // ✅ REPARTIR RETENCIONES ENTRE ÍTEMS (versión corregida)
            if ((retITBIS ?? 0) > 0 || (retISR ?? 0) > 0)
            {
                decimal montoTotalVenta = datosEcf.Items.Sum(i => i.MontoItem);
                decimal retencionITBISAcumulada = 0m;
                decimal retencionISRAcumulada = 0m;
                decimal totalRetITBIS = retITBIS ?? 0m;
                decimal totalRetISR = retISR ?? 0m;

                for (int i = 0; i < datosEcf.Items.Count; i++)
                {
                    var item = datosEcf.Items[i];
                    bool esUltimoItem = (i == datosEcf.Items.Count - 1);

                    if (esUltimoItem)
                    {
                        // Ajustar el último ítem para que la suma exacta dé el total
                        item.MontoITBISRetenido = totalRetITBIS - retencionITBISAcumulada;
                        item.MontoISRRetenido = totalRetISR - retencionISRAcumulada;
                    }
                    else
                    {
                        // Prorrateo proporcional
                        decimal proporcion = item.MontoItem / montoTotalVenta;
                        decimal retITBISItem = Math.Round(totalRetITBIS * proporcion, 2);
                        decimal retISRItem = Math.Round(totalRetISR * proporcion, 2);

                        item.MontoITBISRetenido = retITBISItem;
                        item.MontoISRRetenido = retISRItem;

                        retencionITBISAcumulada += retITBISItem;
                        retencionISRAcumulada += retISRItem;
                    }

                    // Marcar indicador de agente de retención si hay retenciones
                    if ((item.MontoITBISRetenido ?? 0m) > 0 || (item.MontoISRRetenido ?? 0m) > 0)
                    {
                        item.IndicadorAgenteRetencionoPercepcion = "1";
                    }
                }
            }

            string token = await _tokenService.ObtenerTokenAsync(ambiente, certBytes, claveCert!);

            var resultado = await _ecfService.FirmarYEnviarAsync(
                datosEcf, certBytes, claveCert!, token, ambiente, idVenta);

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al procesar facturación electrónica: {ex.Message}" });
        }
    }

    [HttpGet("{idVenta:int}/estado-ecf")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult> ConsultarEstadoEcf(int idVenta, CancellationToken ct)
    {
        var venta = await _repo.GetByIdAsync(GetIdEmpresa(), idVenta, ct);
        if (venta == null)
            return NotFound(new { message = $"No se encontró la venta con ID {idVenta}" });
        var estado = await _ecfRepo.ObtenerEstadoAsync(venta.NCF, ct);
        return Ok(new { ncf = venta.NCF, estado });
    }
    /// <summary>
    /// Consulta el estado de uno o más comprobantes electrónicos por NCF
    /// </summary>
    [HttpPost("ecf/estados")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<Dictionary<string, string>>> ConsultarEstadosEcf(
        [FromBody] List<string> ncfs,
        CancellationToken ct)
    {
        if (ncfs == null || !ncfs.Any())
            return Ok(new Dictionary<string, string>());

        // Filtrar solo NCFs electrónicos (comienzan con E)
        var ncfsEcf = ncfs.Where(n => n.StartsWith("E") && n.Length == 13).ToList();

        if (!ncfsEcf.Any())
            return Ok(new Dictionary<string, string>());

        var estados = await _ecfRepo.ObtenerEstadosPorNcfAsync(ncfsEcf, ct);
        return Ok(estados);
    }
    [HttpGet("{ncf}/ecf-datos")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<EcfDatosImpresionDto>> ObtenerDatosEcf(string ncf, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ncf))
            return BadRequest(new { message = "El NCF es requerido." });

        var datos = await _ecfRepo.ObtenerDatosEcfAsync(ncf.Trim(), ct);
        if (datos == null)
            return NotFound(new { message = $"No se encontraron datos ECF para NCF {ncf}" });

        return Ok(datos);
    }

}