using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TuVaca.Servicios
{
    public class CorreoService
    {
        private readonly IConfiguration _configuracion;

        public CorreoService(IConfiguration configuracion)
        {
            _configuracion = configuracion;
        }

        public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            var servidor = _configuracion["Correo:Servidor"] ?? "smtp.gmail.com";
            var puerto = int.TryParse(_configuracion["Correo:Puerto"], out var numero) ? numero : 465;
            var remitente = _configuracion["Correo:Remitente"];
            // Google muestra la contraseña de aplicación con espacios; se quitan
            var contrasena = _configuracion["Correo:Contrasena"]?.Replace(" ", string.Empty);

            if (string.IsNullOrWhiteSpace(remitente) || string.IsNullOrWhiteSpace(contrasena))
            {
                throw new InvalidOperationException(
                    "Falta configurar Correo:Remitente y Correo:Contrasena en los secretos de usuario.");
            }

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("Tu Vaca", remitente));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = asunto;
            mensaje.Body = new BodyBuilder { HtmlBody = cuerpoHtml }.ToMessageBody();

            using var cliente = new SmtpClient();
            var seguridad = puerto == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await cliente.ConnectAsync(servidor, puerto, seguridad);
            await cliente.AuthenticateAsync(remitente, contrasena);
            await cliente.SendAsync(mensaje);
            await cliente.DisconnectAsync(true);
        }
    }
}