using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("ListaEspera")]
    public class ListaEspera
    {
        [Key]
        public int IdListaEspera { get; set; }

        public int IdPaciente { get; set; }
        public int IdMedico { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [Required, StringLength(20)]
        public string Estado { get; set; } = "Activo"; // Activo, Notificado, Cancelado

        [ForeignKey("IdPaciente")]
        public virtual Paciente? Paciente { get; set; }

        [ForeignKey("IdMedico")]
        public virtual Medico? Medico { get; set; }
    }
}
