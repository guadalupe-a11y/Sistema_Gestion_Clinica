using Sistema_de_Gestión_de_Clinica.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("Medico")]
    public class Medico
    {
        [Key]
        public int IdMedico { get; set; }

        public int IdUsuario { get; set; }
        public int IdEspecialidad { get; set; }
        public int IdConsultorio { get; set; }

        [Required, StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        public int DuracionConsultaMin { get; set; } = 20;

        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey("IdEspecialidad")]
        public virtual Especialidad? Especialidad { get; set; }

        [ForeignKey("IdConsultorio")]
        public virtual Consultorio? Consultorio { get; set; }
    }
}