    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace Sistema_Gestión_Clinica.Models
    {
        [Table("ListaEspera")]
        public class ListaEspera
        {
            [Key]
            public int IdListaEspera { get; set; }

            [Required]
            public int IdPaciente { get; set; }

            // Permitir NULL para cuando la solicitud es general por especialidad
            public int? IdMedico { get; set; }

            [Required]
            public int IdEspecialidad { get; set; }

            [Required]
            [DataType(DataType.Date)]
            public DateTime FechaDeseada { get; set; }

            [Required]
            public int Prioridad { get; set; } = 2;

            [Required]
            public DateTime FechaRegistro { get; set; } = DateTime.Now;

            [Required]
            public bool Atendido { get; set; } = false;

            // Propiedades de navegación
            [ForeignKey("IdPaciente")]
            public virtual Paciente? Paciente { get; set; }

            [ForeignKey("IdMedico")]
            public virtual Medico? Medico { get; set; }

            [ForeignKey("IdEspecialidad")]
            public virtual Especialidad? Especialidad { get; set; }
        }
    }

