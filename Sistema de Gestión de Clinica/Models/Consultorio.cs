using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("Consultorio")]
    public class Consultorio
    {
        internal string? NumeroConsultorio;

        [Key]
        public int IdConsultorio { get; set; }

        [Required, StringLength(50)]
        public string NombreConsultorio { get; set; } = string.Empty;
    }
}