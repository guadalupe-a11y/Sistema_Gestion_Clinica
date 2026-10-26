using System.ComponentModel.DataAnnotations;

namespace Sistema_Gestión_Clinica.ViewModels
{
    public class AgendarCitaViewModel
    {
        public int? IdCita { get; set; } // se usa solo al reprogramar

        [Required(ErrorMessage = "Seleccione una especialidad.")]
        public int IdEspecialidad { get; set; }

        [Required(ErrorMessage = "Seleccione un médico.")]
        public int IdMedico { get; set; }

        [Required(ErrorMessage = "Seleccione una fecha.")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Seleccione una hora.")]
        [DataType(DataType.Time)]
        public TimeSpan Hora { get; set; }

        [Required(ErrorMessage = "Escriba el motivo de la consulta.")]
        [StringLength(200)]
        public string Motivo { get; set; } = "";

        // ---------- ALIAS PARA QUE NO DE ERROR LA VISTA ----------
        // La vista Agendar.cshtml usa FechaHora y MotivoConsulta
        // El controller usa Fecha + Hora y Motivo
        // Con esto los dos nombres apuntan al mismo dato

        public DateTime FechaHora
        {
            get => Fecha.Date + Hora;
            set
            {
                Fecha = value.Date;
                Hora = value.TimeOfDay;
            }
        }

        public string MotivoConsulta
        {
            get => Motivo;
            set => Motivo = value;
        }
    }
}