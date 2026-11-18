namespace TuVaca.Comun
{
    /// <summary>
    /// Fotos que suben los usuarios. Se guardan en wwwroot/uploads/{carpeta}
    /// (esa carpeta está en .gitignore, no se sube a GitHub).
    /// </summary>
    public static class Fotos
    {
        public const long TamanoMaximo = 2 * 1024 * 1024; // RNF-02: una imagen no debe superar 2 MB

        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>Devuelve el mensaje de error, o null si la foto está bien (o si no se subió ninguna).</summary>
        public static string? Validar(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                return null;
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension) || !archivo.ContentType.StartsWith("image/"))
            {
                return "La foto debe ser una imagen JPG, PNG o WEBP.";
            }

            if (archivo.Length > TamanoMaximo)
            {
                return "La foto no puede pesar más de 2 MB.";
            }

            return null;
        }

        /// <summary>Guarda la foto con un nombre único y devuelve su ruta web, por ejemplo "/uploads/vacas/abc.jpg".</summary>
        public static async Task<string> GuardarAsync(IFormFile archivo, string carpetaWeb, string carpeta)
        {
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var directorio = Path.Combine(carpetaWeb, "uploads", carpeta);
            Directory.CreateDirectory(directorio);

            var nombre = $"{Guid.NewGuid():N}{extension}";
            await using (var destino = new FileStream(Path.Combine(directorio, nombre), FileMode.CreateNew))
            {
                await archivo.CopyToAsync(destino);
            }

            return $"/uploads/{carpeta}/{nombre}";
        }
    }
}
