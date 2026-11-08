using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("RegistroAtencion")]
    public class RegistroAtencion
    {
        [Key]
        public int IdRegistro { get; set; }

        public int IdCita { get; set; }

        public string? NotasConsulta { get; set; }
        public string? Diagnostico { get; set; }
        public string? Indicaciones { get; set; }

        // Nuevos campos agregados para la consulta médica completa
        public string? Sintomas { get; set; }
        public string? RecetaMedica { get; set; }
        public string? EstudiosSolicitados { get; set; }
        public DateTime? ProximaCitaRecomendada { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [ForeignKey("IdCita")]
        public virtual Cita? Cita { get; set; }
        public int IdExpediente { get; internal set; }
        public DateTime FechaAtencion { get; internal set; }
    }
}

