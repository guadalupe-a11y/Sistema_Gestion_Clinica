using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("Especialidad")]
    public class Especialidad
    {
        [Key]
        public int IdEspecialidad { get; set; }

        [Required, StringLength(50)]
        public string NombreEspecialidad { get; set; } = string.Empty;
    }
}