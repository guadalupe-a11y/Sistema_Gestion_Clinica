using Sistema_de_Gestión_de_Clinica.Models;
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
        public int? IdMedico { get; set; }
        public int IdEspecialidad { get; set; }

        public DateTime FechaDeseada { get; set; }
        public int Prioridad { get; set; } = 2;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public bool Atendido { get; set; } = false;

        [ForeignKey("IdPaciente")]
        public virtual Paciente? Paciente { get; set; }

        [ForeignKey("IdMedico")]
        public virtual Medico? Medico { get; set; }

        [ForeignKey("IdEspecialidad")]
        public virtual Especialidad? Especialidad { get; set; }
    }
}
