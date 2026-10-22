using System.ComponentModel.DataAnnotations;

namespace Sistema_Gestión_Clinica.ViewModels
{
    public class AgendarCitaViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un médico")]
        public int IdMedico { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la fecha y hora")]
        public DateTime FechaHora { get; set; }

        [Required(ErrorMessage = "Escriba el motivo de su consulta")]
        [StringLength(255)]
        public string MotivoConsulta { get; set; } = string.Empty;
    }
}