using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar servicios MVC
builder.Services.AddControllersWithViews();

// 2. Configuración del DbContext con SQL Server
builder.Services.AddDbContext<SistemaDeGestionDB>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
        }
    ));

// 3. Configuración del esquema de Autenticación mediante Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// 4. Registrar Servicios Personalizados para Inyección de Dependencias
builder.Services.AddScoped<FilaVirtualService>();
builder.Services.AddScoped<ReasignacionListaEsperaService>();

var app = builder.Build();

// 5. Inicializar la Base de Datos automáticamente (Crea BD, tablas y datos iniciales)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SistemaDeGestionDB>();

        // Fuerza la creación física de la BD y sus tablas antes de cualquier consulta
        context.Database.EnsureCreated();

        // Poblar la base de datos con roles, estados y usuarios encriptados
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error crítico al crear o inicializar la base de datos.");

        // Lanza la excepción en desarrollo para visibilizar errores de conexión a SQL Server
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 6. Middleware de Autenticación y Autorización (EL ORDEN ES VITAL)
app.UseAuthentication();
app.UseAuthorization();

// 7. Ruta por defecto apuntando al Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();