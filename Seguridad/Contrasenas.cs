using Microsoft.AspNetCore.Identity;

namespace TuVaca.Seguridad
{
    /// <summary>
    /// Cifrado y reglas de las contraseñas (RNF-01 y RN-3-CU01).
    /// El registro (CU-01), el cambio desde el perfil (CU-24) y la
    /// recuperación (CU-23) deben usar SIEMPRE esta misma clase.
    /// </summary>
    public static class Contrasenas
    {
        /// <summary>RN-3-CU01: mínimo 8 caracteres, con al menos una letra, un número y un carácter especial.</summary>
        public const string Patron =
            @"^(?=.*[A-Za-zÁÉÍÓÚÜÑáéíóúüñ])(?=.*\d)(?=.*[^A-Za-zÁÉÍÓÚÜÑáéíóúüñ\d\s]).{8,}$";

        public const string MensajeRegla =
            "Mínimo 8 caracteres, con al menos una letra, un número y un carácter especial (por ejemplo ! # $ % . -).";

        // PasswordHasher viene con ASP.NET Core: guarda la contraseña con PBKDF2 y sal, nunca en texto plano.
        private static readonly PasswordHasher<object> Hasher = new();

        public static string Hashear(string contrasena) => Hasher.HashPassword(new object(), contrasena);

        public static bool Verificar(string? hashGuardado, string contrasena)
        {
            if (string.IsNullOrEmpty(hashGuardado))
            {
                return false;
            }

            try
            {
                var resultado = Hasher.VerifyHashedPassword(new object(), hashGuardado, contrasena);
                return resultado != PasswordVerificationResult.Failed;
            }
            catch (FormatException)
            {
                // El valor guardado no es un hash válido (por ejemplo, un usuario insertado a mano)
                return false;
            }
        }
    }
}
