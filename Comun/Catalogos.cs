namespace TuVaca.Comun
{
    /// <summary>
    /// Valores fijos de los catálogos de la base de datos
    /// (ver BaseDeDatos/TuVacaDB_corregido.sql y TuVacaDB_02_notificaciones.sql).
    /// </summary>
    public static class Catalogos
    {
        // Roles (tabla Roles, columna NombreRol)
        public const string RolProductor = "Productor/Vendedor";
        public const string RolComprador = "Comprador/Visitante";
        public const string RolAdministrador = "Administrador";

        // Ámbitos de la tabla Estados. La columna AmbitoEstado de cada tabla
        // la calcula SQL Server; aquí solo se usan como referencia.
        public const string AmbitoUsuario = "USUARIO";
        public const string AmbitoVaca = "VACA";
        public const string AmbitoPublicacion = "PUBLICACION";
        public const string AmbitoRegistroSanitario = "REGISTRO_SANITARIO";
        public const string AmbitoNotificacion = "NOTIFICACION";

        // Estados de usuario (CU-10)
        public const int UsuarioActivo = 1;
        public const int UsuarioInactivo = 2;

        // Estados de vaca
        public const int VacaViva = 10;
        public const int VacaFallecida = 11;
        public const int VacaVendidaPlataforma = 12;
        public const int VacaVendidaExterna = 13;

        // Estados de publicación
        public const int PublicacionActiva = 20;
        public const int PublicacionPausada = 21;
        public const int PublicacionVendida = 22;
        public const int PublicacionArrendada = 23;
        public const int PublicacionRechazada = 24;
        public const int PublicacionCancelada = 25;
        public const int PublicacionEliminada = 26;

        // Estados de registro sanitario
        public const int RegistroSanitarioActivo = 30;
        public const int RegistroSanitarioAnulado = 31;

        // Estados de notificación (script 02)
        public const int NotificacionNoLeida = 40;
        public const int NotificacionLeida = 41;
    }
}
