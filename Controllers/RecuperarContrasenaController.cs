using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Cuentas;
using TuVaca.Models.Entidades;
using TuVaca.Seguridad;
using TuVaca.Servicios;

namespace TuVaca.Controllers
{
    /// <summary>CU-23: Recuperar contraseña por correo electrónico.</summary>
    public class RecuperarContrasenaController : Controller
    {
        private const int MinutosVigencia = 30; // RN-1-CU23
        private const string MensajeErrorGeneral = "No fue posible completar la operación. Intenta nuevamente."; // FE-1

        private readonly TuVacaDbContext _db;
        private readonly CorreoService _correo;
        private readonly IWebHostEnvironment _entorno;
        private readonly ILogger<RecuperarContrasenaController> _logger;

        public RecuperarContrasenaController(
            TuVacaDbContext db,
            CorreoService correo,
            IWebHostEnvironment entorno,
            ILogger<RecuperarContrasenaController> logger)
        {
            _db = db;
            _correo = correo;
            _entorno = entorno;
            _logger = logger;
        }

        // ===================== PASOS 1 A 5: PEDIR EL ENLACE =====================

        [HttpGet]
        public IActionResult Solicitar()
        {
            // Precondición: el usuario no tiene sesión activa
            if (User.EstaAutenticado())
            {
                TempData["Error"] = "Para recuperar la contraseña primero cierra sesión.";
                return RedirectToAction("Index", "Home");
            }

            return View(new SolicitarRecuperacionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Solicitar(SolicitarRecuperacionViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            try
            {
                var correo = modelo.Correo.Trim().ToLowerInvariant();
                var usuario = await _db.Usuarios
                    .FirstOrDefaultAsync(u => u.Correo == correo && u.EstadoId == Catalogos.UsuarioActivo);

                // Paso 4: solo si el correo existe se envía el enlace
                if (usuario != null)
                {
                    var ahora = DateTime.Now;

                    // Los enlaces anteriores que no se usaron dejan de servir
                    var anteriores = await _db.TokensRecuperacion
                        .Where(t => t.UsuarioId == usuario.UsuarioId && !t.Usado)
                        .ToListAsync();
                    foreach (var anterior in anteriores)
                    {
                        anterior.Usado = true;
                    }

                    // En la base se guarda solo el hash; el token real viaja únicamente en el correo
                    var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                    _db.TokensRecuperacion.Add(new TokensRecuperacion
                    {
                        UsuarioId = usuario.UsuarioId,
                        Token = HashToken(token),
                        FechaExpiracion = ahora.AddMinutes(MinutosVigencia), // RN-1
                        Usado = false,
                        FechaCreacion = ahora
                    });
                    await _db.SaveChangesAsync();

                    var enlace = Url.Action(nameof(Restablecer), "RecuperarContrasena", new { token }, Request.Scheme)!;
                    await _correo.EnviarAsync(
                        usuario.Correo,
                        "Recupera tu contraseña de Tu Vaca",
                        CuerpoCorreo(usuario.NombreCompleto, enlace));
                }

                // Paso 5 y RN-3: el mismo mensaje exista o no el correo
                TempData["Exito"] = "Si el correo está registrado, recibirás un enlace de recuperación en unos minutos.";
                return RedirectToAction(nameof(Solicitar));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-23: error al enviar el enlace de recuperación");
                ModelState.AddModelError(string.Empty, MensajeError(ex)); // FE-1
                return View(modelo);
            }
        }

        // ================ PASOS 6 A 11: CREAR LA NUEVA CONTRASEÑA ================

        [HttpGet]
        public async Task<IActionResult> Restablecer(string? token)
        {
            if (await BuscarTokenValidoAsync(token) == null)
            {
                return View("EnlaceInvalido"); // FE-2
            }

            return View(new RestablecerContrasenaViewModel { Token = token! });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restablecer(RestablecerContrasenaViewModel modelo)
        {
            var registro = await BuscarTokenValidoAsync(modelo.Token);
            if (registro == null)
            {
                return View("EnlaceInvalido"); // FE-2
            }

            if (!ModelState.IsValid)
            {
                return View(modelo); // FE-3
            }

            try
            {
                var ahora = DateTime.Now;
                var usuarioId = registro.UsuarioId;
                var usuario = await _db.Usuarios.FirstAsync(u => u.UsuarioId == usuarioId);

                // Paso 10: nueva contraseña cifrada con el mismo método del registro
                usuario.ContrasenaHash = Contrasenas.Hashear(modelo.NuevaContrasena);
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
                usuario.FechaActualizacion = ahora;

                // FE-2: el enlace no se puede volver a usar
                registro.Usado = true;

                // RN-4: se cierran todas las sesiones activas de la cuenta en otros dispositivos
                var sesiones = await _db.Sesiones
                    .Where(s => s.UsuarioId == usuarioId && s.Activa)
                    .ToListAsync();
                foreach (var sesion in sesiones)
                {
                    sesion.Activa = false;
                }

                await _db.SaveChangesAsync();

                // Paso 11
                TempData["Exito"] = "Tu contraseña quedó actualizada. Ya puedes iniciar sesión con la nueva.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-23: error al restablecer la contraseña");
                ModelState.AddModelError(string.Empty, MensajeError(ex)); // FE-1
                return View(modelo);
            }
        }

        // ============================== APOYO ==============================

        /// <summary>Busca un enlace que exista, no se haya usado y no esté vencido.</summary>
        private async Task<TokensRecuperacion?> BuscarTokenValidoAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var hash = HashToken(token.Trim());
            var ahora = DateTime.Now;
            return await _db.TokensRecuperacion
                .FirstOrDefaultAsync(t => t.Token == hash && !t.Usado && t.FechaExpiracion > ahora);
        }

        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        /// <summary>Mientras desarrollan se ve el detalle técnico (útil si Gmail rechaza la conexión).</summary>
        private string MensajeError(Exception ex) =>
            _entorno.IsDevelopment()
                ? $"{MensajeErrorGeneral} (Detalle para desarrollo: {ex.Message})"
                : MensajeErrorGeneral;

        private static string CuerpoCorreo(string nombre, string enlace)
        {
            var nombreSeguro = WebUtility.HtmlEncode(nombre);
            var enlaceSeguro = WebUtility.HtmlEncode(enlace);
            return $@"
<div style=""font-family: Arial, sans-serif; max-width: 520px; color: #2b2b2b;"">
  <h2 style=""font-family: Georgia, serif; color: #275d38;"">Tu Vaca</h2>
  <p>Hola, {nombreSeguro}:</p>
  <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta.</p>
  <p style=""margin: 24px 0;"">
    <a href=""{enlaceSeguro}"" style=""background: #275d38; color: #ffffff; padding: 12px 20px; border-radius: 6px; text-decoration: none;"">Restablecer contraseña</a>
  </p>
  <p>El enlace vence en {MinutosVigencia} minutos y solo se puede usar una vez.</p>
  <p style=""color: #777777; font-size: 13px;"">Si tú no pediste este cambio, ignora este correo: tu contraseña sigue igual.</p>
</div>";
        }
    }
}
