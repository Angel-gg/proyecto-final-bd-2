using System.Data;
using EcommerceApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// Sirve imágenes persistidas en Supabase PostgreSQL cuando no se provee clave de API REST externa.
    /// </summary>
    [AllowAnonymous]
    public class ImagenesController(ApplicationDbContext db) : Controller
    {
        [HttpGet("/Imagenes/{id}")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Obtener(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 80)
                return Redirect("/images/productos/placeholder.png");

            var conn = db.Database.GetDbConnection();
            var debeCerrar = conn.State != ConnectionState.Open;
            if (debeCerrar)
                await conn.OpenAsync();

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT ""ContentType"", ""Datos"" FROM catalogo.""ImagenesSubidas"" WHERE ""Id"" = @id LIMIT 1;";
                cmd.Parameters.Add(new NpgsqlParameter("id", id));

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var contentType = reader.GetString(0);
                    var datos = (byte[])reader[1];
                    return File(datos, contentType);
                }
            }
            finally
            {
                if (debeCerrar)
                    await conn.CloseAsync();
            }

            return Redirect("/images/productos/placeholder.png");
        }
    }
}
