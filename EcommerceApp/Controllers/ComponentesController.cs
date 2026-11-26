using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// CRUD de Componentes de hardware normalizado en 3FN — acceso restringido a roles de gestión.
    /// </summary>
    [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Vendedor,Comercial,Admin")]
    public class ComponentesController(ApplicationDbContext db, ISupabaseStorageService storage) : Controller
    {
        private async Task CargarCatalogosAsync()
        {
            ViewBag.Categorias = await db.Categorias.Where(c => c.Activa).OrderBy(c => c.Nombre).ToListAsync();
            ViewBag.Marcas = await db.Marcas.Where(m => m.Activa).OrderBy(m => m.Nombre).ToListAsync();
            ViewBag.Proveedores = await db.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        }

        // GET /Componentes
        public async Task<IActionResult> Index(string? tipo, string? busqueda)
        {
            var query = db.Componentes
                .Include(c => c.Categoria)
                .Include(c => c.Marca)
                .Include(c => c.Proveedor)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tipo))
                query = query.Where(c => c.Categoria != null && c.Categoria.Nombre == tipo);

            if (!string.IsNullOrWhiteSpace(busqueda))
                query = query.Where(c =>
                    c.Nombre.Contains(busqueda) ||
                    (c.Marca != null && c.Marca.Nombre.Contains(busqueda)));

            var componentes = await query
                .OrderBy(c => c.Categoria!.Nombre)
                .ThenBy(c => c.Nombre)
                .ToListAsync();

            var tipos = await db.Categorias
                .Where(c => c.Activa)
                .OrderBy(c => c.Nombre)
                .Select(c => c.Nombre)
                .ToListAsync();

            ViewBag.Tipos = tipos;
            ViewBag.TipoActual = tipo;
            ViewBag.Busqueda = busqueda;

            return View(componentes);
        }

        // GET /Componentes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var comp = await db.Componentes
                .Include(c => c.Categoria)
                .Include(c => c.Marca)
                .Include(c => c.Proveedor)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (comp == null) return NotFound();
            return View(comp);
        }

        // GET /Componentes/Create
        [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Comercial,Admin")]
        public async Task<IActionResult> Create()
        {
            await CargarCatalogosAsync();
            return View(new Componente());
        }

        // POST /Componentes/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Comercial,Admin")]
        public async Task<IActionResult> Create(Componente componente, IFormFile? ImagenArchivo)
        {
            ModelState.Remove("Categoria");
            ModelState.Remove("Marca");
            ModelState.Remove("Proveedor");
            ModelState.Remove("ComboComponentes");

            if (ImagenArchivo != null && ImagenArchivo.Length > 0)
            {
                try
                {
                    componente.ImageUrl = await storage.SubirImagenAsync(ImagenArchivo, "componentes");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("ImageUrl", ex.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarCatalogosAsync();
                return View(componente);
            }

            componente.CreadoEn = DateTime.UtcNow;
            db.Componentes.Add(componente);
            await db.SaveChangesAsync();
            TempData["Exito"] = $"Componente \"{componente.Nombre}\" creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Componentes/Edit/5
        [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Comercial,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var comp = await db.Componentes.FindAsync(id);
            if (comp == null) return NotFound();
            await CargarCatalogosAsync();
            return View(comp);
        }

        // POST /Componentes/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Comercial,Admin")]
        public async Task<IActionResult> Edit(int id, Componente componente, IFormFile? ImagenArchivo)
        {
            if (id != componente.Id) return NotFound();

            ModelState.Remove("Categoria");
            ModelState.Remove("Marca");
            ModelState.Remove("Proveedor");
            ModelState.Remove("ComboComponentes");

            if (ImagenArchivo != null && ImagenArchivo.Length > 0)
            {
                try
                {
                    componente.ImageUrl = await storage.SubirImagenAsync(ImagenArchivo, "componentes");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("ImageUrl", ex.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarCatalogosAsync();
                return View(componente);
            }

            componente.ActualizadoEn = DateTime.UtcNow;
            db.Componentes.Update(componente);
            await db.SaveChangesAsync();
            TempData["Exito"] = $"Componente \"{componente.Nombre}\" actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Componentes/Delete/5
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var comp = await db.Componentes
                .Include(c => c.Categoria)
                .Include(c => c.Marca)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (comp == null) return NotFound();
            return View(comp);
        }

        // POST /Componentes/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comp = await db.Componentes.FindAsync(id);
            if (comp != null)
            {
                db.Componentes.Remove(comp);
                await db.SaveChangesAsync();
                TempData["Exito"] = "Componente eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
