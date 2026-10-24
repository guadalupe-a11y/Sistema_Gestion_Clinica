using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestión_de_Clinica.Models;
using Sistema_Gestión_Clinica.Models;

namespace Sistema_Gestión_Clinica.Data
{
    public class SistemaDeGestionDB : DbContext
    {
        public SistemaDeGestionDB(DbContextOptions<SistemaDeGestionDB> options) : base(options) { }

        public DbSet<Rol> Rol { get; set; }
        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Especialidad> Especialidad { get; set; }
        public DbSet<Consultorio> Consultorio { get; set; }
        public DbSet<Paciente> Paciente { get; set; }
        public DbSet<Medico> Medico { get; set; }
        public DbSet<HorarioMedico> HorarioMedico { get; set; }
        public DbSet<EstadoCita> EstadoCita { get; set; }
        public DbSet<Cita> Cita { get; set; }
        public DbSet<ListaEspera> ListaEspera { get; set; }
        public DbSet<RegistroAtencion> RegistroAtencion { get; set; }
        public DbSet<Expediente> Expediente { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Desactiva el borrado en cascada en Cita para evitar ciclos en SQL Server
            modelBuilder.Entity<Cita>()
                .HasOne(c => c.Paciente)
                .WithMany()
                .HasForeignKey(c => c.IdPaciente)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Cita>()
                .HasOne(c => c.Medico)
                .WithMany()
                .HasForeignKey(c => c.IdMedico)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
