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

        // Mantenemos solo UNA definición de FechaAtencion
        public DateTime FechaAtencion { get; set; } = DateTime.Now;

        [ForeignKey("IdCita")]
        public virtual Cita? Cita { get; set; }
        public int IdExpediente { get; internal set; }
    }
}