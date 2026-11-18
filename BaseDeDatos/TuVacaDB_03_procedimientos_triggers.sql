/* ============================================================
   PROYECTO: TU VACA
   Script 03 - Procedimientos almacenados y triggers

   CÓMO USARLO
   - Ejecútalo DESPUÉS de TuVacaDB_corregido.sql y de
     TuVacaDB_02_notificaciones.sql, sobre TuVacaDB.
   - No borra datos. Se puede ejecutar varias veces
     (usa CREATE OR ALTER).

   CONTENIDO
   Procedimientos almacenados (3)
     SP1 sp_BuscarPublicaciones   -> CU-05 (filtros combinables y orden)
     SP2 sp_ReporteProduccion     -> CU-11 RN-6 (total y promedio por vaca;
                                     lo usa el reporte PDF/Excel)
     SP3 sp_HistorialSanitario    -> CU-08 (historial ordenado y con estado
                                     de la próxima dosis; lo usa el reporte PDF)
   Triggers (3)
     TR1 trg_Vacas_Auditoria                -> RN-5-CU12 (trazabilidad de ediciones)
     TR2 trg_Vacas_CancelarPublicaciones    -> RN-4-CU13 y RN-4-CU14 (si la vaca
                                               deja de estar Viva, se cancelan sus
                                               publicaciones)
     TR3 trg_RegistrosSanitarios_NoEliminar -> RN-5-CU07 / RN-3-CU21 (un registro
                                               sanitario no se borra, se anula)
   ============================================================ */

IF DB_ID('TuVacaDB') IS NULL
BEGIN
    RAISERROR(N'La base TuVacaDB no existe. Ejecuta primero TuVacaDB_corregido.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

USE TuVacaDB;
GO

/* ============================================================
   TABLA DE APOYO: AuditoriaVacas (la llena el TR1)
   ============================================================ */
IF OBJECT_ID('dbo.AuditoriaVacas') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaVacas (
        AuditoriaVacaID   INT IDENTITY(1,1) PRIMARY KEY,
        VacaID            INT NOT NULL,
        Campo             NVARCHAR(30) NOT NULL,
        ValorAnterior     NVARCHAR(300) NULL,
        ValorNuevo        NVARCHAR(300) NULL,
        FechaCambio       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_AuditoriaVacas_Vacas FOREIGN KEY (VacaID) REFERENCES dbo.Vacas(VacaID)
    );
    CREATE INDEX IX_AuditoriaVacas_VacaID ON dbo.AuditoriaVacas(VacaID);
END
GO

/* ============================================================
   SP1. sp_BuscarPublicaciones  (CU-05)
   Todos los parámetros son opcionales (NULL = sin ese filtro).
   RN-1: solo publicaciones Activas (20). RN-3: filtros combinables.
   RN-4: @Orden = 'recientes' | 'precio_asc' | 'precio_desc'.
   Ejemplo:
     EXEC dbo.sp_BuscarPublicaciones @Raza = N'Holstein', @PrecioMax = 5000000;
   ============================================================ */
CREATE OR ALTER PROCEDURE dbo.sp_BuscarPublicaciones
    @Texto          NVARCHAR(100) = NULL,
    @DepartamentoID INT           = NULL,
    @MunicipioID    INT           = NULL,
    @PrecioMin      DECIMAL(12,2) = NULL,
    @PrecioMax      DECIMAL(12,2) = NULL,
    @Raza           NVARCHAR(80)  = NULL,
    @EdadMinAnios   INT           = NULL,
    @EdadMaxAnios   INT           = NULL,
    @Orden          VARCHAR(20)   = 'recientes'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Hoy DATE = CAST(SYSDATETIME() AS DATE);

    SELECT  p.PublicacionVacaID,
            p.Precio,
            p.FotoUrl,
            v.Identificacion,
            v.Raza,
            v.Sexo,
            v.FechaNacimiento,
            DATEDIFF(MONTH, v.FechaNacimiento, @Hoy) / 12 AS EdadAnios,
            v.Peso,
            m.Nombre + N', ' + d.Nombre                  AS Ubicacion,
            u.NombreCompleto                              AS Vendedor,
            p.FechaPublicacion
    FROM dbo.PublicacionesVaca p
    JOIN dbo.Vacas         v ON v.VacaID         = p.VacaID
    JOIN dbo.Usuarios      u ON u.UsuarioID      = v.UsuarioID
    JOIN dbo.Municipios    m ON m.MunicipioID    = p.MunicipioID
    JOIN dbo.Departamentos d ON d.DepartamentoID = m.DepartamentoID
    WHERE p.EstadoID = 20
      AND (@Texto IS NULL OR p.Descripcion LIKE N'%' + @Texto + N'%'
                          OR v.Raza LIKE N'%' + @Texto + N'%'
                          OR v.Identificacion LIKE N'%' + @Texto + N'%'
                          OR m.Nombre LIKE N'%' + @Texto + N'%')
      AND (@MunicipioID    IS NULL OR p.MunicipioID    = @MunicipioID)
      AND (@DepartamentoID IS NULL OR m.DepartamentoID = @DepartamentoID)
      AND (@PrecioMin      IS NULL OR p.Precio        >= @PrecioMin)
      AND (@PrecioMax      IS NULL OR p.Precio        <= @PrecioMax)
      AND (@Raza           IS NULL OR v.Raza           = @Raza)
      AND (@EdadMinAnios   IS NULL OR v.FechaNacimiento <= DATEADD(YEAR, -@EdadMinAnios, @Hoy))
      AND (@EdadMaxAnios   IS NULL OR v.FechaNacimiento >  DATEADD(YEAR, -(@EdadMaxAnios + 1), @Hoy))
    ORDER BY
        CASE WHEN @Orden = 'precio_asc'  THEN p.Precio END ASC,
        CASE WHEN @Orden = 'precio_desc' THEN p.Precio END DESC,
        p.FechaPublicacion DESC;
END
GO

/* ============================================================
   SP2. sp_ReporteProduccion  (CU-11 RN-6)
   Producción de leche de cada vaca hembra de un productor en un
   rango de fechas: días registrados, total, promedio, mínimo y máximo.
   Ejemplo:
     EXEC dbo.sp_ReporteProduccion @UsuarioID = 1, @Desde = '2026-10-01', @Hasta = '2026-11-15';
   ============================================================ */
CREATE OR ALTER PROCEDURE dbo.sp_ReporteProduccion
    @UsuarioID INT,
    @Desde     DATE,
    @Hasta     DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @Desde > @Hasta
    BEGIN
        DECLARE @tmp DATE = @Desde;
        SET @Desde = @Hasta;
        SET @Hasta = @tmp;
    END

    SELECT  v.VacaID,
            v.Identificacion,
            v.Raza,
            e.Nombre                                        AS Estado,
            COUNT(pl.ProduccionID)                          AS DiasRegistrados,
            CAST(ISNULL(SUM(pl.CantidadLitros), 0) AS DECIMAL(12,2)) AS TotalLitros,
            CAST(ISNULL(AVG(pl.CantidadLitros), 0) AS DECIMAL(12,2)) AS PromedioLitros,
            CAST(ISNULL(MIN(pl.CantidadLitros), 0) AS DECIMAL(12,2)) AS MinimoLitros,
            CAST(ISNULL(MAX(pl.CantidadLitros), 0) AS DECIMAL(12,2)) AS MaximoLitros
    FROM dbo.Vacas v
    JOIN dbo.Estados e ON e.EstadoID = v.EstadoID
    LEFT JOIN dbo.ProduccionLeche pl
           ON pl.VacaID = v.VacaID
          AND pl.Fecha BETWEEN @Desde AND @Hasta
    WHERE v.UsuarioID = @UsuarioID
      AND v.Sexo = N'Hembra'
    GROUP BY v.VacaID, v.Identificacion, v.Raza, e.Nombre
    ORDER BY TotalLitros DESC, v.Identificacion;
END
GO

/* ============================================================
   SP3. sp_HistorialSanitario  (CU-08)
   Historial de vacunas y tratamientos de una vaca, del más reciente
   al más antiguo (RN-2). Solo responde si la vaca es del usuario
   (RN-1). Marca la próxima dosis como Pendiente o Vencida (RN-4).
   Ejemplo:
     EXEC dbo.sp_HistorialSanitario @VacaID = 3, @UsuarioID = 1;
   ============================================================ */
CREATE OR ALTER PROCEDURE dbo.sp_HistorialSanitario
    @VacaID    INT,
    @UsuarioID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Hoy DATE = CAST(SYSDATETIME() AS DATE);

    SELECT  r.RegistroID,
            t.Nombre          AS Tipo,
            r.Nombre,
            r.FechaAplicacion,
            r.Responsable,
            r.Observaciones,
            r.ProximaDosis,
            e.Nombre          AS Estado,
            CASE
                WHEN r.EstadoID = 31        THEN N'Anulado'
                WHEN r.ProximaDosis IS NULL THEN N''
                WHEN r.ProximaDosis <  @Hoy THEN N'Vencida'
                ELSE N'Pendiente'
            END               AS EstadoProximaDosis
    FROM dbo.RegistrosSanitarios r
    JOIN dbo.Vacas v                   ON v.VacaID          = r.VacaID
    JOIN dbo.TiposRegistroSanitario t  ON t.TipoRegistroID  = r.TipoRegistroID
    JOIN dbo.Estados e                 ON e.EstadoID        = r.EstadoID
    WHERE r.VacaID = @VacaID
      AND v.UsuarioID = @UsuarioID
    ORDER BY r.FechaAplicacion DESC, r.RegistroID DESC;
END
GO

/* ============================================================
   TR1. trg_Vacas_Auditoria  (RN-5-CU12)
   Cada vez que se edita una vaca, guarda en AuditoriaVacas qué
   campo cambió, el valor anterior y el nuevo.
   ============================================================ */
CREATE OR ALTER TRIGGER dbo.trg_Vacas_Auditoria
ON dbo.Vacas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaVacas (VacaID, Campo, ValorAnterior, ValorNuevo)
    SELECT i.VacaID, c.Campo, c.Anterior, c.Nuevo
    FROM inserted i
    JOIN deleted  d ON d.VacaID = i.VacaID
    CROSS APPLY (VALUES
        (N'Raza',            d.Raza,                                   i.Raza),
        (N'Sexo',            d.Sexo,                                   i.Sexo),
        (N'FechaNacimiento', CONVERT(NVARCHAR(10), d.FechaNacimiento, 23), CONVERT(NVARCHAR(10), i.FechaNacimiento, 23)),
        (N'Peso',            CONVERT(NVARCHAR(20), d.Peso),            CONVERT(NVARCHAR(20), i.Peso)),
        (N'FotoUrl',         d.FotoUrl,                                i.FotoUrl),
        (N'Estado',          CONVERT(NVARCHAR(10), d.EstadoID),        CONVERT(NVARCHAR(10), i.EstadoID))
    ) AS c(Campo, Anterior, Nuevo)
    WHERE ISNULL(c.Anterior, N'') <> ISNULL(c.Nuevo, N'');
END
GO

/* ============================================================
   TR2. trg_Vacas_CancelarPublicaciones  (RN-4-CU13, RN-4-CU14)
   Si una vaca pasa de Viva (10) a Fallecida (11) o Vendida
   externamente (13), sus publicaciones Activas o Pausadas pasan a
   Cancelada (25) y se notifica al dueño (campanita, tipo 6).
   ============================================================ */
CREATE OR ALTER TRIGGER dbo.trg_Vacas_CancelarPublicaciones
ON dbo.Vacas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT UPDATE(EstadoID) RETURN;

    DECLARE @Canceladas TABLE (PublicacionVacaID INT, UsuarioID INT, Identificacion NVARCHAR(50));

    UPDATE p
       SET p.EstadoID = 25,
           p.FechaActualizacion = SYSDATETIME()
    OUTPUT inserted.PublicacionVacaID, i.UsuarioID, i.Identificacion INTO @Canceladas
    FROM dbo.PublicacionesVaca p
    JOIN inserted i ON i.VacaID = p.VacaID
    JOIN deleted  d ON d.VacaID = i.VacaID
    WHERE d.EstadoID = 10
      AND i.EstadoID IN (11, 13)
      AND p.EstadoID IN (20, 21);

    -- Notificación al dueño (solo si existe la tabla del script 02)
    IF OBJECT_ID('dbo.Notificaciones') IS NOT NULL
    BEGIN
        INSERT INTO dbo.Notificaciones (UsuarioID, TipoNotificacionID, Mensaje, Enlace, FechaProgramada, EstadoID, FechaCreacion)
        SELECT c.UsuarioID, 6,
               N'Tu publicación de la vaca ' + c.Identificacion + N' se canceló automáticamente porque la vaca ya no está disponible.',
               N'/Publicaciones', SYSDATETIME(), 40, SYSDATETIME()
        FROM @Canceladas c;
    END
END
GO

/* ============================================================
   TR3. trg_RegistrosSanitarios_NoEliminar  (RN-5-CU07, RN-3-CU21)
   Un registro de vacuna/tratamiento nunca se borra de la base:
   en lugar de DELETE se marca como Anulado (31) y se deja la nota
   en Observaciones, para conservar la trazabilidad del historial.
   ============================================================ */
CREATE OR ALTER TRIGGER dbo.trg_RegistrosSanitarios_NoEliminar
ON dbo.RegistrosSanitarios
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE r
       SET r.EstadoID = 31,
           r.Observaciones = CONCAT(r.Observaciones,
                                    CASE WHEN r.Observaciones IS NULL THEN N'' ELSE N' | ' END,
                                    N'Anulado el ', CONVERT(NVARCHAR(16), SYSDATETIME(), 120),
                                    N' (se intentó eliminar)'),
           r.FechaActualizacion = SYSDATETIME()
    FROM dbo.RegistrosSanitarios r
    JOIN deleted d ON d.RegistroID = r.RegistroID;
END
GO

/* ============================================================
   PRUEBAS RÁPIDAS (quita los "--" y ejecútalas con datos)
   ============================================================ */
-- SP1: EXEC dbo.sp_BuscarPublicaciones @Orden = 'precio_asc';
-- SP2: EXEC dbo.sp_ReporteProduccion @UsuarioID = 1, @Desde = '2026-10-01', @Hasta = '2026-11-30';
-- SP3: EXEC dbo.sp_HistorialSanitario @VacaID = 1, @UsuarioID = 1;
-- TR1: UPDATE Vacas SET Peso = Peso + 10 WHERE VacaID = 1;  SELECT * FROM AuditoriaVacas;
-- TR2: (vaca con publicación activa) UPDATE Vacas SET EstadoID = 13 WHERE VacaID = 1;
--      SELECT EstadoID FROM PublicacionesVaca WHERE VacaID = 1;   -- debe quedar 25
-- TR3: DELETE FROM RegistrosSanitarios WHERE RegistroID = 1;
--      SELECT EstadoID, Observaciones FROM RegistrosSanitarios WHERE RegistroID = 1;  -- 31, sigue ahí

-- Lista de lo que quedó creado:
SELECT name AS Objeto, type_desc AS Tipo
FROM sys.objects
WHERE name IN ('sp_BuscarPublicaciones','sp_ReporteProduccion','sp_HistorialSanitario',
               'trg_Vacas_Auditoria','trg_Vacas_CancelarPublicaciones','trg_RegistrosSanitarios_NoEliminar',
               'AuditoriaVacas')
ORDER BY type_desc, name;
GO
