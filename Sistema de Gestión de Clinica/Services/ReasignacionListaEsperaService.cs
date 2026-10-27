
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;

namespace Sistema_Gestión_Clinica.Services
{
    public class ReasignacionListaEsperaService
    {
        private readonly SistemaDeGestionDB _context;

        public ReasignacionListaEsperaService(
            SistemaDeGestionDB context)
        {
            _context = context;
        }

        public async Task ReasignarCupoLiberadoAsync(
            int idMedico,
            DateTime fechaHora)
        {
            // 1. No reasignar horarios que ya pasaron.
            if (fechaHora <= DateTime.Now)
                return;

            // 2. Buscar el médico y su especialidad.
            var medico = await _context.Medico
                .FirstOrDefaultAsync(m => m.IdMedico == idMedico);

            if (medico == null)
                return;

            // 3. Obtener el estado Agendada.
            var estadoAgendada = await _context.EstadoCita
                .FirstOrDefaultAsync(
                    e => e.NombreEstado == "Agendada");

            if (estadoAgendada == null)
                return;

            // 4. Verificar que el horario liberado
            // no tenga otra cita activa.
            int duracion = medico.DuracionConsultaMin > 0
                ? medico.DuracionConsultaMin
                : 20;

            DateTime finHorario = fechaHora.AddMinutes(duracion);

            var citasDelMedico = await _context.Cita
                .Include(c => c.EstadoCita)
                .Where(c =>
                    c.IdMedico == idMedico &&
                    c.FechaHora >= fechaHora.Date &&
                    c.FechaHora < fechaHora.Date.AddDays(1) &&
                    c.EstadoCita != null &&
                    c.EstadoCita.NombreEstado != "Cancelada" &&
                    c.EstadoCita.NombreEstado != "No-Show")
                .ToListAsync();

            bool horarioOcupado = citasDelMedico.Any(c =>
                c.FechaHora < finHorario &&
                c.FechaHora.AddMinutes(duracion) > fechaHora);

            if (horarioOcupado)
                return;

            // 5. Buscar solicitudes pendientes compatibles
            // con la especialidad, médico y fecha.
            var solicitudes = await _context.ListaEspera
                .Where(l =>
                    !l.Atendido &&
                    l.IdEspecialidad == medico.IdEspecialidad &&
                    (l.IdMedico == null ||
                     l.IdMedico == idMedico) &&
                    l.FechaDeseada.Date == fechaHora.Date)
                .OrderBy(l => l.Prioridad)
                .ThenBy(l => l.FechaRegistro)
                .ToListAsync();

            // 6. Buscar el primer paciente que pueda
            // recibir ese horario.
            foreach (var solicitud in solicitudes)
            {
                bool pacienteOcupado = await _context.Cita
                    .Include(c => c.EstadoCita)
                    .AnyAsync(c =>
                        c.IdPaciente == solicitud.IdPaciente &&
                        c.FechaHora == fechaHora &&
                        c.EstadoCita != null &&
                        c.EstadoCita.NombreEstado != "Cancelada" &&
                        c.EstadoCita.NombreEstado != "No-Show");

                if (pacienteOcupado)
                    continue;

                // 7. Crear una nueva cita.
                string codigo = "R-" +
                    Guid.NewGuid()
                        .ToString("N")
                        .Substring(0, 8)
                        .ToUpperInvariant();

                var nuevaCita = new Cita
                {
                    CodigoTurno = codigo,
                    IdPaciente = solicitud.IdPaciente,
                    IdMedico = idMedico,
                    IdEstadoCita = estadoAgendada.IdEstadoCita,
                    FechaHora = fechaHora,
                    MotivoConsulta =
                        "Cita reasignada desde lista de espera"
                };

                _context.Cita.Add(nuevaCita);

                // 8. Marcar la solicitud como atendida.
                solicitud.Atendido = true;

                // 9. Guardar ambos cambios.
                await _context.SaveChangesAsync();

                // Solo se asigna un paciente por cupo.
                return;
            }
        }
    }
}
