using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// CRUD de Combos — paquetes de componentes con precio especial.
    /// </summary>
    [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Comercial,Admin")]
    public class CombosController(ApplicationDbContext db, ISupabaseStorageService storage) : Controller
    {
        private async Task CargarComponentesAsync()
        {
            ViewBag.Componentes = await db.Componentes
                .Include(c => c.Categoria)
                .Include(c => c.Marca)
                .OrderBy(c => c.Categoria!.Nombre)
                .ThenBy(c => c.Nombre)
                .ToListAsync();
        }

        // GET /Combos
        public async Task<IActionResult> Index()
        {
            var combos = await db.Combos
                .Include(c => c.ComboComponentes)
                    .ThenInclude(cc => cc.Componente)
                        .ThenInclude(comp => comp!.Categoria)
                .OrderBy(c => c.Nombre)
                .ToListAsync();
            return View(combos);
        }

        // GET /Combos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var combo = await db.Combos
                .Include(c => c.ComboComponentes)
                    .ThenInclude(cc => cc.Componente)
                        .ThenInclude(comp => comp!.Categoria)
                .Include(c => c.ComboComponentes)
                    .ThenInclude(cc => cc.Componente)
                        .ThenInclude(comp => comp!.Marca)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (combo == null) return NotFound();
            return View(combo);
        }

        // GET /Combos/Create
        public async Task<IActionResult> Create()
        {
            await CargarComponentesAsync();
            return View();
        }

        // POST /Combos/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Combo combo, int[] ComponenteIds, int[] Cantidades, IFormFile? ImagenArchivo)
        {
            ModelState.Remove("ComboComponentes");

            if (ImagenArchivo != null && ImagenArchivo.Length > 0)
            {
                try
                {
                    combo.ImageUrl = await storage.SubirImagenAsync(ImagenArchivo, "combos");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("ImageUrl", ex.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarComponentesAsync();
                return View(combo);
            }

            combo.CreadoEn = DateTime.UtcNow;
            db.Combos.Add(combo);
            await db.SaveChangesAsync();

            for (int i = 0; i < ComponenteIds.Length; i++)
            {
                db.ComboComponentes.Add(new ComboComponente
                {
                    ComboId      = combo.Id,
                    ComponenteId = ComponenteIds[i],
                    Cantidad     = i < Cantidades.Length ? Cantidades[i] : 1
                });
            }
            await db.SaveChangesAsync();

            TempData["Exito"] = $"Combo \"{combo.Nombre}\" creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Combos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var combo = await db.Combos
                .Include(c => c.ComboComponentes)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (combo == null) return NotFound();

            await CargarComponentesAsync();
            return View(combo);
        }

        // POST /Combos/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Combo combo, int[] ComponenteIds, int[] Cantidades, IFormFile? ImagenArchivo)
        {
            if (id != combo.Id) return NotFound();
            ModelState.Remove("ComboComponentes");

            if (ImagenArchivo != null && ImagenArchivo.Length > 0)
            {
                try
                {
                    combo.ImageUrl = await storage.SubirImagenAsync(ImagenArchivo, "combos");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("ImageUrl", ex.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarComponentesAsync();
                return View(combo);
            }

            combo.ActualizadoEn = DateTime.UtcNow;
            db.Combos.Update(combo);

            var existentes = db.ComboComponentes.Where(cc => cc.ComboId == id);
            db.ComboComponentes.RemoveRange(existentes);

            for (int i = 0; i < ComponenteIds.Length; i++)
            {
                db.ComboComponentes.Add(new ComboComponente
                {
                    ComboId      = id,
                    ComponenteId = ComponenteIds[i],
                    Cantidad     = i < Cantidades.Length ? Cantidades[i] : 1
                });
            }
            await db.SaveChangesAsync();

            TempData["Exito"] = $"Combo \"{combo.Nombre}\" actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Combos/Delete/5
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var combo = await db.Combos
                .Include(c => c.ComboComponentes).ThenInclude(cc => cc.Componente)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (combo == null) return NotFound();
            return View(combo);
        }

        // POST /Combos/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var combo = await db.Combos.FindAsync(id);
            if (combo != null)
            {
                db.Combos.Remove(combo);
                await db.SaveChangesAsync();
                TempData["Exito"] = "Combo eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
