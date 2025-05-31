using System.Net;
using System.Net.Mail;
using System.Text;
using TurnitoCL.Models;

namespace TurnitoCL.Services
{
    public interface IEmailService
    {
        Task EnviarEmailConfirmacionReservaAsync(Reserva reserva);
        Task EnviarEmailCancelacionReservaAsync(Reserva reserva);
        Task EnviarEmailRecordatorioReservaAsync(Reserva reserva);
        Task EnviarEmailNuevoRegistroProveedorAsync(Usuario usuario, Proveedor proveedor);
        Task EnviarEmailNuevoRegistroConsumidorAsync(Usuario usuario);
        Task EnviarEmailAsistenciaConfirmadaAsync(Reserva reserva);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Configuración SMTP (puedes usar Gmail, SendGrid, etc.)
            _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:Username"] ?? "";
            _smtpPassword = _configuration["Email:Password"] ?? "";
            _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@turnitocl.com";
            _fromName = _configuration["Email:FromName"] ?? "TurnitoCL";
        }

        public async Task EnviarEmailConfirmacionReservaAsync(Reserva reserva)
        {
            var asunto = "✅ Confirmación de Cita - TurnitoCL";
            var cuerpo = GenerarEmailConfirmacionReserva(reserva);

            await EnviarEmailAsync(reserva.Consumidor.Email, asunto, cuerpo);

            // También enviar al proveedor
            var asuntoProveedor = "📅 Nueva Cita Agendada - TurnitoCL";
            var cuerpoProveedor = GenerarEmailNuevaCitaProveedor(reserva);
            await EnviarEmailAsync(reserva.Proveedor.Usuario.Email, asuntoProveedor, cuerpoProveedor);
        }

        public async Task EnviarEmailCancelacionReservaAsync(Reserva reserva)
        {
            var asunto = "❌ Cita Cancelada - TurnitoCL";
            var cuerpo = GenerarEmailCancelacionReserva(reserva);

            await EnviarEmailAsync(reserva.Consumidor.Email, asunto, cuerpo);

            // También notificar al proveedor
            var asuntoProveedor = "📅 Cita Cancelada - TurnitoCL";
            var cuerpoProveedor = GenerarEmailCitaCanceladaProveedor(reserva);
            await EnviarEmailAsync(reserva.Proveedor.Usuario.Email, asuntoProveedor, cuerpoProveedor);
        }

        public async Task EnviarEmailRecordatorioReservaAsync(Reserva reserva)
        {
            var asunto = "⏰ Recordatorio de Cita - TurnitoCL";
            var cuerpo = GenerarEmailRecordatorio(reserva);

            await EnviarEmailAsync(reserva.Consumidor.Email, asunto, cuerpo);
        }

        public async Task EnviarEmailNuevoRegistroProveedorAsync(Usuario usuario, Proveedor proveedor)
        {
            var asunto = "🎉 Bienvenido a TurnitoCL - Registro de Proveedor";
            var cuerpo = GenerarEmailBienvenidaProveedor(usuario, proveedor);

            await EnviarEmailAsync(usuario.Email, asunto, cuerpo);
        }

        public async Task EnviarEmailNuevoRegistroConsumidorAsync(Usuario usuario)
        {
            var asunto = "🎉 Bienvenido a TurnitoCL";
            var cuerpo = GenerarEmailBienvenidaConsumidor(usuario);

            await EnviarEmailAsync(usuario.Email, asunto, cuerpo);
        }

        public async Task EnviarEmailAsistenciaConfirmadaAsync(Reserva reserva)
        {
            var asunto = "✅ Asistencia Confirmada - TurnitoCL";
            var cuerpo = GenerarEmailAsistenciaConfirmada(reserva);

            await EnviarEmailAsync(reserva.Consumidor.Email, asunto, cuerpo);
        }

        private async Task EnviarEmailAsync(string toEmail, string asunto, string cuerpoHtml)
        {
            try
            {
                using var smtpClient = new SmtpClient(_smtpServer, _smtpPort);
                smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                smtpClient.EnableSsl = true;

                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(_fromEmail, _fromName);
                mailMessage.To.Add(toEmail);
                mailMessage.Subject = asunto;
                mailMessage.Body = cuerpoHtml;
                mailMessage.IsBodyHtml = true;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.SubjectEncoding = Encoding.UTF8;

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email enviado exitosamente a {Email} con asunto: {Asunto}", toEmail, asunto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email a {Email} con asunto: {Asunto}", toEmail, asunto);
                throw;
            }
        }

        private string GenerarEmailConfirmacionReserva(Reserva reserva)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .cita-info {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #6c757d; color: white; padding: 15px; text-align: center; }}
                        .btn {{ display: inline-block; padding: 10px 20px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>✅ Cita Confirmada</h1>
                            <p>Tu cita ha sido agendada exitosamente</p>
                        </div>
                        <div class='content'>
                            <h2>Hola {reserva.Consumidor.Nombre},</h2>
                            <p>Tu cita ha sido confirmada. Aquí están los detalles:</p>
                            
                            <div class='cita-info'>
                                <h3>📅 Detalles de la Cita</h3>
                                <p><strong>Servicio:</strong> {reserva.Servicio.Nombre}</p>
                                <p><strong>Fecha:</strong> {reserva.FechaHora:dd/MM/yyyy}</p>
                                <p><strong>Hora:</strong> {reserva.FechaHora:HH:mm}</p>
                                <p><strong>Duración:</strong> {reserva.Servicio.DuracionMinutos} minutos</p>
                                <p><strong>Precio:</strong> {reserva.Servicio.PrecioFormateado}</p>
                            </div>

                            <div class='cita-info'>
                                <h3>🏪 Información del Proveedor</h3>
                                <p><strong>{reserva.Proveedor.NombreNegocio}</strong></p>
                                <p>{reserva.Proveedor.Usuario.NombreCompleto}</p>
                                <p>📞 {reserva.Proveedor.Usuario.Telefono}</p>
                                <p>📍 {reserva.Proveedor.Direccion}</p>
                            </div>

                            <p><strong>Código de Reserva:</strong> #{reserva.Id}</p>
                            
                            <h3>📱 Código QR</h3>
                            <p>Presenta este código QR el día de tu cita para confirmar tu asistencia:</p>
                            <p><strong>Código:</strong> {reserva.CodigoQR}</p>
                            
                            <p><strong>⚠️ Importante:</strong></p>
                            <ul>
                                <li>Llega puntual a tu cita</li>
                                <li>Puedes cancelar hasta 2 horas antes</li>
                                <li>Presenta tu código QR para confirmar asistencia</li>
                            </ul>
                        </div>
                        <div class='footer'>
                            <p>© 2024 TurnitoCL - Tu plataforma de agendamiento de citas</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerarEmailCancelacionReserva(Reserva reserva)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .cita-info {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #6c757d; color: white; padding: 15px; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>❌ Cita Cancelada</h1>
                            <p>Tu cita ha sido cancelada</p>
                        </div>
                        <div class='content'>
                            <h2>Hola {reserva.Consumidor.Nombre},</h2>
                            <p>Tu cita ha sido cancelada exitosamente.</p>
                            
                            <div class='cita-info'>
                                <h3>📅 Detalles de la Cita Cancelada</h3>
                                <p><strong>Servicio:</strong> {reserva.Servicio.Nombre}</p>
                                <p><strong>Fecha:</strong> {reserva.FechaHora:dd/MM/yyyy}</p>
                                <p><strong>Hora:</strong> {reserva.FechaHora:HH:mm}</p>
                                <p><strong>Proveedor:</strong> {reserva.Proveedor.NombreNegocio}</p>
                            </div>

                            <p>Esperamos verte pronto. Puedes agendar una nueva cita cuando gustes.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 TurnitoCL - Tu plataforma de agendamiento de citas</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerarEmailRecordatorio(Reserva reserva)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                        .header {{ background-color: #ffc107; color: #212529; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .cita-info {{ background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #6c757d; color: white; padding: 15px; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>⏰ Recordatorio de Cita</h1>
                            <p>Tu cita es mañana</p>
                        </div>
                        <div class='content'>
                            <h2>Hola {reserva.Consumidor.Nombre},</h2>
                            <p>Te recordamos que tienes una cita agendada:</p>
                            
                            <div class='cita-info'>
                                <h3>📅 Detalles de tu Cita</h3>
                                <p><strong>Servicio:</strong> {reserva.Servicio.Nombre}</p>
                                <p><strong>Fecha:</strong> {reserva.FechaHora:dd/MM/yyyy}</p>
                                <p><strong>Hora:</strong> {reserva.FechaHora:HH:mm}</p>
                                <p><strong>Lugar:</strong> {reserva.Proveedor.NombreNegocio}</p>
                                <p><strong>Dirección:</strong> {reserva.Proveedor.Direccion}</p>
                            </div>

                            <p><strong>💡 Recuerda:</strong></p>
                            <ul>
                                <li>Llegar puntual</li>
                                <li>Traer tu código QR: <strong>{reserva.CodigoQR}</strong></li>
                                <li>Si no puedes asistir, cancela con al menos 2 horas de anticipación</li>
                            </ul>
                        </div>
                        <div class='footer'>
                            <p>© 2024 TurnitoCL - Tu plataforma de agendamiento de citas</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerarEmailBienvenidaProveedor(Usuario usuario, Proveedor proveedor)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .footer {{ background-color: #6c757d; color: white; padding: 15px; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🎉 ¡Bienvenido a TurnitoCL!</h1>
                            <p>Tu cuenta de proveedor está lista</p>
                        </div>
                        <div class='content'>
                            <h2>Hola {usuario.NombreCompleto},</h2>
                            <p>¡Bienvenido a TurnitoCL! Tu cuenta de proveedor ha sido creada exitosamente.</p>
                            
                            <h3>🏪 Información de tu Negocio</h3>
                            <p><strong>Nombre del Negocio:</strong> {proveedor.NombreNegocio}</p>
                            <p><strong>Email:</strong> {usuario.Email}</p>
                            
                            <h3>📋 Próximos Pasos</h3>
                            <ol>
                                <li>Configura tus servicios</li>
                                <li>Establece tu disponibilidad</li>
                                <li>¡Comienza a recibir clientes!</li>
                            </ol>
                            
                            <p>¡Estamos emocionados de tenerte en nuestra plataforma!</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 TurnitoCL - Tu plataforma de agendamiento de citas</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerarEmailBienvenidaConsumidor(Usuario usuario)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .footer {{ background-color: #6c757d; color: white; padding: 15px; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🎉 ¡Bienvenido a TurnitoCL!</h1>
                            <p>Tu cuenta está lista</p>
                        </div>
                        <div class='content'>
                            <h2>Hola {usuario.NombreCompleto},</h2>
                            <p>¡Bienvenido a TurnitoCL! Tu cuenta ha sido creada exitosamente.</p>
                            
                            <h3>✨ ¿Qué puedes hacer ahora?</h3>
                            <ul>
                                <li>Buscar servicios de peluquería</li>
                                <li>Agendar citas fácilmente</li>
                                <li>Gestionar tus reservas</li>
                                <li>Recibir códigos QR para tus citas</li>
                            </ul>
                            
                            <p>¡Comienza a agendar tu primera cita!</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 TurnitoCL - Tu plataforma de agendamiento de citas</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerarEmailNuevaCitaProveedor(Reserva reserva)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                        .header {{ background-color: #17a2b8; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .cita-info {{ background-color: #d1ecf1; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #6c757d; color: white; padding: 15px; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>📅 Nueva Cita Agendada</h1>
                            <p>Tienes una nueva reserva</p>
                        </div>
                        <div class='content'>
                            <h2>Hola {reserva.Proveedor.Usuario.Nombre},</h2>
                            <p>Se ha agendado una nueva cita en tu negocio:</p>
                            
                            <div class='cita-info'>
                                <h3>👤 Información del Cliente</h3>
                                <p><strong>Nombre:</strong> {reserva.Consumidor.NombreCompleto}</p>
                                <p><strong>Email:</strong> {reserva.Consumidor.Email}</p>
                                <p><strong>Teléfono:</strong> {reserva.Consumidor.Telefono}</p>
                            </div>

                            <div class='cita-info'>
                                <h3>📅 Detalles de la Cita</h3>
                                <p><strong>Servicio:</strong> {reserva.Servicio.Nombre}</p>
                                <p><strong>Fecha:</strong> {reserva.FechaHora:dd/MM/yyyy}</p>
                                <p><strong>Hora:</strong> {reserva.FechaHora:HH:mm}</p>
                                <p><strong>Duración:</strong> {reserva.Servicio.DuracionMinutos} minutos</p>
                                <p><strong>Precio:</strong> {reserva.Servicio.PrecioFormateado}</p>
                            </div>

                            <p><strong>Código QR del Cliente:</strong> {reserva.CodigoQR}</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 TurnitoCL - Tu plataforma de agendamiento de citas</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerarEmailCitaCanceladaProveedor(Reserva reserva)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .footer {{ background-color: #6c757d; color: white; padding: 15px; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>📅 Cita Cancelada</h1>
                            <p>Una cita ha sido cancelada</p>
                        </div>
                        <div class='content'>
                            <h2>Hola {reserva.Proveedor.Usuario.Nombre},</h2>
                            <p>Se ha cancelado una cita:</p>
                            
                            <p><strong>Cliente:</strong> {reserva.Consumidor.NombreCompleto}</p>
                            <p><strong>Servicio:</strong> {reserva.Servicio.Nombre}</p>
                            <p><strong>Fecha:</strong> {reserva.FechaHora:dd/MM/yyyy HH:mm}</p>
                            
                            <p>El horario ahora está disponible para otros clientes.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 TurnitoCL - Tu plataforma de agendamiento de citas</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerarEmailAsistenciaConfirmada(Reserva reserva)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .footer {{ background-color: #6c757d; color: white; padding: 15px; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>✅ Asistencia Confirmada</h1>
                            <p>Tu cita ha sido completada</p>
                        </div>
                        <div class='content'>
                            <h2>Hola {reserva.Consumidor.Nombre},</h2>
                            <p>Tu asistencia a la cita ha sido confirmada exitosamente.</p>
                            
                            <p><strong>Servicio:</strong> {reserva.Servicio.Nombre}</p>
                            <p><strong>Fecha:</strong> {reserva.FechaHora:dd/MM/yyyy HH:mm}</p>
                            <p><strong>Proveedor:</strong> {reserva.Proveedor.NombreNegocio}</p>
                            
                            <p>¡Gracias por usar TurnitoCL! Esperamos verte pronto.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 TurnitoCL - Tu plataforma de agendamiento de citas</p>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}