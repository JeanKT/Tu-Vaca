using System.Globalization;

namespace TuVaca.Comun
{
    /// <summary>Formatos para mostrar datos en las vistas.</summary>
    public static class Formatos
    {
        private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

        /// <summary>1500000 -> "$ 1.500.000"</summary>
        public static string Pesos(decimal valor) => valor.ToString("C0", Colombia);

        public static string Fecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy");

        public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy");

        public static string FechaHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm");

        /// <summary>Edad a partir de la fecha de nacimiento, por ejemplo "2 años y 3 meses".</summary>
        public static string Edad(DateOnly? nacimiento)
        {
            if (nacimiento == null)
            {
                return "Sin dato";
            }

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var meses = (hoy.Year - nacimiento.Value.Year) * 12 + hoy.Month - nacimiento.Value.Month;
            if (hoy.Day < nacimiento.Value.Day)
            {
                meses--;
            }

            if (meses < 1)
            {
                return "Menos de 1 mes";
            }

            var anios = meses / 12;
            var resto = meses % 12;
            var partes = new List<string>();
            if (anios > 0)
            {
                partes.Add(anios == 1 ? "1 año" : $"{anios} años");
            }
            if (resto > 0)
            {
                partes.Add(resto == 1 ? "1 mes" : $"{resto} meses");
            }
            return string.Join(" y ", partes);
        }
    }
}
