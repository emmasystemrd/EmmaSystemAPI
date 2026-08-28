using Dapper;
using EmmaSystem.Application.DTOs.ECF;
using EmmaSystem.Application.Interfaces;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmmaSystem.Infrastructure.Repositories
{
    public class EcfRecibidoRepository : IEcfRecibidoRepository
    {
        private readonly string _connectionString;

        // Recibe la cadena de conexión a DGII_Recepcion directamente
        public EcfRecibidoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<EcfRecibidoDto>> ObtenerPorRncCompradorAsync(
            string rncComprador, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT 
                    Id, Clave, TipoDocumento, RncEmisor, RncComprador, 
                    eNCF AS ENCF, TipoECF, FechaEmision, MontoTotal, TotalItbis,
                    Estado, CodigoMotivoRechazo, MotivoRechazo, XmlRecibido, FechaRecepcion
                FROM RecepcionXml
                WHERE RncComprador = @RncComprador
                ORDER BY FechaRecepcion DESC";

            using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<EcfRecibidoDto>(
                sql, new { RncComprador = rncComprador });

            return result.ToList();
        }

        public async Task<EcfRecibidoDto?> ObtenerPorIdAsync(long id, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT 
                    Id, Clave, TipoDocumento, RncEmisor, RncComprador, 
                    eNCF AS ENCF, TipoECF, FechaEmision, MontoTotal, TotalItbis,
                    Estado, CodigoMotivoRechazo, MotivoRechazo, XmlRecibido, FechaRecepcion
                FROM RecepcionXml
                WHERE Id = @Id";

            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<EcfRecibidoDto>(
                sql, new { Id = id });
        }
    }
}