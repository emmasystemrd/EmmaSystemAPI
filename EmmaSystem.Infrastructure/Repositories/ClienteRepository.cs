using Dapper;
using EmmaSystem.Application.DTOs.Catalogos;
using EmmaSystem.Application.DTOs.Cliente;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class ClienteRepository : IClienteRepository
{
    private readonly SqlConnectionFactory _factory;
    public ClienteRepository(SqlConnectionFactory factory) => _factory = factory;


    public async Task<IReadOnlyList<ClienteDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        var result = await conn.QueryAsync<ClienteDto>(
            new CommandDefinition("[dbo].[spmostrar_cliente1]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
        return result.AsList();
    }
    // ... código existente ...

    public async Task<ClienteDetalleDto?> GetByCodigoOrDocumentoAsync(string textoBuscar, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@TextoBuscar", textoBuscar, DbType.String);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        var result = await conn.QueryFirstOrDefaultAsync<ClienteDetalleDto>(
            new CommandDefinition(
                "[dbo].[spbuscar_cliente1_codigo]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result;
    }

    public async Task<IReadOnlyList<ClienteDto>> BuscarClientesActivosAsync(string textoBuscar, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var result = await conn.QueryAsync<ClienteDto>(
            new CommandDefinition(
                "[dbo].[spbuscar_cliente1_activo]",
                new
                {
                    TextoBuscar = string.IsNullOrWhiteSpace(textoBuscar) ? "" : textoBuscar.Trim(),
                    Idempresa = idEmpresa
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.ToList();
    }

    public async Task<ClienteDto?> GetByIdAsync(int idCliente, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var result = await conn.QueryFirstOrDefaultAsync<ClienteDto>(
            new CommandDefinition(
                "[dbo].[spbuscar_cliente1_id]",
                new
                {
                    Idcliente = idCliente,
                    Idempresa = idEmpresa
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result;
    }

    public async Task CreateAsync(ClienteSaveDto dto, int idEmpresa, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();

        // Parámetro de Salida (El SP devuelve el ID generado)
        p.Add("@Idcliente", dbType: DbType.Int32, direction: ParameterDirection.Output);

        // Parámetros de Entrada (Usamos null de C# + DbType para los que pueden ser nulos)
        p.Add("@Codigo", dto.Codigo, DbType.String); // Si es null, Dapper envía NULL a SQL
        p.Add("@Estado", "A", DbType.String);
        p.Add("@Razon_Social", dto.Razon_Social, DbType.String);
        p.Add("@Nombre_Comercial", dto.Nombre_Comercial, DbType.String);
        p.Add("@Tipo", dto.Tipo, DbType.Int32);
        p.Add("@Tipo_Id", dto.Tipo_Id, DbType.Int32);
        p.Add("@Num_Documento", dto.Num_Documento, DbType.String);
        p.Add("@Fecha_Nacimiento", null, DbType.Date); // No lo tenemos en el DTO por ahora
        p.Add("@Direccion", dto.Direccion, DbType.String);
        p.Add("@Telefono", dto.Telefono, DbType.String);
        p.Add("@Email", dto.Email, DbType.String);
        p.Add("@Tipo_Ingreso", dto.Tipo_Ingreso, DbType.Int32);
        p.Add("@Tax", dto.Tax, DbType.Boolean);
        p.Add("@Tipo_Comprobante", dto.Tipo_Comprobante, DbType.String);
        p.Add("@Retencion_ITBIS", dto.Retencion_ITBIS, DbType.Int32);
        p.Add("@Retencion_ISR", dto.Retencion_ISR, DbType.Int32);
        p.Add("@Limite", dto.Limite, DbType.Decimal);
        p.Add("@Balance", dto.Balance, DbType.Decimal);
        p.Add("@Fecha1", dto.Fecha1, DbType.Date); // Si es null, Dapper envía NULL
        p.Add("@Num_Cuenta", dto.Num_Cuenta, DbType.String);
        p.Add("@Termino", dto.Termino, DbType.Int32);
        p.Add("@Descuento", dto.Descuento, DbType.Decimal);
        p.Add("@Forma_Pago", dto.Forma_Pago, DbType.Int32);
        p.Add("@Departamento", dto.Departamento, DbType.Int32);
        p.Add("@Vendedor", dto.Vendedor, DbType.Int32);
        p.Add("@Lista_Precio", dto.Lista_Precio, DbType.Int32);
        p.Add("@Comentario", dto.Comentario, DbType.String);
        p.Add("@Idprovincia", dto.Idprovincia, DbType.Int32);
        p.Add("@Idmunicipio", dto.Idmunicipio, DbType.Int32);
        p.Add("@Idsector", dto.Idsector, DbType.Int32);
        p.Add("@Ruta", dto.Ruta, DbType.String);
        p.Add("@Idlogin", idLogin, DbType.Int32);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_cliente1]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task UpdateAsync(int idCliente, ClienteSaveDto dto, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();

        p.Add("@Codigo", dto.Codigo, DbType.String); // En edición pasamos el código existente o vacío
        p.Add("@Estado", "A", DbType.String);
        p.Add("@Idcliente", idCliente, DbType.Int32);
        p.Add("@Razon_Social", dto.Razon_Social, DbType.String);
        p.Add("@Nombre_Comercial", dto.Nombre_Comercial, DbType.String);
        p.Add("@Tipo", dto.Tipo, DbType.Int32);
        p.Add("@Tipo_Id", dto.Tipo_Id, DbType.Int32);
        p.Add("@Num_Documento", dto.Num_Documento, DbType.String);
        p.Add("@Fecha_Nacimiento", null, DbType.Date);
        p.Add("@Direccion", dto.Direccion, DbType.String);
        p.Add("@Telefono", dto.Telefono, DbType.String);
        p.Add("@Email", dto.Email, DbType.String);
        p.Add("@Tipo_Ingreso", dto.Tipo_Ingreso, DbType.Int32);
        p.Add("@Tax", dto.Tax, DbType.Boolean);
        p.Add("@Tipo_Comprobante", dto.Tipo_Comprobante, DbType.String);
        p.Add("@Retencion_ITBIS", dto.Retencion_ITBIS, DbType.Int32);
        p.Add("@Retencion_ISR", dto.Retencion_ISR, DbType.Int32);
        p.Add("@Limite", dto.Limite, DbType.Decimal);
        p.Add("@Num_Cuenta", dto.Num_Cuenta, DbType.String);
        p.Add("@Termino", dto.Termino, DbType.Int32);
        p.Add("@Descuento", dto.Descuento, DbType.Decimal);
        p.Add("@Forma_Pago", dto.Forma_Pago, DbType.Int32);
        p.Add("@Departamento", dto.Departamento, DbType.Int32);
        p.Add("@Vendedor", dto.Vendedor, DbType.Int32);
        p.Add("@Lista_Precio", dto.Lista_Precio, DbType.Int32);
        p.Add("@Comentario", dto.Comentario, DbType.String);
        p.Add("@Idprovincia", dto.Idprovincia, DbType.Int32);
        p.Add("@Idmunicipio", dto.Idmunicipio, DbType.Int32);
        p.Add("@Idsector", dto.Idsector, DbType.Int32);
        p.Add("@Ruta", dto.Ruta, DbType.String);
        p.Add("@Idlogin", idLogin, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_cliente1]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int idCliente, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idcliente", idCliente, DbType.Int32);
        p.Add("@Idlogin", idLogin, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_cliente1]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }



    public async Task<IReadOnlyList<TipoComprobanteDto>> GetTiposComprobanteAsync(bool esVenta, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var result = await conn.QueryAsync<TipoComprobanteDto>(
            new CommandDefinition(
                "[dbo].[spcargar_ecf]",
                new { IsVenta = esVenta ? 1 : 0 },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.ToList();
    }

    public async Task<IReadOnlyList<RetencionDto>> GetRetencionesAsync(int idEmpresa, bool tipo, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var result = await conn.QueryAsync<RetencionDto>(
            new CommandDefinition(
                "[dbo].[spcargar_retencion]",
                new { Idempresa = idEmpresa, Tipo = tipo ? 1 : 0 },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.ToList();
    }

    public async Task<IReadOnlyList<CuentaContableDto>> BuscarCuentasContablesAsync(
        string textoBuscar,
        int idEmpresa,
        CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var result = await conn.QueryAsync<CuentaContableDto>(
            new CommandDefinition(
                "[dbo].[spbuscar_cuenta_detalle]",
                new { textobuscar = textoBuscar, Idempresa = idEmpresa },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.ToList();
    }
}