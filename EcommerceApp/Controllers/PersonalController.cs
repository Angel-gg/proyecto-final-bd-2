using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "GerenteGeneral,Admin")]
    public class PersonalController(UserManager<ApplicationUser> userManager, ApplicationDbContext db) : Controller
    {
        private static readonly string[] RolesDisponibles =
        {
            "GerenteGeneral", "GerenteRegional", "GerenteSucursal",
            "Vendedor", "Comercial", "Cliente", "Admin"
        };

        public async Task<IActionResult> Index(string? search, string? rol)
        {
            var usersQuery = userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.Email!.ToLower().Contains(s) ||
                    (u.NombreCompleto != null && u.NombreCompleto.ToLower().Contains(s)));
            }

            var users = await usersQuery.OrderByDescending(u => u.CreatedAt).ToListAsync();

            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var u in users)
            {
                userRoles[u.Id] = await userManager.GetRolesAsync(u);
            }

            if (!string.IsNullOrEmpty(rol) && rol != "Todos")
            {
                users = users.Where(u => userRoles[u.Id].Contains(rol)).ToList();
            }

            ViewBag.RolesDisponibles = RolesDisponibles;
            ViewBag.UserRoles = userRoles;
            ViewBag.Search = search;
            ViewBag.RolActual = rol;
            ViewBag.CurrentUserId = userManager.GetUserId(User);

            return View(users);
        }

        public async Task<IActionResult> EditarRol(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await userManager.GetRolesAsync(user);

            ViewBag.RolesDisponibles = RolesDisponibles;
            ViewBag.CurrentRoles = currentRoles;

            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarRoles(string id, List<string> selectedRoles)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            selectedRoles ??= new List<string>();
            var currentRoles = await userManager.GetRolesAsync(user);

            if (currentRoles.Contains("GerenteGeneral") && !selectedRoles.Contains("GerenteGeneral"))
            {
                var gerentes = await userManager.GetUsersInRoleAsync("GerenteGeneral");
                if (gerentes.Count <= 1)
                {
                    TempData["Error"] = "No puedes quitar el rol de GerenteGeneral al único usuario que lo tiene.";
                    return RedirectToAction(nameof(EditarRol), new { id = user.Id });
                }
            }

            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();

            if (rolesToRemove.Any())
                await userManager.RemoveFromRolesAsync(user, rolesToRemove);

            if (rolesToAdd.Any())
                await userManager.AddToRolesAsync(user, rolesToAdd);

            TempData["Exito"] = $"Roles actualizados para {user.Email}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUsuario(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Identificador de usuario inválido.";
                return RedirectToAction(nameof(Index));
            }

            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "El usuario especificado no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (user.Id == userManager.GetUserId(User))
            {
                TempData["Error"] = "No puedes eliminar tu propia cuenta mientras estás autenticado.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(user.Email) &&
                user.Email.EndsWith("@techparts.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "No se pueden eliminar cuentas institucionales @techparts.com.";
                return RedirectToAction(nameof(Index));
            }

            if (await db.Pedidos.AnyAsync(p => p.UserId == id))
            {
                TempData["Error"] = "No se puede eliminar el usuario porque tiene historial de pedidos registrado (integridad referencial RESTRICT).";
                return RedirectToAction(nameof(Index));
            }

            var carritos = await db.CarritosAbandonados
                .Where(c => c.UserId == id)
                .ToListAsync();
            if (carritos.Any())
            {
                db.CarritosAbandonados.RemoveRange(carritos);
                await db.SaveChangesAsync();
            }

            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Exito"] = "Usuario eliminado correctamente.";
            }
            else
            {
                TempData["Error"] = "No se pudo eliminar el usuario: " +
                    string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> BuscarUsuario(string q)
        {
            if (string.IsNullOrEmpty(q)) return Json(new List<object>());

            var s = q.ToLower();
            var users = await userManager.Users
                .Where(u => u.Email!.ToLower().Contains(s) || (u.NombreCompleto != null && u.NombreCompleto.ToLower().Contains(s)))
                .Take(10)
                .Select(u => new { u.Id, u.Email, u.NombreCompleto })
                .ToListAsync();

            return Json(users);
        }
    }
}
