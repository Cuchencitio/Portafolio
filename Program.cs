using Microsoft.EntityFrameworkCore;
using TurnitoCL.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Configurar Entity Framework
builder.Services.AddDbContext<TurnitoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

// Configurar autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireConsumidor", policy =>
        policy.RequireAuthenticatedUser().RequireClaim("Rol", "Consumidor"));

    options.AddPolicy("RequireProveedor", policy =>
        policy.RequireAuthenticatedUser().RequireClaim("Rol", "Proveedor"));
});

// Agregar servicios MVC
builder.Services.AddControllersWithViews();

// Configurar sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

// Usar sesiones
app.UseSession();

// Usar autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Configurar rutas
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Inicializar base de datos
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TurnitoDbContext>();
    try
    {
        // Crear la base de datos si no existe
        context.Database.EnsureCreated();
        Console.WriteLine("? Base de datos inicializada correctamente");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "? Error al inicializar la base de datos");
        Console.WriteLine($"? Error: {ex.Message}");
    }
}

app.Run();