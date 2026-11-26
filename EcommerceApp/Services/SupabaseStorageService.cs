using System.Data;
using System.Net.Http.Headers;
using EcommerceApp.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EcommerceApp.Services
{
    public interface ISupabaseStorageService
    {
        Task<string> SubirImagenAsync(IFormFile archivo, string subcarpeta);
    }

    /// <summary>
    /// Servicio de almacenamiento de imágenes para catálogo (Componentes y Combos).
    /// Sube al bucket público de Supabase Storage cuando Supabase:ApiKey está configurada,
    /// y mantiene respaldo automático persistente en Supabase PostgreSQL (catalogo."ImagenesSubidas")
    /// para que ninguna imagen se pierda ante reinicios efímeros en Render.
    /// </summary>
    public class SupabaseStorageService(
        HttpClient http,
        IConfiguration config,
        ApplicationDbContext db,
        ILogger<SupabaseStorageService> logger) : ISupabaseStorageService
    {
        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".webp", ".gif"
        };

        private const long TamanoMaximoBytes = 5 * 1024 * 1024; // 5 MB

        public async Task<string> SubirImagenAsync(IFormFile archivo, string subcarpeta)
        {
            if (archivo == null || archivo.Length == 0)
                throw new InvalidOperationException("El archivo de imagen está vacío.");

            if (archivo.Length > TamanoMaximoBytes)
                throw new InvalidOperationException("La imagen supera el tamaño máximo permitido (5 MB).");

            var ext = Path.GetExtension(archivo.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !ExtensionesPermitidas.Contains(ext))
                throw new InvalidOperationException("Formato no válido. Usa imágenes JPG, PNG, WEBP o GIF.");

            var contentType = ext.ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };

            await using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var objectPath = $"{subcarpeta.Trim('/')}/{fileName}";

            var supabaseUrl = config["Supabase:Url"]?.TrimEnd('/');
            var bucket = string.IsNullOrWhiteSpace(config["Supabase:Bucket"]) ? "productos" : config["Supabase:Bucket"]!.Trim();
            var apiKey = config["Supabase:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = Environment.GetEnvironmentVariable("SUPABASE_API_KEY");

            // 1. Subida directa a Supabase Storage REST API si hay ApiKey configurada
            if (!string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var uploadUrl = $"{supabaseUrl}/storage/v1/object/{bucket}/{objectPath}";
                    using var req = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    req.Headers.Add("apikey", apiKey);
                    req.Headers.Add("x-upsert", "true");

                    var byteContent = new ByteArrayContent(bytes);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                    req.Content = byteContent;

                    using var resp = await http.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                    {
                        return $"{supabaseUrl}/storage/v1/object/public/{bucket}/{objectPath}";
                    }

                    var body = await resp.Content.ReadAsStringAsync();
                    logger.LogWarning("Supabase Storage devolvió {Status}: {Body}. Usando respaldo persistente en Supabase DB.",
                        (int)resp.StatusCode, body);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Fallo al conectar con Supabase Storage API. Usando respaldo persistente en Supabase DB.");
                }
            }

            // 2. Respaldo persistente en Supabase PostgreSQL (sobrevive reinicios de Render sin requerir ApiKey extra)
            var conn = db.Database.GetDbConnection();
            var debeCerrar = conn.State != ConnectionState.Open;
            if (debeCerrar)
                await conn.OpenAsync();

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO catalogo.""ImagenesSubidas"" (""Id"", ""ContentType"", ""Datos"", ""CreadoEn"")
                    VALUES (@id, @ct, @datos, NOW())
                    ON CONFLICT (""Id"") DO UPDATE
                       SET ""ContentType"" = EXCLUDED.""ContentType"",
                           ""Datos"" = EXCLUDED.""Datos"";";
                cmd.Parameters.Add(new NpgsqlParameter("id", fileName));
                cmd.Parameters.Add(new NpgsqlParameter("ct", contentType));
                cmd.Parameters.Add(new NpgsqlParameter("datos", bytes));
                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                if (debeCerrar)
                    await conn.CloseAsync();
            }

            return $"/Imagenes/{fileName}";
        }
    }
}
