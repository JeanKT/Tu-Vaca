using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using TuVaca.Data;
using TuVaca.Seguridad;

// Reportes PDF (QuestPDF): licencia Community, gratuita para proyectos académicos
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Base de datos TuVacaDB (cadena "TuVacaDB" en appsettings.json)
builder.Services.AddDbContext<TuVacaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TuVacaDB")));

// CU-01: inicio de sesión con cookies.
// RN-6: en cada solicitud se revisa en la tabla Sesiones que la sesión siga activa (EventosSesion).
builder.Services.AddScoped<EventosSesion>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccesoDenegado";
        options.Cookie.Name = "TuVaca.Sesion";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.EventsType = typeof(EventosSesion);
    });
builder.Services.AddAuthorization();

// Servicios de los demás casos de uso (cada rama agrega aquí su línea):
builder.Services.AddScoped<TuVaca.Servicios.NotificacionService>();
builder.Services.AddScoped<TuVaca.Servicios.CorreoService>();
builder.Services.AddScoped<TuVaca.Servicios.ReporteService>();

var app = builder.Build();

// Los campos numéricos del navegador envían el decimal con punto ("350.5").
// Se usa español de Colombia, pero con punto decimal, para que el peso y el precio se lean bien.
var cultura = (CultureInfo)CultureInfo.GetCultureInfo("es-CO").Clone();
cultura.NumberFormat.NumberDecimalSeparator = ".";
cultura.NumberFormat.NumberGroupSeparator = ",";
cultura.NumberFormat.CurrencyDecimalSeparator = ".";
cultura.NumberFormat.CurrencyGroupSeparator = ",";
var localizacion = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = new List<CultureInfo> { cultura },
    SupportedUICultures = new List<CultureInfo> { cultura }
};
localizacion.RequestCultureProviders.Clear();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRequestLocalization(localizacion);
app.UseHttpsRedirection();
app.UseStaticFiles();   // también sirve las fotos que se suben a wwwroot/uploads
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
