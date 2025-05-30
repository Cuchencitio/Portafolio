using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TurnitoCL.Data;
using TurnitoCL.Models;
using TurnitoCL.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace TurnitoCL.Controllers
{
    public class AccountController : Controller
    {
        private readonly TurnitoDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(TurnitoDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Activo);

                if (usuario != null && BCrypt.Net.BCrypt.Verify(model.Password, usuario.PasswordHash))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                        new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                        new Claim(ClaimTypes.Email, usuario.Email),
                        new Claim("Rol", usuario.Rol)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("Usuario {Email} ha iniciado sesión como {Rol}", usuario.Email, usuario.Rol);

                    // Redirigir según el rol
                    if (usuario.Rol == Roles.Proveedor)
                    {
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Dashboard");
                    }
                }

                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            }

            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Verificar si el email ya existe
                var existeUsuario = await _context.Usuarios
                    .AnyAsync(u => u.Email == model.Email);

                if (existeUsuario)
                {
                    ModelState.AddModelError("Email", "Este email ya está registrado.");
                    return View(model);
                }

                // Crear nuevo usuario
                var usuario = new Usuario
                {
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Nombre = model.Nombre,
                    Apellido = model.Apellido,
                    Telefono = model.Telefono,
                    Rol = model.Rol,
                    FechaCreacion = DateTime.Now,
                    Activo = true
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // Si es proveedor, crear registro en tabla Proveedores
                if (model.Rol == Roles.Proveedor)
                {
                    var proveedor = new Proveedor
                    {
                        UsuarioId = usuario.Id,
                        NombreNegocio = model.NombreNegocio,
                        Direccion = model.Direccion,
                        Descripcion = model.Descripcion,
                        FechaCreacion = DateTime.Now
                    };

                    _context.Proveedores.Add(proveedor);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Nuevo usuario registrado: {Email} como {Rol}", usuario.Email, usuario.Rol);

                TempData["SuccessMessage"] = "Registro exitoso. Por favor, inicia sesión.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("Usuario ha cerrado sesión");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}