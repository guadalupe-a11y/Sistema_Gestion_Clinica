using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("HorarioMedico")]
    public class HorarioMedico
    {
        [Key]
        public int IdHorario { get; set; }

        [Required]
        public int IdMedico { get; set; }

        [Required]
        [Range(1, 7, ErrorMessage = "El día debe estar entre 1 (Lunes) y 7 (Domingo).")]
        public int DiaSemana { get; set; } // Cambiado a int (1 = Lunes, 7 = Domingo)

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        public TimeSpan HoraFin { get; set; }

        // Propiedad de navegación
        [ForeignKey("IdMedico")]
        public virtual Medico? Medico { get; set; }
    }
}

