using Dapper;
using EmmaSystem.Application.DTOs.Proveedor;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;
namespace EmmaSystem.Infrastructure.Repositories;
public sealed class ProveedorRepository : IProveedorRepository
{
    private readonly SqlConnectionFactory _factory;
    public ProveedorRepository(SqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<ProveedorDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        var result = await conn.QueryAsync<ProveedorDto>(
            new CommandDefinition("[dbo].[spmostrar_proveedor1]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
        return result.AsList();
    }

    public async Task CreateAsync(ProveedorSaveDto dto, int idEmpresa, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idproveedor", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@Codigo", dto.Codigo, DbType.String);
        p.Add("@Estado", "A", DbType.String);
        p.Add("@Razon_Social", dto.Razon_Social, DbType.String);
        p.Add("@Nombre_Comercial", dto.Nombre_Comercial, DbType.String);
        p.Add("@Tipo", dto.Tipo, DbType.Int32);
        p.Add("@Tipo_Id", dto.Tipo_Id, DbType.Int32);
        p.Add("@Num_Documento", dto.Num_Documento, DbType.String);
        p.Add("@Fecha_Nacimiento", null, DbType.Date);
        p.Add("@Direccion", dto.Direccion, DbType.String);
        p.Add("@Telefono", dto.Telefono, DbType.String);
        p.Add("@Email", dto.Email, DbType.String);
        p.Add("@Tipo_B_S", dto.Tipo_B_S, DbType.Int32);
        p.Add("@Retencion_ITBIS", dto.Retencion_ITBIS, DbType.Int32);
        p.Add("@Retencion_ISR", dto.Retencion_ISR, DbType.Int32);
        p.Add("@Limite", dto.Limite, DbType.Decimal);
        p.Add("@Balance", dto.Balance, DbType.Decimal);
        p.Add("@Fecha1", dto.Fecha1, DbType.Date);
        p.Add("@Num_Cuenta", dto.Num_Cuenta, DbType.String);
        p.Add("@Plazo", dto.Plazo, DbType.Int32);
        p.Add("@Descuento", dto.Descuento, DbType.Decimal);
        p.Add("@Forma_Pago", dto.Forma_Pago, DbType.Int32);
        p.Add("@Comentario", dto.Comentario, DbType.String);
        p.Add("@Idprovincia", dto.Idprovincia, DbType.Int32);
        p.Add("@Idmunicipio", dto.Idmunicipio, DbType.Int32);
        p.Add("@Idsector", dto.Idsector, DbType.Int32);
        p.Add("@Idlogin", idLogin, DbType.Int32);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_proveedor1]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task UpdateAsync(int idProveedor, ProveedorSaveDto dto, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Codigo", dto.Codigo ?? "", DbType.String);
        p.Add("@Estado", "A", DbType.String);
        p.Add("@Idproveedor", idProveedor, DbType.Int32);
        p.Add("@Razon_Social", dto.Razon_Social, DbType.String);
        p.Add("@Nombre_Comercial", dto.Nombre_Comercial, DbType.String);
        p.Add("@Tipo", dto.Tipo, DbType.Int32);
        p.Add("@Tipo_Id", dto.Tipo_Id, DbType.Int32);
        p.Add("@Num_Documento", dto.Num_Documento, DbType.String);
        p.Add("@Fecha_Nacimiento", null, DbType.Date);
        p.Add("@Direccion", dto.Direccion, DbType.String);
        p.Add("@Telefono", dto.Telefono, DbType.String);
        p.Add("@Email", dto.Email, DbType.String);
        p.Add("@Tipo_B_S", dto.Tipo_B_S, DbType.Int32);
        p.Add("@Retencion_ITBIS", dto.Retencion_ITBIS, DbType.Int32);
        p.Add("@Retencion_ISR", dto.Retencion_ISR, DbType.Int32);
        p.Add("@Limite", dto.Limite, DbType.Decimal);
        p.Add("@Num_Cuenta", dto.Num_Cuenta, DbType.String);
        p.Add("@Plazo", dto.Plazo, DbType.Int32);
        p.Add("@Descuento", dto.Descuento, DbType.Decimal);
        p.Add("@Forma_Pago", dto.Forma_Pago, DbType.Int32);
        p.Add("@Comentario", dto.Comentario, DbType.String);
        p.Add("@Idprovincia", dto.Idprovincia, DbType.Int32);
        p.Add("@Idmunicipio", dto.Idmunicipio, DbType.Int32);
        p.Add("@Idsector", dto.Idsector, DbType.Int32);
        p.Add("@Idlogin", idLogin, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_proveedor1]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int idProveedor, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idproveedor", idProveedor, DbType.Int32);
        p.Add("@Idlogin", idLogin, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_proveedor1]", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
}