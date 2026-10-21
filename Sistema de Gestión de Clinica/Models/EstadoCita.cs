using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("EstadoCita")]
    public class EstadoCita
    {
        [Key]
        public int IdEstadoCita { get; set; }

        [Required, StringLength(30)]
        public string NombreEstado { get; set; } = string.Empty;
    }
}
