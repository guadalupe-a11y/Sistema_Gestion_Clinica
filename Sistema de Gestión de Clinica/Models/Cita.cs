using Sistema_de_Gestión_de_Clinica.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("Cita")]
    public class Cita
    {
        [Key]
        public int IdCita { get; set; }

        [Required, StringLength(10)]
        public string CodigoTurno { get; set; } = string.Empty;

        public int IdPaciente { get; set; }
        public int IdMedico { get; set; }
        public int IdEstadoCita { get; set; }

        public DateTime FechaHora { get; set; }

        [StringLength(255)]
        public string? MotivoConsulta { get; set; }

        public DateTime? HoraCheckIn { get; set; }

        [ForeignKey("IdPaciente")]
        public virtual Paciente? Paciente { get; set; }

        [ForeignKey("IdMedico")]
        public virtual Medico? Medico { get; set; }

        [ForeignKey("IdEstadoCita")]
        public virtual EstadoCita? EstadoCita { get; set; }
    }
}

