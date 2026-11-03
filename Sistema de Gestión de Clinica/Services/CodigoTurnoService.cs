using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;

namespace Sistema_Gestión_Clinica.Services
{
    public static class CodigoTurnoService
    {
        public static async Task<string> GenerarAsync(
            SistemaDeGestionDB context)
        {
            var numeros = await context.Database
                .SqlQueryRaw<long>(
                    "SELECT NEXT VALUE FOR dbo.SecuenciaTurnos AS [Value]")
                .ToListAsync();

            long numero = numeros.Single();

            return $"TR-{numero:D6}";
        }
    }
}
