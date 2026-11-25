using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "GerenteGeneral,GerenteRegional,GerenteSucursal,Vendedor,Comercial,Admin")]
    public class AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext db) : Controller
    {
        public async Task<IActionResult> Dashboard()
        {
            var user = await userManager.GetUserAsync(User);
            var roles = await userManager.GetRolesAsync(user!);

            ViewBag.NombreUsuario = user?.NombreCompleto ?? user?.Email;
            ViewBag.Rol = roles.FirstOrDefault() ?? "Sin rol";
            ViewBag.SucursalId = user?.SucursalId;
            ViewBag.RegionId = user?.RegionId;

            var hoy = DateTime.UtcNow.Date;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // KPIs básicos
            var totalInventario = await db.Componentes.SumAsync(c => c.Stock);
            var ventasMes = await db.Pedidos
                .Where(p => p.FechaPedido >= inicioMes &&
                            (p.Estado == EstadoPedido.Completado || p.Estado == EstadoPedido.Entregado))
                .SumAsync(p => p.Total);
            var clientes = await userManager.GetUsersInRoleAsync("Cliente");
            var sucursales = await db.Sucursales.CountAsync(s => s.Activa);

            ViewBag.TotalInventario = totalInventario;
            ViewBag.VentasMes = ventasMes;
            ViewBag.TotalClientes = clientes.Count;
            ViewBag.TotalSucursales = sucursales;

            // Alertas
            var stockCritico = await db.Componentes
                .Include(c => c.Categoria)
                .Where(c => c.Stock <= c.StockMinimo)
                .OrderBy(c => c.Stock)
                .Take(5)
                .ToListAsync();
            var carritosActivos = await db.CarritosAbandonados.CountAsync(c => !c.Convertido);

            ViewBag.StockCritico = stockCritico;
            ViewBag.CarritosActivos = carritosActivos;

            // Últimas 5 ventas
            var ultimasVentas = await db.Pedidos
                .Include(p => p.Usuario)
                .OrderByDescending(p => p.FechaPedido)
                .Take(5)
                .ToListAsync();
            ViewBag.UltimasVentas = ultimasVentas;

            // Datos para Gráfico Líneas (6 meses)
            var ventasMeses = new List<decimal>();
            var labelsMeses = new List<string>();
            for (int i = 5; i >= 0; i--)
            {
                var m = hoy.AddMonths(-i);
                var start = new DateTime(m.Year, m.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var end = start.AddMonths(1);
                var sum = await db.Pedidos
                    .Where(p => p.FechaPedido >= start && p.FechaPedido < end &&
                                (p.Estado == EstadoPedido.Completado || p.Estado == EstadoPedido.Entregado))
                    .SumAsync(p => p.Total);
                ventasMeses.Add(sum);
                labelsMeses.Add(start.ToString("MMM yyyy"));
            }
            ViewBag.LabelsVentas = labelsMeses;
            ViewBag.DatosVentas = ventasMeses;

            // Datos para Gráfico Barras (Top 5 Productos)
            var topProductos = await db.PedidoItems
                .GroupBy(i => i.ProductoNombre)
                .Select(g => new { Nombre = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .OrderByDescending(g => g.Cantidad)
                .Take(5)
                .ToListAsync();
            ViewBag.LabelsTop = topProductos.Select(p => p.Nombre).ToList();
            ViewBag.DatosTop = topProductos.Select(p => p.Cantidad).ToList();

            // Datos para Dona (Métodos de pago)
            var metodos = await db.Pedidos
                .Where(p => p.Estado == EstadoPedido.Completado || p.Estado == EstadoPedido.Entregado)
                .GroupBy(p => p.MetodoPago)
                .Select(g => new { Metodo = g.Key, Total = g.Count() })
                .ToListAsync();
            ViewBag.LabelsMetodos = metodos.Select(m => m.Metodo.ToString()).ToList();
            ViewBag.DatosMetodos = metodos.Select(m => m.Total).ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SincronizarBD()
        {
            try
            {
                if (!await db.Database.CanConnectAsync())
                {
                    TempData["Error"] = "No hay conexión activa con Supabase PostgreSQL.";
                    return RedirectToAction(nameof(Dashboard));
                }

                if (!await db.Regiones.AnyAsync())
                {
                    db.Regiones.AddRange(
                        new Region { Nombre = "Cochabamba", Codigo = "CBB" },
                        new Region { Nombre = "La Paz",     Codigo = "LPZ" },
                        new Region { Nombre = "Santa Cruz", Codigo = "SCZ" },
                        new Region { Nombre = "Oruro",      Codigo = "ORU" },
                        new Region { Nombre = "Tarija",     Codigo = "TJA" }
                    );
                    await db.SaveChangesAsync();
                }

                if (!await db.Sucursales.AnyAsync())
                {
                    var regCbb = await db.Regiones.FirstOrDefaultAsync(r => r.Codigo == "CBB");
                    db.Sucursales.AddRange(
                        new Sucursal { Nombre = "Sucursal Centro",     Direccion = "Plaza Principal 14 de Septiembre", Ciudad = "Cochabamba", Estado = "Cochabamba", RegionId = regCbb?.Id, Telefono = "44556677", Email = "centro@techparts.com.bo", Activa = true },
                        new Sucursal { Nombre = "Sucursal Norte",      Direccion = "Av. América y Libertador",         Ciudad = "Cochabamba", Estado = "Cochabamba", RegionId = regCbb?.Id, Telefono = "44556678", Email = "norte@techparts.com.bo", Activa = true },
                        new Sucursal { Nombre = "Sucursal Sur",        Direccion = "Av. Panamericana esq. 6 de Agosto",Ciudad = "Cochabamba", Estado = "Cochabamba", RegionId = regCbb?.Id, Telefono = "44556679", Email = "sur@techparts.com.bo", Activa = true }
                    );
                    await db.SaveChangesAsync();
                }

                TempData["Exito"] = "Conexión multiesquema 3FN con Supabase verificada y datos base sincronizados.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                return Content($"Error al sincronizar BD: {ex.Message} \n\nDetalles: {ex.InnerException?.Message ?? ex.StackTrace}");
            }
        }
    }
}
