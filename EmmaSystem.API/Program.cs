    using EmmaSystem.API.Filters;
    using EmmaSystem.API.Middleware;
    using EmmaSystem.Application.Interfaces;
    using EmmaSystem.Infrastructure.Security;
    using EmmaSystem.Application.Services;
    using EmmaSystem.Infrastructure.Data;
    using EmmaSystem.Infrastructure.Repositories;
    using EmmaSystem.Infrastructure.Services;
    using EmmaSystem.Infrastructure.Settings;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;
    using System.Text;

    namespace EmmaSystem.API
    {
        public class Program
        {
            public static void Main(string[] args)
            {
                var builder = WebApplication.CreateBuilder(args);

                // ──────────────────────────────────────────────
                // 1) Configuración de Settings
                // ──────────────────────────────────────────────
                builder.Services.Configure<EmmaSystemSettings>(
                    builder.Configuration.GetSection("EmmaSystem"));

                builder.Services.AddSingleton(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    return config.GetSection("EmmaSystem").Get<EmmaSystemSettings>()
                        ?? throw new InvalidOperationException("No se pudo cargar la configuración de EmmaSystem");
                });

                builder.Services.Configure<EncryptionSettings>(
                    builder.Configuration.GetSection("Encryption"));

                // ──────────────────────────────────────────────
                // 2) Servicios Core (Controllers, Swagger, Cache)
                // ──────────────────────────────────────────────
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddMemoryCache();

                // ──────────────────────────────────────────────
                // 3) Seguridad: Cifrado AES-256 (Singleton)
                // ──────────────────────────────────────────────
                builder.Services.AddSingleton<ICifradoService, AES256CifradoService>();

                // ──────────────────────────────────────────────
                // 4) Sistema Multi-Tenant
                // ──────────────────────────────────────────────
                builder.Services.AddScoped<SqlConnectionFactory>(); // Conexión a EmmaSystemCentral
                builder.Services.AddScoped<ITenantContext, TenantContext>(); // Una instancia por request
                builder.Services.AddSingleton<ITenantConnectionFactory, TenantConnectionFactory>(); // Factoría compartida

            // ──────────────────────────────────────────────
            // 5) Inyección de Repositorios (UN solo registro por tipo)
            // ──────────────────────────────────────────────

            // Autenticación y Permisos (sin interfaz, se inyectan directamente)
            builder.Services.AddScoped<AuthRepository>();
            builder.Services.AddScoped<PermissionRepository>();

            // Repositorios con interfaz (registro único por par)
            builder.Services.AddScoped<IEmpresaRepository, EmpresaRepository>();
            builder.Services.AddScoped<IDepartamentoRepository, DepartamentoRepository>();
            builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
            builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
            builder.Services.AddScoped<IArticuloRepository, ArticuloRepository>();
            builder.Services.AddScoped<IDetalleProductoRepository, DetalleProductoRepository>();
            builder.Services.AddScoped<IMedidaRepository, MedidaRepository>();
            builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
            builder.Services.AddScoped<ITerminoRepository, TerminoRepository>();
            builder.Services.AddScoped<ICotizacionRepository, CotizacionRepository>();
            builder.Services.AddScoped<IVentaRepository, VentaRepository>();
            builder.Services.AddScoped<IUbicacionRepository, UbicacionRepository>();
            builder.Services.AddScoped<IEnCFRepository, EnCFRepository>();
            builder.Services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
            builder.Services.AddScoped<ICursosRepository, CursosRepository>();
            builder.Services.AddScoped<IEstudiantesRepository, EstudiantesRepository>();
            builder.Services.AddScoped<IInscripcionesRepository, InscripcionesRepository>();
            builder.Services.AddScoped<IAsistenciasRepository, AsistenciasRepository>();
            builder.Services.AddScoped<ICobrosRepository, CobrosRepository>();
            // Registrar servicios de facturación electrónica
            builder.Services.AddScoped<IFacturacionElectronicaService, FacturacionElectronicaService>();
            builder.Services.AddScoped<IEcfXmlRepository, EcfXmlRepository>();
            builder.Services.AddScoped<ITokenDgiiService, TokenDgiiService>();
            builder.Services.AddScoped<ILicenciaService, LicenciaService>(); // ← AGREGADO

            // ──────────────────────────────────────────────
            // 6) Servicios de Aplicación
            // ──────────────────────────────────────────────
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IAuthService, AuthService>(); // ← AGREGADO: Registro de IAuthService
            builder.Services.AddScoped<SesionRepository>();
            builder.Services.AddScoped<ISesionService, SesionService>();
            builder.Services.AddScoped<IPermissionService, PermissionService>();
                builder.Services.AddScoped<IDgiiService, DgiiService>();
            // Registrar el servicio de versiones
            builder.Services.AddScoped<IVersionService, VersionService>();
            // ──────────────────────────────────────────────
            // 7) Filtros Globales
            // ──────────────────────────────────────────────
            builder.Services.AddScoped<PermissionAuthorizationFilter>();

                // ──────────────────────────────────────────────
                // 8) Autenticación JWT
                // ──────────────────────────────────────────────
                var jwt = builder.Configuration.GetSection("Jwt");
                var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

                builder.Services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false; // true en producción
                        options.SaveToken = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwt["Issuer"],
                            ValidAudience = jwt["Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ClockSkew = TimeSpan.FromMinutes(1)
                        };
                    });

                builder.Services.AddAuthorization();

                // ──────────────────────────────────────────────
                // 9) Swagger con soporte para JWT Bearer
                // ──────────────────────────────────────────────
                builder.Services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "EmmaSystem Web API",
                        Version = "v1",
                        Description = "ERP contable dominicano — backend REST sobre SQL Server + Dapper."
                    });

                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Ingrese el token JWT. Ejemplo: eyJhbGci..."
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                            },
                            Array.Empty<string>()
                        }
                    });
                });

                // ──────────────────────────────────────────────
                // 10) CORS
                // ──────────────────────────────────────────────
                builder.Services.AddCors(o => o.AddPolicy("ReactDev", p =>
                    p.WithOrigins(
                            "http://localhost:5173",
                            "http://localhost:3000",
                            "https://emmasystem.net")
                     .AllowAnyHeader()
                     .AllowAnyMethod()));

                // ──────────────────────────────────────────────
                // Construir la aplicación
                // ──────────────────────────────────────────────
                var app = builder.Build();

                // ──────────────────────────────────────────────
                // Pipeline de Middleware (EL ORDEN ES CRÍTICO)
                // ──────────────────────────────────────────────

                app.UseSwagger();
                app.UseSwaggerUI();

                // Middleware global para capturar excepciones SQL
                app.UseMiddleware<SqlExceptionMiddleware>();

                app.UseHttpsRedirection();

                // CORS debe ir ANTES de autenticación
                app.UseCors("ReactDev");

                // 1ro: ¿Quién eres? (JWT)
                app.UseAuthentication();

                // 2do: ¿Qué puedes hacer? (Permisos)
                app.UseAuthorization();

                // 3ro: Establecer contexto del tenant (ClienteId, EmpresaId)
                // DEBE ir después de autenticación y autorización
                app.UseTenantMiddleware();

                app.MapControllers();

                // ──────────────────────────────────────────────
                // Health Check: Conexión a BD Central
                // ──────────────────────────────────────────────
                app.MapGet("/api/health/db", async (SqlConnectionFactory factory) =>
                {
                    try
                    {
                        using var connection = factory.CreateConnection();
                        await connection.OpenAsync();

                        using var command = connection.CreateCommand();
                        command.CommandText = "SELECT GETDATE() AS ServerTime, DB_NAME() AS DatabaseName";

                        using var reader = await command.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            return Results.Ok(new
                            {
                                Status = "✅ Conexión exitosa",
                                ServerTime = reader["ServerTime"],
                                DatabaseName = reader["DatabaseName"],
                                Message = "La base de datos central está accesible."
                            });
                        }

                        return Results.Problem("No se pudo leer la respuesta de la base de datos");
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(
                            title: "❌ Error de conexión a SQL Server",
                            detail: ex.Message,
                            statusCode: 500
                        );
                    }
                })
                .WithTags("Health Check");

                app.Run();
            }
        }
    }