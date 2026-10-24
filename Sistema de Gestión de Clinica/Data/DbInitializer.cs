using Sistema_Gestión_Clinica.Models;

namespace Sistema_Gestión_Clinica.Data
{
    public static class DbInitializer
    {
        public static void Initialize(SistemaDeGestionDB context)
        {

            // 1. Verificar/Insertar Roles
            if (context.Rol.Any())
            {
            }
            else
            {
                context.Rol.AddRange(
                    new Rol { NombreRol = "Administrador" },
                    new Rol { NombreRol = "Recepcionista" },
                    new Rol { NombreRol = "Medico" },
                    new Rol { NombreRol = "Paciente" }
                );
                context.SaveChanges();
            }

            // 2. Verificar/Insertar Estados de Cita
            if (!context.EstadoCita.Any())
            {
                context.EstadoCita.AddRange(
                    new EstadoCita { NombreEstado = "Agendada" },
                    new EstadoCita { NombreEstado = "En Espera" },
                    new EstadoCita { NombreEstado = "En Consulta" },
                    new EstadoCita { NombreEstado = "Finalizada" },
                    new EstadoCita { NombreEstado = "Cancelada" }
                );
                context.SaveChanges();
            }

            // 3. Insertar Usuarios y datos si la tabla de usuarios está vacía
            if (!context.Usuario.Any())
            {
                // Especialidades y Consultorios
                var espGeneral = new Especialidad { NombreEspecialidad = "Medicina General" };
                var cons101 = new Consultorio { NombreConsultorio = "Consultorio 101" };
                context.Especialidad.Add(espGeneral);
                context.Consultorio.Add(cons101);
                context.SaveChanges();

                // IDs de Roles
                var idAdmin = context.Rol.First(r => r.NombreRol == "Administrador").IdRol;
                var idRecep = context.Rol.First(r => r.NombreRol == "Recepcionista").IdRol;
                var idMedico = context.Rol.First(r => r.NombreRol == "Medico").IdRol;
                var idPaciente = context.Rol.First(r => r.NombreRol == "Paciente").IdRol;

                // Crear Usuarios
                var uAdmin = new Usuario { Correo = "admin@clinica.com", PasswordHash = "123456", IdRol = idAdmin, Activo = true };
                var uRecep = new Usuario { Correo = "recepcion@clinica.com", PasswordHash = "123456", IdRol = idRecep, Activo = true };
                var uMed1 = new Usuario { Correo = "medico1@clinica.com", PasswordHash = "123456", IdRol = idMedico, Activo = true };
                var uPac1 = new Usuario { Correo = "paciente1@clinica.com", PasswordHash = "123456", IdRol = idPaciente, Activo = true };

                context.Usuario.AddRange(uAdmin, uRecep, uMed1, uPac1);
                context.SaveChanges();

                // Perfiles de Médico y Paciente
                var medico = new Medico
                {
                    IdUsuario = uMed1.IdUsuario,
                    IdEspecialidad = espGeneral.IdEspecialidad,
                    IdConsultorio = cons101.IdConsultorio,
                    Nombre = "Carlos",
                    Apellido = "Gómez",
                    DuracionConsultaMin = 20
                };

                var paciente = new Paciente
                {
                    IdUsuario = uPac1.IdUsuario,
                    DUI = "01234567-8",
                    Nombre = "Juan",
                    Apellido = "Pérez",
                    Telefono = "7777-8888"
                };

                context.Medico.Add(medico);
                context.Paciente.Add(paciente);
                context.SaveChanges();

                // Cita inicial de prueba
                var estadoEspera = context.EstadoCita.First(e => e.NombreEstado == "En Espera").IdEstadoCita;
                context.Cita.Add(new Cita
                {
                    CodigoTurno = "T-001",
                    IdPaciente = paciente.IdPaciente,
                    IdMedico = medico.IdMedico,
                    IdEstadoCita = estadoEspera,
                    FechaHora = DateTime.Now,
                    MotivoConsulta = "Consulta de prueba por dolor de cabeza",
                    HoraCheckIn = DateTime.Now
                });
                context.SaveChanges();
            }
        }
    }
}