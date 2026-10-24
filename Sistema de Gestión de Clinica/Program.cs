using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configuración del DbContext con SQL Server
builder.Services.AddDbContext<SistemaDeGestionDB>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
        }
        ));

// ----------------------------------------------------------------------
// Registrar Servicios Personalizados para Inyección de Dependencias
// ----------------------------------------------------------------------
builder.Services.AddScoped<FilaVirtualService>();
builder.Services.AddScoped<ReasignacionListaEsperaService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Ruta por defecto apuntando al Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
