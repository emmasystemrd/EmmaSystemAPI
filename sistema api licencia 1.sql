-- ============================================
-- Base de datos: EmmaSystemCentral
-- Descripción: Sistema central SaaS multi-tenant
--              con Login Central Único
-- ============================================

use master
go
--IF DB_ID('EmmaSystemCentral') IS NOT NULL
--BEGIN
--    ALTER DATABASE EmmaSystemCentral SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
--    DROP DATABASE EmmaSystemCentral;
--END
--GO

CREATE DATABASE EmmaSystemCentral;
GO

USE EmmaSystemCentral;
GO

-- ============================================
-- 1. TABLAS CORE
-- ============================================

CREATE TABLE clientes (
    IdCliente INT IDENTITY(1,1) NOT NULL,
    CodigoCliente VARCHAR(20) NOT NULL,
    RazonSocial NVARCHAR(150) NOT NULL,
    RNC VARCHAR(15) NULL,
    CorreoPrincipal NVARCHAR(100) NOT NULL,
    Telefono VARCHAR(20) NULL,
    Estado TINYINT NOT NULL CONSTRAINT DF_clientes_Estado DEFAULT 1,
    FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_clientes_FechaRegistro DEFAULT GETUTCDATE(),
    SaltCifrado VARBINARY(64) NOT NULL,
    CONSTRAINT PK_clientes PRIMARY KEY CLUSTERED (IdCliente),
    CONSTRAINT UQ_clientes_CodigoCliente UNIQUE (CodigoCliente),
    CONSTRAINT UQ_clientes_RNC UNIQUE (RNC),
    CONSTRAINT CK_clientes_Estado CHECK (Estado IN (1, 2, 3))
);

CREATE TABLE planes (
    IdPlan INT IDENTITY(1,1) NOT NULL,
    CodigoPlan VARCHAR(20) NOT NULL,
    NombrePlan NVARCHAR(100) NOT NULL,
    MaxEmpresas INT NOT NULL CONSTRAINT DF_planes_MaxEmpresas DEFAULT 1,
    PrecioMensualDOP DECIMAL(10,2) NOT NULL,
    Estado TINYINT NOT NULL CONSTRAINT DF_planes_Estado DEFAULT 1,
    CONSTRAINT PK_planes PRIMARY KEY CLUSTERED (IdPlan),
    CONSTRAINT UQ_planes_CodigoPlan UNIQUE (CodigoPlan),
    CONSTRAINT CK_planes_Estado CHECK (Estado IN (1, 2)),
    CONSTRAINT CK_planes_MaxEmpresas CHECK (MaxEmpresas > 0),
    CONSTRAINT CK_planes_Precio CHECK (PrecioMensualDOP >= 0)
);

CREATE TABLE licencias (
    IdLicencia INT IDENTITY(1,1) NOT NULL,
    IdCliente INT NOT NULL,
    IdPlan INT NOT NULL,
    EstadoLicencia TINYINT NOT NULL CONSTRAINT DF_licencias_EstadoLicencia DEFAULT 1,
    FechaInicio DATE NOT NULL,
    FechaVencimiento DATE NOT NULL,
    FechaGracia DATE NULL,
    UltimaValidacion DATETIME2 NULL,
    CONSTRAINT PK_licencias PRIMARY KEY CLUSTERED (IdLicencia),
    CONSTRAINT FK_licencias_clientes FOREIGN KEY (IdCliente) REFERENCES clientes(IdCliente) ON DELETE CASCADE,
    CONSTRAINT FK_licencias_planes FOREIGN KEY (IdPlan) REFERENCES planes(IdPlan) ON DELETE NO ACTION,
    CONSTRAINT CK_licencias_EstadoLicencia CHECK (EstadoLicencia IN (1, 2, 3)),
    CONSTRAINT CK_licencias_Fechas CHECK (FechaVencimiento >= FechaInicio)
);

CREATE TABLE empresas_contratadas (
    IdEmpresa INT IDENTITY(1,1) NOT NULL,
    IdCliente INT NOT NULL,
    NombreEmpresa NVARCHAR(150) NOT NULL,
    NombreBD NVARCHAR(100) NOT NULL,
    ServidorBD NVARCHAR(100) NOT NULL,
    CadenaConexionEnc VARBINARY(MAX) NOT NULL,
    VectorIV VARBINARY(16) NOT NULL,
    Estado TINYINT NOT NULL CONSTRAINT DF_empresas_Estado DEFAULT 1,
    EsEmpresaDefault BIT NOT NULL CONSTRAINT DF_empresas_EsEmpresaDefault DEFAULT 0,
    UltimoAcceso DATETIME2 NULL,
    CONSTRAINT PK_empresas_contratadas PRIMARY KEY CLUSTERED (IdEmpresa),
    CONSTRAINT FK_empresas_clientes FOREIGN KEY (IdCliente) REFERENCES clientes(IdCliente) ON DELETE CASCADE,
    CONSTRAINT UQ_empresas_NombreBD UNIQUE (NombreBD),
    CONSTRAINT CK_empresas_Estado CHECK (Estado IN (1, 2, 3))
);

CREATE TABLE usuarios_central (
    IdUsuarioCentral INT IDENTITY(1,1) NOT NULL,
    IdCliente INT NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    NombreCompleto NVARCHAR(150) NOT NULL,
    Telefono VARCHAR(20) NULL,
    EsSuperAdmin BIT NOT NULL CONSTRAINT DF_usuarios_central_EsSuperAdmin DEFAULT 0,
    Estado TINYINT NOT NULL CONSTRAINT DF_usuarios_central_Estado DEFAULT 1,
    FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_usuarios_central_FechaRegistro DEFAULT GETUTCDATE(),
    UltimoAcceso DATETIME2 NULL,
    CONSTRAINT PK_usuarios_central PRIMARY KEY CLUSTERED (IdUsuarioCentral),
    CONSTRAINT FK_usuarios_central_clientes FOREIGN KEY (IdCliente) REFERENCES clientes(IdCliente) ON DELETE CASCADE,
    CONSTRAINT UQ_usuarios_central_Email UNIQUE (Email),
    CONSTRAINT CK_usuarios_central_Estado CHECK (Estado IN (1, 2, 3))
);

CREATE NONCLUSTERED INDEX IX_usuarios_central_Email ON usuarios_central(Email);

-- CORREGIDO: ON DELETE NO ACTION para evitar ciclo de cascada
CREATE TABLE usuario_empresas (
    IdUsuarioCentral INT NOT NULL,
    IdEmpresa INT NOT NULL,
    FechaAsignacion DATETIME2 NOT NULL CONSTRAINT DF_usuario_empresas_Fecha DEFAULT GETUTCDATE(),
    CONSTRAINT PK_usuario_empresas PRIMARY KEY CLUSTERED (IdUsuarioCentral, IdEmpresa),
    CONSTRAINT FK_ue_usuarios FOREIGN KEY (IdUsuarioCentral) 
        REFERENCES usuarios_central(IdUsuarioCentral) ON DELETE CASCADE,
    CONSTRAINT FK_ue_empresas FOREIGN KEY (IdEmpresa) 
        REFERENCES empresas_contratadas(IdEmpresa) ON DELETE NO ACTION
);

CREATE TABLE log_accesos (
    IdLog BIGINT IDENTITY(1,1) NOT NULL,
    IdCliente INT NULL,
    IdEmpresa INT NULL,
    TipoEvento VARCHAR(50) NOT NULL,
    IPOrigen VARCHAR(45) NOT NULL,
    EndpointAccedido NVARCHAR(255) NULL,
    Resultado VARCHAR(50) NOT NULL,
    FechaEvento DATETIME2 NOT NULL CONSTRAINT DF_log_accesos_FechaEvento DEFAULT GETUTCDATE(),
    CONSTRAINT PK_log_accesos PRIMARY KEY CLUSTERED (IdLog),
    CONSTRAINT FK_log_accesos_clientes FOREIGN KEY (IdCliente) REFERENCES clientes(IdCliente) ON DELETE NO ACTION,
    CONSTRAINT FK_log_accesos_empresas FOREIGN KEY (IdEmpresa) REFERENCES empresas_contratadas(IdEmpresa) ON DELETE NO ACTION,
    CONSTRAINT CK_log_accesos_Resultado CHECK (Resultado IN ('Exitoso', 'Fallido', 'Error'))
);
GO

-- ============================================
-- 2. ÍNDICES ADICIONALES
-- ============================================
CREATE NONCLUSTERED INDEX IX_clientes_Estado ON clientes(Estado);
CREATE NONCLUSTERED INDEX IX_clientes_FechaRegistro ON clientes(FechaRegistro);
CREATE NONCLUSTERED INDEX IX_planes_Estado ON planes(Estado);
CREATE NONCLUSTERED INDEX IX_licencias_IdCliente ON licencias(IdCliente);
CREATE NONCLUSTERED INDEX IX_licencias_FechaVencimiento ON licencias(FechaVencimiento);
CREATE NONCLUSTERED INDEX IX_licencias_EstadoLicencia ON licencias(EstadoLicencia);
CREATE NONCLUSTERED INDEX IX_empresas_IdCliente ON empresas_contratadas(IdCliente);
CREATE NONCLUSTERED INDEX IX_empresas_Estado ON empresas_contratadas(Estado);
CREATE NONCLUSTERED INDEX IX_log_accesos_FechaEvento ON log_accesos(FechaEvento DESC);
CREATE NONCLUSTERED INDEX IX_log_accesos_IdCliente_Fecha ON log_accesos(IdCliente, FechaEvento DESC);
CREATE NONCLUSTERED INDEX IX_log_accesos_TipoEvento ON log_accesos(TipoEvento);
GO

-- ============================================
-- 3. DATOS DE PRUEBA (TODO EN UN SOLO LOTE)
-- NO HAY "GO" ENTRE ESTAS SENTENCIAS
-- ============================================

-- Planes
INSERT INTO planes (CodigoPlan, NombrePlan, MaxEmpresas, PrecioMensualDOP, Estado) 
VALUES 
    ('PLAN-BASICO', 'Plan Básico', 2, 500.00, 1),
    ('PLAN-PREMIUM', 'Plan Premium', 5, 1200.00, 1);

DECLARE @IdPlanBasico INT = (SELECT IdPlan FROM planes WHERE CodigoPlan = 'PLAN-BASICO');
DECLARE @IdPlanPremium INT = (SELECT IdPlan FROM planes WHERE CodigoPlan = 'PLAN-PREMIUM');

-- Clientes
INSERT INTO clientes (CodigoCliente, RazonSocial, RNC, CorreoPrincipal, Telefono, Estado, SaltCifrado)
VALUES 
    ('CLI001', 'Distribuidora del Norte SRL', '131-12345-6', 'contacto@norte.com.do', '809-555-0101', 1, CONVERT(VARBINARY(64), HASHBYTES('SHA2_512', 'Salt1'))),
    ('CLI002', 'Farmacia Salud Total SA', '131-67890-1', 'admin@saludtotal.com.do', '809-555-0202', 1, CONVERT(VARBINARY(64), HASHBYTES('SHA2_512', 'Salt2')));

DECLARE @IdCliente1 INT = (SELECT IdCliente FROM clientes WHERE CodigoCliente = 'CLI001');
DECLARE @IdCliente2 INT = (SELECT IdCliente FROM clientes WHERE CodigoCliente = 'CLI002');

-- Licencias
INSERT INTO licencias (IdCliente, IdPlan, EstadoLicencia, FechaInicio, FechaVencimiento, FechaGracia)
VALUES 
    (@IdCliente1, @IdPlanBasico, 1, '2026-01-01', '2026-12-31', '2027-01-15'),
    (@IdCliente2, @IdPlanPremium, 1, '2026-01-01', '2026-12-31', '2027-01-15');

-- Empresas Contratadas
INSERT INTO empresas_contratadas (IdCliente, NombreEmpresa, NombreBD, ServidorBD, CadenaConexionEnc, VectorIV, Estado, EsEmpresaDefault)
VALUES 
    (@IdCliente1, 'Distribuidora del Norte - Sucursal Central', 'EmmaSystem_CLI001_Emp01', 'SQLPROD01', 0x0102030405060708090A0B0C0D0E0F10, 0x0102030405060708090A0B0C0D0E0F10, 1, 1),
    (@IdCliente1, 'Distribuidora del Norte - Sucursal Norte', 'EmmaSystem_CLI001_Emp02', 'SQLPROD01', 0x1112131415161718191A1B1C1D1E1F20, 0x1112131415161718191A1B1C1D1E1F20, 1, 0),
    (@IdCliente2, 'Farmacia Salud Total - Sede Principal', 'EmmaSystem_CLI002_Emp01', 'SQLPROD02', 0x2122232425262728292A2B2C2D2E2F30, 0x2122232425262728292A2B2C2D2E2F30, 1, 1),
    (@IdCliente2, 'Farmacia Salud Total - Sucursal Este', 'EmmaSystem_CLI002_Emp02', 'SQLPROD02', 0x3132333435363738393A3B3C3D3E3F40, 0x3132333435363738393A3B3C3D3E3F40, 1, 0);

DECLARE @IdEmpresa1 INT = (SELECT IdEmpresa FROM empresas_contratadas WHERE NombreBD = 'EmmaSystem_CLI001_Emp01');
DECLARE @IdEmpresa2 INT = (SELECT IdEmpresa FROM empresas_contratadas WHERE NombreBD = 'EmmaSystem_CLI001_Emp02');
DECLARE @IdEmpresa3 INT = (SELECT IdEmpresa FROM empresas_contratadas WHERE NombreBD = 'EmmaSystem_CLI002_Emp01');
DECLARE @IdEmpresa4 INT = (SELECT IdEmpresa FROM empresas_contratadas WHERE NombreBD = 'EmmaSystem_CLI002_Emp02');

-- Usuarios Centrales
-- REEMPLAZAR '$2a$11$HASH_BCRYPT_VENDEDOR' con hash real generado por BCrypt.Net
INSERT INTO usuarios_central (IdCliente, Email, PasswordHash, NombreCompleto, EsSuperAdmin, Estado)
VALUES 
    (@IdCliente1, 'admin@norte.com.do', '$2a$11$SRTaOd5E38oOnLCXlKJQSOYMKlpbHzzJctti3gKWgieFcRHKY.6l2', 'Administrador Norte', 1, 1),
    (@IdCliente2, 'admin@saludtotal.com.do', '$2a$11$SRTaOd5E38oOnLCXlKJQSOYMKlpbHzzJctti3gKWgieFcRHKY.6l2', 'Administrador Salud Total', 1, 1),
    (@IdCliente1, 'vendedor@norte.com.do', '$2a$11$HASH_BCRYPT_VENDEDOR', 'Juan Vendedor', 0, 1);

DECLARE @IdAdminNorte INT = (SELECT IdUsuarioCentral FROM usuarios_central WHERE Email = 'admin@norte.com.do');
DECLARE @IdAdminSalud INT = (SELECT IdUsuarioCentral FROM usuarios_central WHERE Email = 'admin@saludtotal.com.do');
DECLARE @IdVendedor INT = (SELECT IdUsuarioCentral FROM usuarios_central WHERE Email = 'vendedor@norte.com.do');

-- Asignaciones de Empresas a Usuarios
-- Admin Norte: ambas sucursales
INSERT INTO usuario_empresas (IdUsuarioCentral, IdEmpresa) VALUES (@IdAdminNorte, @IdEmpresa1);
INSERT INTO usuario_empresas (IdUsuarioCentral, IdEmpresa) VALUES (@IdAdminNorte, @IdEmpresa2);

-- Admin Salud Total: ambas sucursales
INSERT INTO usuario_empresas (IdUsuarioCentral, IdEmpresa) VALUES (@IdAdminSalud, @IdEmpresa3);
INSERT INTO usuario_empresas (IdUsuarioCentral, IdEmpresa) VALUES (@IdAdminSalud, @IdEmpresa4);

-- Vendedor Juan: solo Sucursal Central
INSERT INTO usuario_empresas (IdUsuarioCentral, IdEmpresa) VALUES (@IdVendedor, @IdEmpresa1);

-- Logs de acceso de ejemplo
INSERT INTO log_accesos (IdCliente, IdEmpresa, TipoEvento, IPOrigen, EndpointAccedido, Resultado)
VALUES 
    (@IdCliente1, @IdEmpresa1, 'LOGIN', '192.168.1.100', '/api/auth/login/central', 'Exitoso'),
    (@IdCliente1, @IdEmpresa1, 'API_CALL', '192.168.1.100', '/api/facturas', 'Exitoso'),
    (@IdCliente2, @IdEmpresa3, 'LOGIN', '192.168.2.50', '/api/auth/login/central', 'Exitoso'),
    (@IdCliente2, @IdEmpresa3, 'API_CALL', '192.168.2.50', '/api/clientes', 'Exitoso'),
    (@IdCliente1, @IdEmpresa2, 'LOGIN', '192.168.1.101', '/api/auth/login/central', 'Fallido');
GO

-- ============================================
-- 4. VERIFICACIÓN FINAL
-- ============================================
SELECT 'clientes' AS Tabla, COUNT(*) AS Total FROM clientes
UNION ALL SELECT 'planes', COUNT(*) FROM planes
UNION ALL SELECT 'licencias', COUNT(*) FROM licencias
UNION ALL SELECT 'empresas_contratadas', COUNT(*) FROM empresas_contratadas
UNION ALL SELECT 'usuarios_central', COUNT(*) FROM usuarios_central
UNION ALL SELECT 'usuario_empresas', COUNT(*) FROM usuario_empresas
UNION ALL SELECT 'log_accesos', COUNT(*) FROM log_accesos;
GO

PRINT 'Script ejecutado exitosamente. EmmaSystemCentral lista con Login Central Único.';


USE EmmaSystemCentral;
GO

-- Agregar columna RncCedula (máximo 11 dígitos, sin guiones)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.empresas_contratadas') AND name = 'RncCedula')
BEGIN
    ALTER TABLE dbo.empresas_contratadas 
    ADD RncCedula VARCHAR(11) NULL;
    
    -- Restricción: solo números, máximo 11 caracteres
    ALTER TABLE dbo.empresas_contratadas 
    ADD CONSTRAINT CK_empresas_RncCedula CHECK (RncCedula IS NULL OR (LEN(RncCedula) <= 11 AND RncCedula NOT LIKE '%[^0-9]%'));
END
GO

-- Agregar columna Ambiente (1=Prueba, 2=Certificación, 3=Producción)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.empresas_contratadas') AND name = 'Ambiente')
BEGIN
    ALTER TABLE dbo.empresas_contratadas 
    ADD Ambiente TINYINT NOT NULL CONSTRAINT DF_empresas_Ambiente DEFAULT 1;
    
    ALTER TABLE dbo.empresas_contratadas 
    ADD CONSTRAINT CK_empresas_Ambiente CHECK (Ambiente IN (1, 2, 3));
END
GO

-- Actualizar datos existentes con valores por defecto para pruebas
UPDATE empresas_contratadas 
SET RncCedula = CASE IdEmpresa
                    WHEN 1 THEN '131123456'
                    WHEN 2 THEN '131123456'
                    WHEN 3 THEN '131678901'
                    WHEN 4 THEN '131678901'
                END,
    Ambiente = 1 -- Por defecto en ambiente de prueba
WHERE RncCedula IS NULL;
GO

-- Verificar cambios
SELECT IdEmpresa, NombreEmpresa, NombreBD, RncCedula, Ambiente, Estado 
FROM empresas_contratadas;
GO

USE EmmaSystemCentral;
SELECT IdCliente, CodigoCliente, CONVERT(VARCHAR(128), SaltCifrado, 1) AS SaltHex
FROM clientes WHERE IdCliente = 2;

USE EmmaSystemCentral;
GO

-- Empresa 1: Sucursal Central
UPDATE empresas_contratadas
SET 
    CadenaConexionEnc = 0x36C4B77E05E52D34E085B2F9EF3751C3B7E01D5205738C583CFEE55BAB8DB81303A62A35A7C2D8FC03657CEEADF50B022F2D20CD8468966A71FCFBFD76658A16746525DD623B958F1075004C68E6B666320EC7556487B8FDE8E42C4EE08A967BC5BD1487E1DFD4AE8E865E1AC4268F1A,  -- Valor real generado
    VectorIV = 0x81CFEAD3E3F26FD1803D5F1E44DDBA24,           -- Valor real generado
    ServidorBD = '.\EmmaSystem',            -- Servidor real
    NombreBD = 'TALLERBARAHONA'    -- BD real que ya existe
WHERE IdEmpresa = 1;

UPDATE empresas_contratadas
SET 
    CadenaConexionEnc = 0xC4587AC7D38390F71DE251E91A322A4EF934C666AE75C4CA661733F99FC5B8933B637FD6C0D75465492DC146F2AE029FCF0B8D60C2CF6ED9BF943E2ABF0D691CB7B0D4BEC50D4E63224A2FFFCF12C7A7FAE7F89729AE96E8F2788F7801AA2A191A7396685D5F10477DFBA081AD645EC9,  -- Valor real generado
    VectorIV = 0x82E3D16C629635A9441C93DEF605AA7E,           -- Valor real generado
    ServidorBD = '.\EmmaSystem',            -- Servidor real
    NombreBD = 'EMMANUEL'    -- BD real que ya existe
WHERE IdEmpresa = 2;

UPDATE empresas_contratadas
SET 
    CadenaConexionEnc = 0xA8BC96BD2F6B9DCCDCE339A8D71740E030BB07CDC907D9AF91BBECB9225A631691B53352063478E4D1D2D06A63C310F4DE812B166C62F7D3FFD98ABC21C3E8074F5A23456D6554EDF0E6DC481525797A0603CF55D95BF86C9B7E69B2C37FB9652433CC890D4535E500981FEB420E1AB3,  -- Valor real generado
    VectorIV = 0xB6F19A756F0717ADC3B8DC917305B608,           -- Valor real generado
    ServidorBD = '.\EmmaSystem',            -- Servidor real
    NombreBD = 'SMARTBYTES'    -- BD real que ya existe
WHERE IdEmpresa = 3;

UPDATE empresas_contratadas
SET 
    CadenaConexionEnc = 0x71C5AB5182B7956489C7BA1D0A8EF2E09B3D94364C3613D22985899FFD40C2EEA9EE4286933BD1D4625367996F5D1DB61D699B2859A7F75FB46C7948C7C19792F1587D3095A164F9A58AAE3BD091827845DDCB3B811776FC0B27110B163DC25DCDAE5424F2B7E7BF97348C91AC97ACA5F95B26C4387466975A8EB6E3C2880291,  -- Valor real generado
    VectorIV = 0xD39AFFFC92AB5F9132A964FBD71D5B8A,           -- Valor real generado
    ServidorBD = '.\EmmaSystem',            -- Servidor real
    NombreBD = 'DISTRIBUIDORAINNOVA'    -- BD real que ya existe
WHERE IdEmpresa = 4;
-- Repetir para cada empresa con su propia cadena
GO

USE EmmaSystemCentral;
GO

SELECT 
    ec.IdEmpresa,
    ec.NombreEmpresa,
    ec.NombreBD,
    CONVERT(VARCHAR(128), ec.CadenaConexionEnc, 1) AS CadenaEncHex,
    CONVERT(VARCHAR(32), ec.VectorIV, 1) AS VectorIVHex,
    CONVERT(VARCHAR(128), c.SaltCifrado, 1) AS SaltHex,
    LEN(c.SaltCifrado) AS SaltLength
FROM empresas_contratadas ec
INNER JOIN clientes c ON ec.IdCliente = c.IdCliente
WHERE ec.IdEmpresa = 1;



USE EmmaSystemCentral;
SELECT 
    ec.IdEmpresa,
    ec.NombreBD,
    LEN(ec.VectorIV) AS IV_Length,        -- Debe ser 16
    LEN(ec.CadenaConexionEnc) AS Enc_Length,
    CONVERT(VARCHAR(128), c.SaltCifrado, 1) AS SaltHex,
    LEN(c.SaltCifrado) AS Salt_Length      -- Debe ser 64
FROM empresas_contratadas ec
INNER JOIN clientes c ON ec.IdCliente = c.IdCliente
WHERE ec.IdEmpresa = 1;

USE EmmaSystemCentral;
SELECT 
    IdCliente,
    CodigoCliente,
    DATALENGTH(SaltCifrado) AS SaltBytes,  -- Debe ser EXACTAMENTE 64
    CONVERT(VARCHAR(130), SaltCifrado, 1) AS SaltHex
FROM clientes 
WHERE IdCliente = 1;

USE EmmaSystemCentral;
GO

USE EmmaSystemCentral;
GO

-- Reescribir completamente el salt (sin el byte 0x25 extra)
UPDATE clientes
SET SaltCifrado = 0x8DFEFF10EF735A5ED994FD5848C7A572E5A676E0E7CCC3F8D5973FECF1C899C5EDF1EDCF2018CFF63AFF524D6D7A9D3D308471476FB4C3AC03B0036F90592A
WHERE IdCliente = 1;

-- Verificación CRÍTICA: debe terminar en 2A, NO en 25
SELECT 
    DATALENGTH(SaltCifrado) AS Bytes,
    RIGHT(CONVERT(VARCHAR(130), SaltCifrado, 1), 4) AS Ultimos2Bytes
FROM clientes 
WHERE IdCliente = 1;
GO

USE EmmaSystemCentral;
SELECT IdCliente FROM empresas_contratadas WHERE IdEmpresa = 1 AND Estado = 1;

BACKUP DATABASE [EmmaSystem_Template] 
TO DISK = N'C:\EmmaSystem\EmmaSystem_Template.bak' 
WITH INIT, FORMAT, COMPRESSION;


BACKUP DATABASE EmmaSystem2026 
TO DISK = N'C:\EmmaSystem\EmmaSystem2026.bak' 
WITH INIT, FORMAT, COMPRESSION;


USE EmmaSystemCentral;
GO

-- Agregar columna para clave secreta de validación offline
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.clientes') AND name = 'SecretKey')
BEGIN
    ALTER TABLE dbo.clientes
    ADD SecretKey VARBINARY(256) NULL;
    
    PRINT '✅ Columna SecretKey agregada exitosamente.';
END
ELSE
BEGIN
    PRINT 'ℹ️ La columna SecretKey ya existe.';
END
GO

-- Verificar que se agregó correctamente
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'clientes' AND COLUMN_NAME = 'SecretKey';
GO

-- ============================================================================
-- FASE 1: MODIFICACIONES A LA BASE DE DATOS
-- ============================================================================

USE EmmaSystemCentral;
GO

-- ============================================================================
-- 1.1 AGREGAR MaxConcurrentes A LA TABLA planes
-- ============================================================================
PRINT '=== 1.1 Agregando MaxConcurrentes a planes ===';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'planes' AND COLUMN_NAME = 'MaxConcurrentes')
BEGIN
    ALTER TABLE planes ADD MaxConcurrentes INT NOT NULL DEFAULT 1;
    PRINT '✅ Columna MaxConcurrentes agregada';
END
ELSE
BEGIN
    PRINT 'ℹ️ Columna MaxConcurrentes ya existe';
END
go
-- Actualizar valores de planes existentes
UPDATE planes SET MaxConcurrentes = 2 WHERE IdPlan = 1; -- Plan Básico
UPDATE planes SET MaxConcurrentes = 5 WHERE IdPlan = 2; -- Plan Premium
PRINT '✅ Valores de MaxConcurrentes actualizados';
GO

-- ============================================================================
-- 1.2 CREAR TABLA sesiones_activas
-- ============================================================================
PRINT '=== 1.2 Creando tabla sesiones_activas ===';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
               WHERE TABLE_NAME = 'sesiones_activas')
BEGIN
    CREATE TABLE sesiones_activas (
        IdSesion BIGINT PRIMARY KEY IDENTITY(1,1),
        IdUsuarioCentral INT NOT NULL,
        IdEmpresa INT NULL,
        Token NVARCHAR(500) NOT NULL,
        IPAddress VARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        FechaInicio DATETIME2 NOT NULL DEFAULT GETDATE(),
        UltimoActividad DATETIME2 NOT NULL DEFAULT GETDATE(),
        Estado BIT NOT NULL DEFAULT 1,
        
        CONSTRAINT FK_sesiones_usuario FOREIGN KEY (IdUsuarioCentral) 
            REFERENCES usuarios_central(IdUsuarioCentral),
        CONSTRAINT FK_sesiones_empresa FOREIGN KEY (IdEmpresa) 
            REFERENCES empresas_contratadas(IdEmpresa)
    );
    
    -- Índices para mejor performance
    CREATE INDEX IX_sesiones_usuario_activo ON sesiones_activas(IdUsuarioCentral, Estado);
    CREATE INDEX IX_sesiones_token ON sesiones_activas(Token);
    CREATE INDEX IX_sesiones_actividad ON sesiones_activas(UltimoActividad, Estado);
    
    PRINT '✅ Tabla sesiones_activas creada';
END
ELSE
BEGIN
    PRINT 'ℹ️ Tabla sesiones_activas ya existe';
END
GO

-- ============================================================================
-- 1.3 CREAR TABLA empresas_eliminadas (historial)
-- ============================================================================
PRINT '=== 1.3 Creando tabla empresas_eliminadas ===';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
               WHERE TABLE_NAME = 'empresas_eliminadas')
BEGIN
    CREATE TABLE empresas_eliminadas (
        IdHistorial BIGINT PRIMARY KEY IDENTITY(1,1),
        IdEmpresa INT NOT NULL,
        IdCliente INT NOT NULL,
        NombreEmpresa NVARCHAR(200) NOT NULL,
        NombreBD NVARCHAR(100) NOT NULL,
        ServidorBD NVARCHAR(100) NOT NULL,
        FechaEliminacion DATETIME2 NOT NULL DEFAULT GETDATE(),
        EliminadoPor INT NOT NULL,
        Motivo NVARCHAR(500) NULL,
        
        CONSTRAINT FK_empresas_eliminadas_cliente FOREIGN KEY (IdCliente) 
            REFERENCES clientes(IdCliente),
        CONSTRAINT FK_empresas_eliminadas_usuario FOREIGN KEY (EliminadoPor) 
            REFERENCES usuarios_central(IdUsuarioCentral)
    );
    
    PRINT '✅ Tabla empresas_eliminadas creada';
END
ELSE
BEGIN
    PRINT 'ℹ️ Tabla empresas_eliminadas ya existe';
END
GO

-- ============================================================================
-- 1.4 VERIFICAR ESTADO FINAL
-- ============================================================================
PRINT '=== 1.4 Verificación final ===';

SELECT 'planes' AS Tabla, COUNT(*) AS TotalRegistros FROM planes
UNION ALL
SELECT 'sesiones_activas', COUNT(*) FROM sesiones_activas
UNION ALL
SELECT 'empresas_eliminadas', COUNT(*) FROM empresas_eliminadas;

PRINT '✅ FASE 1 COMPLETADA';
GO

-- ============================================================================
-- CORRECCIÓN: Agregar columna IdCliente a tabla sesiones_activas
-- ============================================================================

USE EmmaSystemCentral;
GO

-- 1. Verificar si la columna IdCliente existe
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'sesiones_activas' 
    AND COLUMN_NAME = 'IdCliente'
)
BEGIN
    -- Agregar la columna IdCliente
    ALTER TABLE sesiones_activas 
    ADD IdCliente INT NULL;
    
    PRINT '✅ Columna IdCliente agregada a sesiones_activas';
    
    -- Actualizar los registros existentes con el IdCliente correspondiente
    UPDATE sesiones_activas 
    SET IdCliente = uc.IdCliente
    FROM sesiones_activas sa
    INNER JOIN usuarios_central uc ON sa.IdUsuarioCentral = uc.IdUsuarioCentral
    WHERE sa.IdCliente IS NULL;
    
    PRINT '✅ Registros existentes actualizados con IdCliente';
    
    -- Hacer la columna NOT NULL después de actualizar los datos
    ALTER TABLE sesiones_activas 
    ALTER COLUMN IdCliente INT NOT NULL;
    
    PRINT '✅ Columna IdCliente ahora es NOT NULL';
END
ELSE
BEGIN
    PRINT 'ℹ️ Columna IdCliente ya existe en sesiones_activas';
END
GO

-- 2. Verificar la estructura final
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'sesiones_activas'
ORDER BY ORDINAL_POSITION;
GO

-- 3. Verificar que los datos estén correctos
SELECT TOP 5
    sa.IdSesion,
    sa.IdUsuarioCentral,
    sa.IdCliente,
    uc.Email,
    sa.IdEmpresa,
    sa.Token,
    sa.Estado
FROM sesiones_activas sa
INNER JOIN usuarios_central uc ON sa.IdUsuarioCentral = uc.IdUsuarioCentral
ORDER BY sa.IdSesion DESC;
GO

PRINT '✅ Corrección completada';



-- ============================================================================
-- CORRECCIÓN: Aumentar tamaño de columna Token en sesiones_activas
-- ============================================================================

USE EmmaSystemCentral;
GO

-- 1. Verificar tamaño actual de la columna Token
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'sesiones_activas' 
AND COLUMN_NAME = 'Token';
GO

-- 2. Aumentar el tamaño de la columna Token a 2000 caracteres
ALTER TABLE sesiones_activas
ALTER COLUMN Token NVARCHAR(2000) NOT NULL;

PRINT '✅ Columna Token aumentada a NVARCHAR(2000)';
GO

-- 3. Verificar la estructura final
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'sesiones_activas'
ORDER BY ORDINAL_POSITION;
GO

PRINT '✅ Corrección completada. Ahora puedes intentar iniciar sesión nuevamente.';





-- ============================================================================
-- CORRECCIÓN: Modificar restricción CHECK para permitir Estado = 0
-- ============================================================================

USE EmmaSystemCentral;
GO

-- 1. Verificar la restricción actual
SELECT 
    name,
    definition
FROM sys.check_constraints
WHERE name = 'CK_empresas_contratadas_Estado';
GO

-- 2. Eliminar la restricción antigua
IF EXISTS (
    SELECT 1 
    FROM sys.check_constraints 
    WHERE name = 'CK_empresas_contratadas_Estado'
)
BEGIN
    ALTER TABLE empresas_contratadas 
    DROP CONSTRAINT CK_empresas_contratadas_Estado;
    
    PRINT '✅ Restricción antigua eliminada';
END
GO

-- 3. Crear nueva restricción que permita 0 y 1
ALTER TABLE empresas_contratadas
ADD CONSTRAINT CK_empresas_contratadas_Estado 
CHECK (Estado IN (0, 1));

PRINT '✅ Nueva restricción creada (permite 0 y 1)';
GO

-- 4. Verificar la nueva restricción
SELECT 
    name,
    definition
FROM sys.check_constraints
WHERE name = 'CK_empresas_contratadas_Estado';
GO

-- 5. Verificar valores actuales de Estado
SELECT 
    IdEmpresa,
    NombreEmpresa,
    Estado
FROM empresas_contratadas
WHERE IdCliente = 1
ORDER BY IdEmpresa;
GO

PRINT '✅ Corrección completada. Ahora puedes eliminar empresas.';