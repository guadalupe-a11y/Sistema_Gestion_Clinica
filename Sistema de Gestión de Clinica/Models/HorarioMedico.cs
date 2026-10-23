using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("HorarioMedico")]
    public class HorarioMedico
    {
        [Key]
        public int IdHorario { get; set; }

        public int IdMedico { get; set; }

        [Required, StringLength(20)]
        public string DiaSemana { get; set; } = string.Empty;

        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }

        [ForeignKey("IdMedico")]
        public virtual Medico? Medico { get; set; }
    }
}