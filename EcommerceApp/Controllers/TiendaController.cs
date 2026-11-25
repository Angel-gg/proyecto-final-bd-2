using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Text.Json;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// Tienda web (E-Commerce): Catálogo 3FN + Carrito + Checkout transaccional (sp_confirmar_venta) + Factura + Mis Pedidos.
    /// </summary>
    public class TiendaController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager) : Controller
    {
        private const string CarritoKey = "carrito_techparts";

        // ─── Catálogo ─────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? tipo, string? tab)
        {
            tab ??= "componentes";
            ViewBag.Tab = tab;

            var componentesQuery = db.Componentes
                .Include(c => c.Categoria)
                .Include(c => c.Marca)
                .Where(c => c.Activo)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(tipo))
                componentesQuery = componentesQuery.Where(c => c.Categoria != null && c.Categoria.Nombre == tipo);

            var componentes = await componentesQuery
                .OrderBy(c => c.Categoria!.Nombre)
                .ThenBy(c => c.Nombre)
                .ToListAsync();

            var combos = await db.Combos
                .Include(c => c.ComboComponentes)
                    .ThenInclude(cc => cc.Componente)
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var tipos = await db.Categorias
                .Where(c => c.Activa)
                .OrderBy(c => c.Nombre)
                .Select(c => c.Nombre)
                .ToListAsync();

            ViewBag.Tipos        = tipos;
            ViewBag.TipoActual   = tipo;
            ViewBag.Combos       = combos;
            ViewBag.CarritoCount = ObtenerCarrito().TotalItems;

            return View(componentes);
        }

        // ─── Carrito ──────────────────────────────────────────────────────────

        [Authorize]
        public IActionResult Carrito()
        {
            var carrito = ObtenerCarrito();
            return View(carrito);
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> AgregarAlCarrito(int id, string tipo = "componente")
        {
            var carrito = ObtenerCarrito();
            var existente = carrito.Items.FirstOrDefault(i => i.Tipo == tipo && i.Id == id);

            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                if (tipo == "componente")
                {
                    var comp = await db.Componentes.FindAsync(id);
                    if (comp == null) return NotFound();
                    if (comp.Stock == 0)
                    {
                        TempData["Error"] = $"\"{comp.Nombre}\" está agotado.";
                        return RedirectToAction(nameof(Index));
                    }
                    carrito.Items.Add(new CarritoItem
                    {
                        Id = comp.Id, Nombre = comp.Nombre, Precio = comp.Precio,
                        Cantidad = 1, ImageUrl = comp.ImageUrl, Tipo = "componente"
                    });
                }
                else if (tipo == "combo")
                {
                    var combo = await db.Combos.FindAsync(id);
                    if (combo == null) return NotFound();
                    carrito.Items.Add(new CarritoItem
                    {
                        Id = combo.Id, Nombre = combo.Nombre, Precio = combo.PrecioVenta,
                        Cantidad = 1, ImageUrl = combo.ImageUrl, Tipo = "combo"
                    });
                }
            }

            GuardarCarrito(carrito);
            await RegistrarCarritoAbandonado(carrito);
            TempData["Exito"] = "Producto agregado al carrito ✓";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> QuitarDelCarrito(int id, string tipo)
        {
            var carrito = ObtenerCarrito();
            var item = carrito.Items.FirstOrDefault(i => i.Id == id && i.Tipo == tipo);
            if (item != null)
            {
                if (item.Cantidad > 1) item.Cantidad--;
                else carrito.Items.Remove(item);
            }
            GuardarCarrito(carrito);
            await RegistrarCarritoAbandonado(carrito);
            return RedirectToAction(nameof(Carrito));
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> EliminarDelCarrito(int id, string tipo)
        {
            var carrito = ObtenerCarrito();
            carrito.Items.RemoveAll(i => i.Id == id && i.Tipo == tipo);
            GuardarCarrito(carrito);
            await RegistrarCarritoAbandonado(carrito);
            return RedirectToAction(nameof(Carrito));
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> VaciarCarrito()
        {
            HttpContext.Session.Remove(CarritoKey);
            await RegistrarCarritoAbandonado(new CarritoViewModel());
            TempData["Exito"] = "Carrito vaciado.";
            return RedirectToAction(nameof(Carrito));
        }

        [HttpGet]
        public IActionResult CarritoCount()
        {
            var carrito = ObtenerCarrito();
            return Json(new { count = carrito.TotalItems });
        }

        // ─── Checkout (invoca procedimiento transaccional ventas.sp_confirmar_venta) ───

        [Authorize]
        public IActionResult Checkout()
        {
            var carrito = ObtenerCarrito();
            if (!carrito.Items.Any())
            {
                TempData["Error"] = "Tu carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }
            ViewBag.Carrito = carrito;
            return View();
        }

        [HttpPost, Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago(MetodoPago metodoPago, string? notas)
        {
            var carrito = ObtenerCarrito();
            if (!carrito.Items.Any())
            {
                TempData["Error"] = "Tu carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var fechaHoy  = DateTime.UtcNow;
            var numPedido = $"TPC-{fechaHoy:yyyyMMdd-HHmmss}-{Random.Shared.Next(100, 999)}";
            var sucursalId = user.SucursalId ??
                await db.Sucursales.Where(s => s.Activa).Select(s => (int?)s.Id).FirstOrDefaultAsync();

            var itemsPayload = carrito.Items.Select(i => new
            {
                tipo = i.Tipo,
                id = i.Id,
                cantidad = i.Cantidad
            });
            var itemsJson = JsonSerializer.Serialize(itemsPayload);

            try
            {
                var conn = (NpgsqlConnection)db.Database.GetDbConnection();
                bool debeCerrar = conn.State != ConnectionState.Open;
                if (debeCerrar) await conn.OpenAsync();

                int pedidoId;
                try
                {
                    await using var cmd = new NpgsqlCommand(
                        "CALL ventas.sp_confirmar_venta(@p_user_id, @p_numero_pedido, @p_items, @p_metodo_pago, @p_sucursal_id, @p_notas, @p_pedido_id)",
                        conn);

                    cmd.Parameters.Add(new NpgsqlParameter("p_user_id", NpgsqlDbType.Text) { Value = user.Id });
                    cmd.Parameters.Add(new NpgsqlParameter("p_numero_pedido", NpgsqlDbType.Varchar) { Value = numPedido });
                    cmd.Parameters.Add(new NpgsqlParameter("p_items", NpgsqlDbType.Jsonb) { Value = itemsJson });
                    cmd.Parameters.Add(new NpgsqlParameter("p_metodo_pago", NpgsqlDbType.Smallint) { Value = (short)metodoPago });
                    cmd.Parameters.Add(new NpgsqlParameter("p_sucursal_id", NpgsqlDbType.Integer) { Value = (object?)sucursalId ?? DBNull.Value });
                    cmd.Parameters.Add(new NpgsqlParameter("p_notas", NpgsqlDbType.Text) { Value = (object?)notas ?? DBNull.Value });

                    var outPedidoId = new NpgsqlParameter("p_pedido_id", NpgsqlDbType.Integer)
                    {
                        Direction = ParameterDirection.InputOutput,
                        Value = DBNull.Value
                    };
                    cmd.Parameters.Add(outPedidoId);

                    await cmd.ExecuteNonQueryAsync();
                    pedidoId = Convert.ToInt32(outPedidoId.Value);
                }
                finally
                {
                    if (debeCerrar) await conn.CloseAsync();
                }

                HttpContext.Session.Remove(CarritoKey);
                TempData["Exito"] = $"¡Compra completada con integridad transaccional! Pedido {numPedido}";
                return RedirectToAction(nameof(Factura), new { id = pedidoId });
            }
            catch (PostgresException pgEx)
            {
                TempData["Error"] = $"No se pudo confirmar la compra (ROLLBACK): {pgEx.MessageText}";
                return RedirectToAction(nameof(Checkout));
            }
        }

        // ─── Factura / Comprobante ─────────────────────────────────────────────

        [Authorize]
        public async Task<IActionResult> Factura(int id)
        {
            var userId = userManager.GetUserId(User);
            bool isStaff = User.IsInRole("GerenteGeneral") || User.IsInRole("Admin") ||
                           User.IsInRole("GerenteSucursal") || User.IsInRole("GerenteRegional");

            var pedido = await db.Pedidos
                .Include(p => p.Items)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id && (p.UserId == userId || isStaff));

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // ─── Mis Pedidos ──────────────────────────────────────────────────────

        [Authorize]
        public async Task<IActionResult> MisPedidos()
        {
            var userId = userManager.GetUserId(User);
            bool isStaff = User.IsInRole("GerenteGeneral") || User.IsInRole("Admin") ||
                           User.IsInRole("GerenteSucursal") || User.IsInRole("GerenteRegional") ||
                           User.IsInRole("Vendedor") || User.IsInRole("Comercial");

            IQueryable<Pedido> query = db.Pedidos.Include(p => p.Items).Include(p => p.Usuario);
            if (!isStaff)
                query = query.Where(p => p.UserId == userId);

            var pedidos = await query.OrderByDescending(p => p.FechaPedido).ToListAsync();
            ViewBag.IsStaff = isStaff;
            return View(pedidos);
        }

        [HttpPost, Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarDevolucion(int id)
        {
            var userId = userManager.GetUserId(User);
            bool isStaff = User.IsInRole("GerenteGeneral") || User.IsInRole("Admin");

            var pedido = await db.Pedidos
                .FirstOrDefaultAsync(p => p.Id == id && (p.UserId == userId || isStaff));

            if (pedido == null) return NotFound();
            if (pedido.Estado == EstadoPedido.Devuelto)
            {
                TempData["Error"] = "Este pedido ya fue devuelto.";
            }
            else
            {
                pedido.Estado = EstadoPedido.Devuelto;
                await db.SaveChangesAsync();
                TempData["Exito"] = $"Devolución registrada para el pedido {pedido.NumeroPedido}.";
            }
            return RedirectToAction(nameof(MisPedidos));
        }

        // ─── Helpers privados ─────────────────────────────────────────────────

        private CarritoViewModel ObtenerCarrito()
        {
            var json = HttpContext.Session.GetString(CarritoKey);
            if (string.IsNullOrEmpty(json)) return new CarritoViewModel();
            return JsonSerializer.Deserialize<CarritoViewModel>(json) ?? new CarritoViewModel();
        }

        private void GuardarCarrito(CarritoViewModel carrito)
        {
            var json = JsonSerializer.Serialize(carrito);
            HttpContext.Session.SetString(CarritoKey, json);
        }

        private async Task RegistrarCarritoAbandonado(CarritoViewModel carrito)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return;

            var userId = userManager.GetUserId(User);
            if (userId == null) return;

            var existente = await db.CarritosAbandonados
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.Convertido);

            var itemsJson = JsonSerializer.Serialize(carrito.Items);

            if (existente == null)
            {
                if (carrito.Items.Any())
                {
                    db.CarritosAbandonados.Add(new CarritoAbandonado
                    {
                        UserId               = userId,
                        FechaCreacion        = DateTime.UtcNow,
                        FechaUltimaActividad = DateTime.UtcNow,
                        FechaExpira          = DateTime.UtcNow.AddDays(7),
                        ItemsJson            = itemsJson,
                        TotalEstimado        = carrito.Total,
                        TotalItems           = carrito.TotalItems,
                        Convertido           = false
                    });
                }
            }
            else
            {
                existente.FechaUltimaActividad = DateTime.UtcNow;
                existente.ItemsJson            = itemsJson;
                existente.TotalEstimado        = carrito.Total;
                existente.TotalItems           = carrito.TotalItems;
                db.CarritosAbandonados.Update(existente);
            }

            try { await db.SaveChangesAsync(); }
            catch { /* no bloquear si falla el tracking */ }
        }
    }
}
