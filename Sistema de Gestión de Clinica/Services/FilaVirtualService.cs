
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Services
{
    public class FilaVirtualService
    {
        private readonly SistemaDeGestionDB _context;

        public FilaVirtualService(SistemaDeGestionDB context)
        {
            _context = context;
        }

        public async Task<FilaVirtualViewModel?> ObtenerEstadoFilaAsync(
            int idCita)
        {
            // 1. Buscar la cita solicitada.
            var cita = await _context.Cita
                .AsNoTracking()
                .Include(c => c.Medico)
                .Include(c => c.EstadoCita)
                .FirstOrDefaultAsync(c => c.IdCita == idCita);

            if (cita == null)
                return null;

            string estadoActual =
                cita.EstadoCita?.NombreEstado ?? "Sin estado";

            int pacientesAdelante = 0;

            // 2. Solo calcular la fila para citas pendientes.
            if (estadoActual == "Agendada" ||
                estadoActual == "En Espera")
            {
                DateTime inicioDia = cita.FechaHora.Date;
                DateTime finDia = inicioDia.AddDays(1);

                // 3. Contar las citas anteriores
                // del mismo médico y día.
                pacientesAdelante = await _context.Cita
                    .AsNoTracking()
                    .CountAsync(c =>
                        c.IdMedico == cita.IdMedico &&
                        c.IdCita != cita.IdCita &&
                        c.FechaHora >= inicioDia &&
                        c.FechaHora < finDia &&

                        (
                            c.FechaHora < cita.FechaHora ||

                            (c.FechaHora == cita.FechaHora &&
                             c.IdCita < cita.IdCita)
                        ) &&

                        c.EstadoCita != null &&

                        (
                            c.EstadoCita.NombreEstado == "Agendada" ||
                            c.EstadoCita.NombreEstado == "En Espera"
                        )
                    );
            }

            // 4. Obtener duración de consulta.
            int duracion =
                cita.Medico?.DuracionConsultaMin ?? 20;

            if (duracion <= 0)
                duracion = 20;

            // 5. Calcular tiempo estimado.
            int tiempoEstimado = pacientesAdelante * duracion;

            // 6. Devolver información a la vista.
            return new FilaVirtualViewModel
            {
                IdCita = cita.IdCita,

                NombreMedico =
                    $"Dr/a. {cita.Medico?.Nombre} " +
                    $"{cita.Medico?.Apellido}",

                PacientesAdelante = pacientesAdelante,

                TiempoEstimadoMinutos = tiempoEstimado,

                EstadoActual = estadoActual
            };
        }
    }
}
