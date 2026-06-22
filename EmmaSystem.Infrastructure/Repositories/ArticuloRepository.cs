using Dapper;
using EmmaSystem.Application.DTOs.Articulo;
using EmmaSystem.Application.Interfaces;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class ArticuloRepository : IArticuloRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    // ✅ Constante: dentro de cada BD de empresa, IdEmpresa siempre es 1
    private const int EmpresaIdInterna = 1;

    public ArticuloRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<IReadOnlyList<ArticuloDto>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        // ✅ spmostrar_producto REQUIERE @Idempresa
        var p = new DynamicParameters();
        p.Add("@Idempresa", EmpresaIdInterna, DbType.Int32);

        var result = await conn.QueryAsync<ArticuloDto>(
            new CommandDefinition("[dbo].[spmostrar_producto]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();

    }

    public async Task<IReadOnlyList<ArticuloVentaDto>> SearchForSalesAsync(string textoBuscar, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        // ✅ spbuscararticulo_venta1 REQUIERE @Idempresa
        var p = new DynamicParameters();
        p.Add("@textobuscar", textoBuscar, DbType.String);
        p.Add("@Idempresa", EmpresaIdInterna, DbType.Int32);

        var result = await conn.QueryAsync<ArticuloVentaDto>(
            new CommandDefinition("[dbo].[spbuscararticulo_venta1]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();
    }

    public async Task<IReadOnlyList<ArticuloDto>> SearchAsync(string texto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        // ✅ spbuscar_articulo1_codigo REQUIERE @Idempresa
        var p = new DynamicParameters();
        p.Add("@textobuscar", texto, DbType.String);
        p.Add("@Idempresa", EmpresaIdInterna, DbType.Int32);

        var result = await conn.QueryAsync<ArticuloDto>(
            new CommandDefinition("[dbo].[spbuscar_articulo1_codigo]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();
    }

    public async Task<int> CreateAsync(ArticuloSaveDto dto, int idLogin, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        // PASO 1: Insertar Artículo cabecera
        // ✅ spinsertar_articulo1 REQUIERE @Idempresa
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
        pArt.Add("@Idempresa", EmpresaIdInterna, DbType.Int32); // ← AGREGADO
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
        // Convertir base64 a bytes si existe
        byte[]? fotoBytes = null;
        if (!string.IsNullOrWhiteSpace(dto.FotoBase64))
        {
            var base64Data = dto.FotoBase64;
            // Remover prefijo "data:image/...;base64," si existe
            if (base64Data.Contains(","))
                base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
            fotoBytes = Convert.FromBase64String(base64Data);
        }
        pArt.Add("@Foto", fotoBytes, DbType.Binary);
        // ✅ AHORA
        pArt.Add("@Estado", dto.Estado, DbType.String);
        pArt.Add("@Idarticulo", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[spinsertar_articulo1]", pArt,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

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
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return idArticulo;
    }

    public async Task UpdateAsync(int idArticulo, ArticuloSaveDto dto, int idLogin, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        // ✅ speditar_articulo1 NO requiere @Idempresa (confirmado por query SQL)
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
        byte[]? fotoBytes = null;
        if (!string.IsNullOrWhiteSpace(dto.FotoBase64))
        {
            var base64Data = dto.FotoBase64;
            if (base64Data.Contains(","))
                base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
            fotoBytes = Convert.FromBase64String(base64Data);
        }
        p.Add("@Foto", fotoBytes, DbType.Binary);
        p.Add("@Categoria", dto.CategoriaAF, DbType.Int32);
        p.Add("@Idlogin", idLogin, DbType.Int32);
        // ✅ AHORA
        p.Add("@Estado", dto.Estado, DbType.String);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speditar_articulo1]", p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task DeleteAsync(int idArticulo, int idLogin, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        // ✅ speliminar_articulo1 NO requiere @Idempresa (confirmado por query SQL)
        var p = new DynamicParameters();
        p.Add("@Idarticulo", idArticulo, DbType.Int32);
        p.Add("@Idlogin", idLogin, DbType.Int32);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[speliminar_articulo1]", p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task<IReadOnlyList<DetalleProductoPrecioDto>> GetDetallePreciosAsync(
        int idArticulo,
        int idMedida,
        string nombre,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        // ✅ spbuscar_detalle_producto_precio NO aparece en la lista de SPs con @Idempresa
        var p = new DynamicParameters();
        p.Add("@Idarticulo", idArticulo, DbType.Int32);
        p.Add("@Idmedida", idMedida, DbType.Int32);
        p.Add("@Nombre", nombre, DbType.String);

        var result = await conn.QueryAsync<DetalleProductoPrecioDto>(
            new CommandDefinition("[dbo].[spbuscar_detalle_producto_precio]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();
    }
    public async Task<ArticuloDto?> GetByIdAsync(int idArticulo, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        // ❌ QUITAR: await conn.OpenAsync(ct); ← La conexión ya viene abierta

        dynamic? result = await conn.QueryFirstOrDefaultAsync(
            new CommandDefinition("[dbo].[spbuscar_articulo1_id]",
                new { textobuscar = idArticulo },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        if (result == null) return null;

        var dto = new ArticuloDto
        {
            Idarticulo = (int)result.Idarticulo,
            Codigo = (string)result.Codigo,
            Nombre = (string)result.Nombre,
            Descripcion = (string?)result.Descripcion,
            Fecha1 = result.Fecha1 != null ? (DateTime?)result.Fecha1 : null,
            Idcategoria = (int)result.Idcategoria,
            Idmedida = (int)result.Idmedida,
            Costo = (decimal)result.Costo,
            Precio = (decimal)result.Precio,
            Comision = (decimal)result.Comision,
            Tipo = (string)result.Tipo,
            Categoria = result.Categoria != null ? (int?)result.Categoria : null,
            //Balance_Inicial = result.Balance_Inicial != null ? (decimal?)result.Balance_Inicial : null,
            Exist = (decimal)result.Exist,
            Maximo = (int)result.Maximo,
            Minimo = (int)result.Minimo,
            Tax = (decimal)result.Tax,
            IsVencimiento = result.IsVencimiento != null ? (bool)result.IsVencimiento : false,
            Fecha_Vencimiento = result.Fecha_Vencimiento != null ? (DateTime?)result.Fecha_Vencimiento : null,
            Cta_Inventario = result.Cta_Inventario,
            Cta_Costo = result.Cta_Costo,
            Cta_Ingreso = result.Cta_Ingreso,
            Cta_VentaAF = result.Cta_VentaAF,
            Facturar_Sin_Existencia = result.Facturar_Sin_Existencia,
            Estado = result.Estado,
            Existencia = result.Existencia,
        };

        byte[]? fotoBytes = result.Foto as byte[];
        if (fotoBytes != null && fotoBytes.Length > 0)
        {
            dto.FotoBase64 = Convert.ToBase64String(fotoBytes);
        }

        return dto;
    }
    public async Task<int> GetSecuenciaAsync(string tipo, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition("[dbo].[spsecuencia_articulo]",
                new { Tipo = tipo },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result;
    }
}