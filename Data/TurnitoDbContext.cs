using Microsoft.EntityFrameworkCore;
using TurnitoCL.Models;

namespace TurnitoCL.Data
{
    public class TurnitoDbContext : DbContext
    {
        public TurnitoDbContext(DbContextOptions<TurnitoDbContext> options) : base(options)
        {
        }

        // DbSets - Representan las tablas
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<DisponibilidadSemanal> DisponibilidadSemanal { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Asistencia> Asistencias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Apellido).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.Rol).IsRequired().HasMaxLength(20);
            });

            // Configuración Proveedor
            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NombreNegocio).HasMaxLength(200);
                entity.Property(e => e.Direccion).HasMaxLength(500);
                entity.Property(e => e.Descripcion).HasMaxLength(1000);

                // Relación con Usuario
                entity.HasOne(p => p.Usuario)
                      .WithOne(u => u.Proveedor)
                      .HasForeignKey<Proveedor>(p => p.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración Servicio
            modelBuilder.Entity<Servicio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasMaxLength(500);
                entity.Property(e => e.Precio).HasPrecision(10, 2).IsRequired();
                entity.Property(e => e.DuracionMinutos).IsRequired();
                entity.Property(e => e.Activo).IsRequired();

                // Relación con Proveedor
                entity.HasOne(s => s.Proveedor)
                      .WithMany(p => p.Servicios)
                      .HasForeignKey(s => s.ProveedorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración DisponibilidadSemanal
            modelBuilder.Entity<DisponibilidadSemanal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DiaSemana).IsRequired();
                entity.Property(e => e.HoraInicio).IsRequired();
                entity.Property(e => e.HoraFin).IsRequired();
                entity.Property(e => e.Activo).IsRequired();

                // Índice único para evitar duplicados
                entity.HasIndex(e => new { e.ProveedorId, e.DiaSemana, e.HoraInicio })
                      .IsUnique();

                // Relación con Proveedor
                entity.HasOne(d => d.Proveedor)
                      .WithMany(p => p.Disponibilidades)
                      .HasForeignKey(d => d.ProveedorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración Reserva
            modelBuilder.Entity<Reserva>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FechaHora).IsRequired();
                entity.Property(e => e.Estado).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CodigoQR).HasMaxLength(500);
                entity.Property(e => e.Observaciones).HasMaxLength(1000);

                // Índice único para CodigoQR
                entity.HasIndex(e => e.CodigoQR).IsUnique();

                // Relaciones
                entity.HasOne(r => r.Consumidor)
                      .WithMany(u => u.ReservasComoConsumidor)
                      .HasForeignKey(r => r.ConsumidorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Proveedor)
                      .WithMany(p => p.Reservas)
                      .HasForeignKey(r => r.ProveedorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Servicio)
                      .WithMany(s => s.Reservas)
                      .HasForeignKey(r => r.ServicioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración Asistencia
            modelBuilder.Entity<Asistencia>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MetodoRegistro).HasMaxLength(50);
                entity.Property(e => e.Observaciones).HasMaxLength(500);

                // Relación con Reserva
                entity.HasOne(a => a.Reserva)
                      .WithMany(r => r.Asistencias)
                      .HasForeignKey(a => a.ReservaId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}