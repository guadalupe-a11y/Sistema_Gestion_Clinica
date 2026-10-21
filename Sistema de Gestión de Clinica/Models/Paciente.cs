using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("Paciente")]
    public class Paciente
    {
        [Key]
        public int IdPaciente { get; set; }
        public int IdUsuario { get; set; }

        [Required, StringLength(10)]
        public string DUI { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [Required, StringLength(15)]
        public string Telefono { get; set; } = string.Empty;

        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}
