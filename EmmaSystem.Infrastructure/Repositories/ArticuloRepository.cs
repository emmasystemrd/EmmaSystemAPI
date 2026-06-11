using Dapper;
using EmmaSystem.Application.DTOs.Articulo;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class ArticuloRepository : IArticuloRepository
{
    private readonly SqlConnectionFactory _factory;
    public ArticuloRepository(SqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<ArticuloDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        // Usa el SP existente que ya incluye existencia calculada
        var result = await conn.QueryAsync<ArticuloDto>(
            new CommandDefinition("[dbo].[spmostrar_producto]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
        return result.AsList();
    }

    // ... código existente ...

    public async Task<IReadOnlyList<ArticuloVentaDto>> SearchForSalesAsync(string textoBuscar, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@textobuscar", textoBuscar, DbType.String);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        var result = await conn.QueryAsync<ArticuloVentaDto>(
            new CommandDefinition(
                "[dbo].[spbuscararticulo_venta1]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();
    }

    public async Task<IReadOnlyList<ArticuloDto>> SearchAsync(string texto, int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@textobuscar", texto, DbType.String);
        p.Add("@Idempresa", idEmpresa, DbType.Int32);

        var result = await conn.QueryAsync<ArticuloDto>(
            new CommandDefinition("[dbo].[spbuscar_articulo1_codigo]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
        return result.AsList();
    }

    public async Task<int> CreateAsync(ArticuloSaveDto dto, int idEmpresa, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        // PASO 1: Insertar Artículo cabecera
        var pArt = new DynamicParameters();
        pArt.Add("@Codigo", dto.Codigo, DbType.String);
        pArt.Add("@Nombre", dto.Nombre, DbType.String);
        pArt.Add("@Descripcion", dto.Descripcion, DbType.String);
        pArt.Add("@Fecha1", dto.Fecha1, DbType.Date);
        pArt.Add("@Idcategoria", dto.Idcategoria, DbType.Int32);
        pArt.Add("@Idmedida", dto.Idmedida, DbType.Int32);
        pArt.Add("@Costo", dto.Costo, DbType.Decimal);
        pArt.Add("@Precio", dto.Precio, DbType.Decimal);
        pArt.Add("@Comision", dto.Comision, DbType.Decimal);
        pArt.Add("@Balance_Inicial", dto.Balance_Inicial, DbType.Decimal);
        pArt.Add("@Tipo", dto.Tipo, DbType.String);
        pArt.Add("@Categoria", dto.CategoriaAF, DbType.Int32);
        pArt.Add("@Idlogin", idLogin, DbType.Int32);
        pArt.Add("@Maximo", dto.Maximo, DbType.Int32);
        pArt.Add("@Minimo", dto.Minimo, DbType.Int32);
        pArt.Add("@Tax", dto.Tax, DbType.Decimal);
        pArt.Add("@Isvencimiento", dto.IsVencimiento, DbType.Boolean);
        pArt.Add("@Fecha_Vencimiento", dto.Fecha_Vencimiento, DbType.Date);
        pArt.Add("@Cta_Inventario", dto.Cta_Inventario, DbType.String);
        pArt.Add("@Cta_Costo", dto.Cta_Costo, DbType.String);
        pArt.Add("@Cta_Ingreso", dto.Cta_Ingreso, DbType.String);
        pArt.Add("@Cta_Venta", dto.Cta_VentaAF, DbType.String);
        pArt.Add("@Facturar_Sin_Existencia", dto.Facturar_Sin_Existencia, DbType.String);
        pArt.Add("@Foto", null, DbType.Binary); // Manejo de imagen pendiente
        pArt.Add("@Idempresa", idEmpresa, DbType.Int32);
        pArt.Add("@Estado", "A", DbType.String);
        pArt.Add("@Idarticulo", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_articulo1]", pArt,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        int idArticulo = pArt.Get<int>("@Idarticulo");

        // PASO 2: Insertar presentación principal en Detalle_Producto
        var pDet = new DynamicParameters();
        pDet.Add("@Idarticulo", idArticulo, DbType.Int32);
        pDet.Add("@Codigo_Barra", dto.Codigo_Barra, DbType.String);
        pDet.Add("@Nombre", dto.Nombre, DbType.String);
        pDet.Add("@Idmedida", dto.Idmedida, DbType.Int32);
        pDet.Add("@Unidades", dto.Unidades, DbType.Decimal);
        pDet.Add("@Costo", dto.Costo, DbType.Decimal);
        pDet.Add("@Margen", dto.Precio > 0 && dto.Costo > 0
            ? (dto.Precio - dto.Costo) / dto.Costo : 0m, DbType.Decimal);
        pDet.Add("@Precio", dto.Precio, DbType.Decimal);
        pDet.Add("@Existencia", dto.Balance_Inicial / (dto.Unidades > 0 ? dto.Unidades : 1), DbType.Decimal);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_detalle_producto]", pDet,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return idArticulo;
    }

    public async Task UpdateAsync(int idArticulo, ArticuloSaveDto dto, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idarticulo", idArticulo, DbType.Int32);
        p.Add("@Codigo", dto.Codigo, DbType.String);
        p.Add("@Nombre", dto.Nombre, DbType.String);
        p.Add("@Fecha1", dto.Fecha1, DbType.Date);
        p.Add("@Descripcion", dto.Descripcion, DbType.String);
        p.Add("@Tipo", dto.Tipo, DbType.String);
        p.Add("@Idcategoria", dto.Idcategoria, DbType.Int32);
        p.Add("@Idmedida", dto.Idmedida, DbType.Int32);
        p.Add("@Costo", dto.Costo, DbType.Decimal);
        p.Add("@Precio", dto.Precio, DbType.Decimal);
        p.Add("@Comision", dto.Comision, DbType.Decimal);
        p.Add("@Maximo", dto.Maximo, DbType.Int32);
        p.Add("@Minimo", dto.Minimo, DbType.Int32);
        p.Add("@Tax", dto.Tax, DbType.Decimal);
        p.Add("@IsVencimiento", dto.IsVencimiento, DbType.Boolean);
        p.Add("@Fecha_Vencimiento", dto.Fecha_Vencimiento, DbType.Date);
        p.Add("@Cta_Inventario", dto.Cta_Inventario, DbType.String);
        p.Add("@Cta_Costo", dto.Cta_Costo, DbType.String);
        p.Add("@Cta_Ingreso", dto.Cta_Ingreso, DbType.String);
        p.Add("@Cta_Venta", dto.Cta_VentaAF, DbType.String);
        p.Add("@Facturar_Sin_Existencia", dto.Facturar_Sin_Existencia, DbType.String);
        p.Add("@Foto", null, DbType.Binary);
        p.Add("@Categoria", dto.CategoriaAF, DbType.Int32);
        p.Add("@Idlogin", idLogin, DbType.Int32);
        p.Add("@Estado", "A", DbType.String);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_articulo1]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task DeleteAsync(int idArticulo, int idLogin, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Idarticulo", idArticulo, DbType.Int32);
        p.Add("@Idlogin", idLogin, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_articulo1]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
    // ... código existente ...

    public async Task<IReadOnlyList<DetalleProductoPrecioDto>> GetDetallePreciosAsync(
        int idArticulo,
        int idMedida,
        string nombre,
        CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@Idarticulo", idArticulo, DbType.Int32);
        p.Add("@Idmedida", idMedida, DbType.Int32);
        p.Add("@Nombre", nombre, DbType.String);

        var result = await conn.QueryAsync<DetalleProductoPrecioDto>(
            new CommandDefinition(
                "[dbo].[spbuscar_detalle_producto_precio]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();
    }


}