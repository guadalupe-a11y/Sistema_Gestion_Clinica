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

        [ForeignKey("IdCita")]
        public virtual Cita? Cita { get; set; }
    }
}
