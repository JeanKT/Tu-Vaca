/* ============================================================
   PROYECTO: TU VACA
   Script 02 - Notificaciones (la campanita)

   CÓMO USARLO
   - Ejecútalo DESPUÉS de TuVacaDB_corregido.sql, sobre TuVacaDB.
   - No borra nada: sirve aunque la base ya tenga datos.
   - Si lo ejecutas dos veces no pasa nada: revisa antes de crear.

   CASOS DE USO QUE GENERAN NOTIFICACIONES
   - CU-06 paso 6            : un comprador le escribe al vendedor
   - CU-07 FA-2              : recordatorio de la próxima dosis
   - CU-09 RN-4 y paso 7     : publicación aprobada, rechazada o eliminada
   - CU-13 FA-2 y CU-14 FA-2 : publicación cancelada automáticamente
   - CU-15 RN-6 y paso 7     : venta confirmada (le llega al comprador)
   ============================================================ */

/* Si la base no existe, el script se detiene aquí y no toca nada */
IF DB_ID('TuVacaDB') IS NULL
BEGIN
    RAISERROR(N'La base TuVacaDB no existe. Ejecuta primero TuVacaDB_corregido.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

USE TuVacaDB;
GO

/* ------------------------------------------------------------
   1. ESTADOS DE LA NOTIFICACIÓN (catálogo de estados centralizado)
   Se agrega el ámbito 'NOTIFICACION' a la tabla Estados, con los
   estados No leída (40) y Leída (41).
   ------------------------------------------------------------ */

-- 1.1 Se quita la regla que solo aceptaba los ámbitos anteriores
DECLARE @regla SYSNAME, @sql NVARCHAR(400);

SELECT @regla = name
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('Estados')
  AND definition LIKE '%Ambito%';

IF @regla IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE Estados DROP CONSTRAINT ' + QUOTENAME(@regla);
    EXEC sp_executesql @sql;
END
GO

-- 1.2 Nueva regla con el ámbito NOTIFICACION incluido
ALTER TABLE Estados ADD CONSTRAINT CK_Estados_Ambito
    CHECK (Ambito IN ('USUARIO','VACA','PUBLICACION','REGISTRO_SANITARIO','NOTIFICACION'));
GO

-- 1.3 Estados de la notificación
IF NOT EXISTS (SELECT 1 FROM Estados WHERE EstadoID = 40)
    INSERT INTO Estados (EstadoID, Nombre, Ambito) VALUES (40, N'No leída', 'NOTIFICACION');

IF NOT EXISTS (SELECT 1 FROM Estados WHERE EstadoID = 41)
    INSERT INTO Estados (EstadoID, Nombre, Ambito) VALUES (41, N'Leída', 'NOTIFICACION');
GO

/* ------------------------------------------------------------
   2. TIPOS DE NOTIFICACIÓN (catálogo)
   Los ID son fijos porque el código los usa.
   ------------------------------------------------------------ */
IF OBJECT_ID('TiposNotificacion') IS NULL
    CREATE TABLE TiposNotificacion (
        TipoNotificacionID  INT          NOT NULL PRIMARY KEY,
        Nombre              NVARCHAR(60) NOT NULL UNIQUE
    );
GO

IF NOT EXISTS (SELECT 1 FROM TiposNotificacion)
    INSERT INTO TiposNotificacion (TipoNotificacionID, Nombre) VALUES
        (1, N'Nuevo mensaje de un comprador'),          -- CU-06 paso 6
        (2, N'Recordatorio de próxima dosis'),          -- CU-07 FA-2
        (3, N'Publicación aprobada'),                   -- CU-09 RN-4
        (4, N'Publicación rechazada'),                  -- CU-09 RN-4 (con el motivo, RN-2)
        (5, N'Publicación eliminada'),                  -- CU-09 RN-4 (con el motivo, RN-2)
        (6, N'Publicación cancelada automáticamente'),  -- CU-13 FA-2 y CU-14 FA-2
        (7, N'Venta confirmada');                       -- CU-15 RN-6
GO

/* ------------------------------------------------------------
   3. NOTIFICACIONES (lo que muestra la campanita)
   - UsuarioID: quién recibe la notificación.
   - Enlace: página que se abre al hacer clic (opcional).
   - FechaProgramada: desde cuándo se muestra. Normalmente es el
     momento en que se crea. Para el recordatorio de la próxima
     dosis (CU-07 FA-2) se guarda la fecha de esa dosis, y la
     campanita solo lo muestra cuando llega el día.
   - EstadoID: 40 = No leída, 41 = Leída (catálogo Estados).
   - Si el usuario elimina su cuenta (CU-25), sus notificaciones
     se borran con ella (ON DELETE CASCADE).
   ------------------------------------------------------------ */
IF OBJECT_ID('Notificaciones') IS NULL
    CREATE TABLE Notificaciones (
        NotificacionID      INT IDENTITY(1,1) PRIMARY KEY,
        UsuarioID           INT           NOT NULL,
        TipoNotificacionID  INT           NOT NULL,
        Mensaje             NVARCHAR(500) NOT NULL,
        Enlace              NVARCHAR(300) NULL,
        FechaProgramada     DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        EstadoID            INT           NOT NULL DEFAULT 40,   -- 40 = No leída
        AmbitoEstado        AS CAST('NOTIFICACION' AS VARCHAR(20)) PERSISTED,
        FechaCreacion       DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_Notificaciones_Usuarios FOREIGN KEY (UsuarioID)
            REFERENCES Usuarios(UsuarioID) ON DELETE CASCADE,
        CONSTRAINT FK_Notificaciones_TiposNotificacion FOREIGN KEY (TipoNotificacionID)
            REFERENCES TiposNotificacion(TipoNotificacionID),
        CONSTRAINT FK_Notificaciones_Estados FOREIGN KEY (EstadoID, AmbitoEstado)
            REFERENCES Estados(EstadoID, Ambito)
    );
GO

-- Para que la campanita cuente rápido las no leídas de cada usuario
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Notificaciones_Usuario'
                 AND object_id = OBJECT_ID('Notificaciones'))
    CREATE INDEX IX_Notificaciones_Usuario
        ON Notificaciones (UsuarioID, EstadoID, FechaProgramada);
GO

/* ------------------------------------------------------------
   4. VERIFICACIÓN
   Debe salir: TiposNotificacion 7, Estados de notificación 2 y
   Notificaciones 0 (si es la primera vez).
   ------------------------------------------------------------ */
SELECT N'TiposNotificacion' AS Tabla, COUNT(*) AS Registros FROM TiposNotificacion
UNION ALL SELECT N'Estados de notificación', COUNT(*) FROM Estados WHERE Ambito = 'NOTIFICACION'
UNION ALL SELECT N'Notificaciones',          COUNT(*) FROM Notificaciones;
GO

/* ------------------------------------------------------------
   PRUEBA (opcional, para cuando la campanita esté lista)
   Quita los "--" para crearle una notificación al usuario 1.
   ------------------------------------------------------------ */
-- INSERT INTO Notificaciones (UsuarioID, TipoNotificacionID, Mensaje)
-- VALUES (1, 3, N'Tu publicación fue aprobada por el administrador.');

SET NOEXEC OFF;
GO

/* ============================================================
   FIN DEL SCRIPT 02
   ============================================================ */
