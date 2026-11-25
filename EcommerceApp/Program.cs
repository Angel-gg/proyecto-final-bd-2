using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

// Compatibilidad de DateTime con PostgreSQL en Npgsql 6+
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// Habilitar sesiones (para el carrito de compras del E-Commerce)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ─── Encabezados de seguridad y permisos ─────────────────────────────────────
// Permite el uso del micrófono (Web Speech API) en producción
app.Use(async (context, next) =>
{
    context.Response.Headers["Permissions-Policy"] = "microphone=*, camera=()";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ─── Verificación de conexión y seeding seguro (Supabase BD2) ────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // No ejecutar MigrateAsync() sobre el esquema administrado por script_bd2_completo.sql
    if (!await db.Database.CanConnectAsync())
    {
        Console.WriteLine("[BD] No se pudo conectar a PostgreSQL/Supabase. Se omite el seeding inicial.");
    }
    else
    {
        Console.WriteLine("[BD] Conexión a Supabase verificada correctamente.");

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Sembrar Regiones si está vacía
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
            Console.WriteLine("[SEED] Regiones creadas.");
        }

        var regionCbb = await db.Regiones.FirstOrDefaultAsync(r => r.Codigo == "CBB");
        var regionLpz = await db.Regiones.FirstOrDefaultAsync(r => r.Codigo == "LPZ");
        var regionScz = await db.Regiones.FirstOrDefaultAsync(r => r.Codigo == "SCZ");

        // 2. Sembrar Sucursales si está vacía
        if (!await db.Sucursales.AnyAsync())
        {
            db.Sucursales.AddRange(
                new Sucursal { Nombre = "Sucursal Centro",     Direccion = "Plaza Principal 14 de Septiembre", Ciudad = "Cochabamba", Estado = "Cochabamba", RegionId = regionCbb?.Id, Telefono = "44556677", Email = "centro@techparts.com.bo",    Responsable = "Roberto Jiménez Cruz", Activa = true },
                new Sucursal { Nombre = "Sucursal Norte",      Direccion = "Av. América y Libertador",         Ciudad = "Cochabamba", Estado = "Cochabamba", RegionId = regionCbb?.Id, Telefono = "44556678", Email = "norte@techparts.com.bo",     Responsable = "Laura Sánchez Morales", Activa = true },
                new Sucursal { Nombre = "Sucursal Sur",        Direccion = "Av. Panamericana esq. 6 de Agosto",Ciudad = "Cochabamba", Estado = "Cochabamba", RegionId = regionCbb?.Id, Telefono = "44556679", Email = "sur@techparts.com.bo",       Responsable = "Marco Antonio Torrico", Activa = true },
                new Sucursal { Nombre = "Sucursal La Paz",     Direccion = "Calle Comercio 1230",              Ciudad = "La Paz",     Estado = "La Paz",     RegionId = regionLpz?.Id, Telefono = "22445566", Email = "lapaz@techparts.com.bo",     Responsable = "María Fernández López", Activa = true },
                new Sucursal { Nombre = "Sucursal Santa Cruz", Direccion = "Av. Cristo Redentor 890",          Ciudad = "Santa Cruz", Estado = "Santa Cruz", RegionId = regionScz?.Id, Telefono = "33445566", Email = "santacruz@techparts.com.bo", Responsable = "Diego Reyes Castillo", Activa = true }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("[SEED] Sucursales creadas.");
        }

        var sucursalCentro = await db.Sucursales.FirstOrDefaultAsync(s => s.Nombre == "Sucursal Centro");

        // 3. Sembrar Roles
        string[] roles =
        {
            "GerenteGeneral",
            "GerenteRegional",
            "GerenteSucursal",
            "Vendedor",
            "Comercial",
            "Cliente",
            "Admin",
            "User"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 4. Sembrar Usuarios demo si no existen
        const string demoPassword = "TechParts2026!";
        var demoUsers = new (string Email, string NombreCompleto, string Rol, int? SucursalId, int? RegionId)[]
        {
            ("gerente.general@techparts.com",  "Carlos Mendoza Rivera",  "GerenteGeneral",  null, null),
            ("gerente.regional@techparts.com", "María Fernández López",  "GerenteRegional", null, regionCbb?.Id),
            ("gerente.sucursal@techparts.com", "Roberto Jiménez Cruz",   "GerenteSucursal", sucursalCentro?.Id, null),
            ("vendedor@techparts.com",         "Laura Sánchez Morales",  "Vendedor",        sucursalCentro?.Id, null),
            ("comercial@techparts.com",        "Diego Reyes Castillo",   "Comercial",       null, null),
            ("cliente@techparts.com",          "Ana García Pérez",       "Cliente",         null, null),
        };

        foreach (var (email, nombre, rol, sucId, regId) in demoUsers)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var demoUser = new ApplicationUser
                {
                    UserName       = email,
                    Email          = email,
                    EmailConfirmed = true,
                    NombreCompleto = nombre,
                    SucursalId     = sucId,
                    RegionId       = regId,
                    CodigoCliente  = rol == "Cliente" ? "CLI-DEMO01" : null
                };

                var createResult = await userManager.CreateAsync(demoUser, demoPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(demoUser, rol);
                    Console.WriteLine($"[SEED] Usuario '{email}' creado con rol '{rol}'.");
                }
            }
        }

        // 5. Sembrar Proveedores si está vacía
        if (!await db.Proveedores.AnyAsync())
        {
            db.Proveedores.AddRange(
                new Proveedor { Nombre = "NVIDIA Latin America", Contacto = "Carlos Mendoza", Telefono = "800123456", Email = "ventas@nvidia.la",    Ciudad = "Miami",      Pais = "Estados Unidos", CategoriaEspecialidad = "Tarjetas de Video", SitioWeb = "https://www.nvidia.com", Activo = true },
                new Proveedor { Nombre = "AMD Bolivia",          Contacto = "Ana Suárez",     Telefono = "800654321", Email = "distribucion@amd.bo", Ciudad = "Cochabamba", Pais = "Bolivia",        CategoriaEspecialidad = "Procesadores",      SitioWeb = "https://www.amd.com", Activo = true },
                new Proveedor { Nombre = "Intel Andina",         Contacto = "Luis Fernández", Telefono = "800111222", Email = "sales@intel.com.bo",  Ciudad = "La Paz",     Pais = "Bolivia",        CategoriaEspecialidad = "Procesadores",      SitioWeb = "https://www.intel.com", Activo = true },
                new Proveedor { Nombre = "ASUS ROG Latam",       Contacto = "Pedro Gómez",    Telefono = "800333444", Email = "rog@asus.la",         Ciudad = "Taipei",     Pais = "Taiwán",         CategoriaEspecialidad = "Tarjetas Madre",    SitioWeb = "https://rog.asus.com", Activo = true },
                new Proveedor { Nombre = "G.SKILL International",Contacto = "Mei Lin",        Telefono = "800555666", Email = "latam@gskill.com",    Ciudad = "Taipei",     Pais = "Taiwán",         CategoriaEspecialidad = "Memoria RAM",       SitioWeb = "https://www.gskill.com", Activo = true },
                new Proveedor { Nombre = "Corsair Distribución", Contacto = "Miguel Rojas",   Telefono = "800999000", Email = "b2b@corsair.com",     Ciudad = "Fremont",    Pais = "Estados Unidos", CategoriaEspecialidad = "Fuentes de Poder",  SitioWeb = "https://www.corsair.com", Activo = true },
                new Proveedor { Nombre = "LG Electronics BO",    Contacto = "Pedro Mamani",   Telefono = "800444666", Email = "gaming@lg.com.bo",    Ciudad = "Santa Cruz", Pais = "Bolivia",        CategoriaEspecialidad = "Monitores",         SitioWeb = "https://www.lg.com", Activo = true },
                new Proveedor { Nombre = "Samsung Electronics",  Contacto = "Roberto Díaz",   Telefono = "800444555", Email = "ssd@samsung.com",     Ciudad = "Seúl",       Pais = "Corea del Sur",  CategoriaEspecialidad = "Almacenamiento",    SitioWeb = "https://www.samsung.com", Activo = true },
                new Proveedor { Nombre = "Logitech G Latam",     Contacto = "Carla Ruiz",     Telefono = "800666777", Email = "gaming@logitech.bo",  Ciudad = "Lausana",    Pais = "Suiza",          CategoriaEspecialidad = "Periféricos",       SitioWeb = "https://www.logitechg.com", Activo = true },
                new Proveedor { Nombre = "Cooler Master Latam",  Contacto = "Rodrigo Lagos",  Telefono = "800777888", Email = "cm@latam.com",        Ciudad = "Santiago",   Pais = "Chile",          CategoriaEspecialidad = "Refrigeración",     SitioWeb = "https://www.coolermaster.com", Activo = true }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("[SEED] Proveedores creados.");
        }

        // 6. Sembrar Categorías si está vacía
        if (!await db.Categorias.AnyAsync())
        {
            db.Categorias.AddRange(
                new Categoria { Nombre = "Tarjetas de Video", Slug = "tarjetas-de-video", Descripcion = "GPUs de alto rendimiento" },
                new Categoria { Nombre = "Procesadores",      Slug = "procesadores",      Descripcion = "CPUs Intel y AMD" },
                new Categoria { Nombre = "Almacenamiento",    Slug = "almacenamiento",    Descripcion = "Unidades SSD NVMe" },
                new Categoria { Nombre = "Tarjetas Madre",    Slug = "tarjetas-madre",    Descripcion = "Placas base DDR5" },
                new Categoria { Nombre = "Memoria RAM",       Slug = "memoria-ram",       Descripcion = "Módulos DDR5 gamer" },
                new Categoria { Nombre = "Fuentes de Poder",  Slug = "fuentes-de-poder",  Descripcion = "Fuentes modulares 80 Plus" },
                new Categoria { Nombre = "Monitores",         Slug = "monitores",         Descripcion = "Pantallas OLED/IPS" },
                new Categoria { Nombre = "Periféricos",       Slug = "perifericos",       Descripcion = "Mouse y teclados" },
                new Categoria { Nombre = "Refrigeración",     Slug = "refrigeracion",     Descripcion = "Refrigeración líquida AIO" },
                new Categoria { Nombre = "Cases",             Slug = "cases",             Descripcion = "Gabinetes ATX" }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("[SEED] Categorías creadas.");
        }

        // 7. Sembrar Marcas si está vacía
        if (!await db.Marcas.AnyAsync())
        {
            db.Marcas.AddRange(
                new Marca { Nombre = "NVIDIA",        Pais = "Estados Unidos" },
                new Marca { Nombre = "Intel",         Pais = "Estados Unidos" },
                new Marca { Nombre = "Samsung",       Pais = "Corea del Sur" },
                new Marca { Nombre = "ASUS",          Pais = "Taiwán" },
                new Marca { Nombre = "G.Skill",       Pais = "Taiwán" },
                new Marca { Nombre = "Corsair",       Pais = "Estados Unidos" },
                new Marca { Nombre = "LG",            Pais = "Corea del Sur" },
                new Marca { Nombre = "Logitech",      Pais = "Suiza" },
                new Marca { Nombre = "Cooler Master", Pais = "Taiwán" },
                new Marca { Nombre = "Razer",         Pais = "Estados Unidos" },
                new Marca { Nombre = "AMD",           Pais = "Estados Unidos" },
                new Marca { Nombre = "Lian Li",       Pais = "Taiwán" }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("[SEED] Marcas creadas.");
        }

        // 8. Sembrar Componentes si está vacía
        if (!await db.Componentes.AnyAsync())
        {
            var cats  = await db.Categorias.ToDictionaryAsync(c => c.Nombre, c => c.Id);
            var marks = await db.Marcas.ToDictionaryAsync(m => m.Nombre, m => m.Id);
            var provs = await db.Proveedores.ToDictionaryAsync(p => p.Nombre, p => p.Id);

            db.Componentes.AddRange(
                new Componente { Nombre = "NVIDIA RTX 5080 Ti", CategoriaId = cats["Tarjetas de Video"], MarcaId = marks["NVIDIA"], ProveedorId = provs.GetValueOrDefault("NVIDIA Latin America"), Descripcion = "GPU de última generación con 24 GB GDDR7 y DLSS 4.0 para gaming 4K extremo.", Especificaciones = "24 GB GDDR7 · PCIe 5.0 · 450W TDP · DLSS 4.0", Precio = 19500.00m, Stock = 15, StockMinimo = 5, ImageUrl = "https://pngimg.com/uploads/video_card/video_card_PNG155.png" },
                new Componente { Nombre = "Intel Core Ultra 9 285K", CategoriaId = cats["Procesadores"], MarcaId = marks["Intel"], ProveedorId = provs.GetValueOrDefault("Intel Andina"), Descripcion = "Procesador de 24 núcleos (8P+16E) a 5.7 GHz boost, socket LGA1851.", Especificaciones = "24 núcleos · 5.7 GHz boost · LGA1851 · 125W TDP", Precio = 6500.00m, Stock = 28, StockMinimo = 5, ImageUrl = "https://pngimg.com/uploads/processor/processor_PNG40.png" },
                new Componente { Nombre = "Samsung 990 Pro 2TB NVMe", CategoriaId = cats["Almacenamiento"], MarcaId = marks["Samsung"], ProveedorId = provs.GetValueOrDefault("Samsung Electronics"), Descripcion = "SSD PCIe 5.0 con lectura hasta 14,800 MB/s.", Especificaciones = "2 TB · PCIe 5.0 · 14,800 MB/s · M.2 2280", Precio = 1600.00m, Stock = 52, StockMinimo = 10, ImageUrl = "https://pngimg.com/uploads/hard_disc/hard_disc_PNG8436.png" },
                new Componente { Nombre = "ASUS ROG Maximus Z890 Hero", CategoriaId = cats["Tarjetas Madre"], MarcaId = marks["ASUS"], ProveedorId = provs.GetValueOrDefault("ASUS ROG Latam"), Descripcion = "Tarjeta madre ATX LGA1851, DDR5 hasta 8000 MHz OC, Wi-Fi 7.", Especificaciones = "LGA1851 · DDR5-8000 · 5x M.2 · Wi-Fi 7", Precio = 4500.00m, Stock = 10, StockMinimo = 4, ImageUrl = "https://pngimg.com/uploads/motherboard/motherboard_PNG39.png" },
                new Componente { Nombre = "G.SKILL Trident Z5 RGB 64GB DDR5", CategoriaId = cats["Memoria RAM"], MarcaId = marks["G.Skill"], ProveedorId = provs.GetValueOrDefault("G.SKILL International"), Descripcion = "Kit DDR5-6400 CL32, 2x32 GB con iluminación RGB.", Especificaciones = "64 GB (2x32) · DDR5-6400 · CL32 · ARGB", Precio = 2400.00m, Stock = 34, StockMinimo = 8, ImageUrl = "https://pngimg.com/uploads/ram/ram_PNG46.png" },
                new Componente { Nombre = "Corsair HX1500i Platinum", CategoriaId = cats["Fuentes de Poder"], MarcaId = marks["Corsair"], ProveedorId = provs.GetValueOrDefault("Corsair Distribución"), Descripcion = "Fuente modular 1500W 80+ Platinum ATX 3.0.", Especificaciones = "1500W · 80+ Platinum · Full Modular", Precio = 2500.00m, Stock = 20, StockMinimo = 5, ImageUrl = "https://pngimg.com/uploads/computer_case/computer_case_PNG27.png" },
                new Componente { Nombre = "LG UltraGear 27GS95QE OLED 240Hz", CategoriaId = cats["Monitores"], MarcaId = marks["LG"], ProveedorId = provs.GetValueOrDefault("LG Electronics BO"), Descripcion = "Monitor OLED QHD 27 pulgadas a 240Hz con 0.03ms GTG.", Especificaciones = "27\" OLED QHD · 240Hz · 0.03ms · HDMI 2.1", Precio = 8500.00m, Stock = 8, StockMinimo = 4, ImageUrl = "https://pngimg.com/uploads/monitor/monitor_PNG88.png" },
                new Componente { Nombre = "Logitech G Pro X Superlight 2", CategoriaId = cats["Periféricos"], MarcaId = marks["Logitech"], ProveedorId = provs.GetValueOrDefault("Logitech G Latam"), Descripcion = "Mouse gaming inalámbrico 32,000 DPI, 60g, sensor HERO 2.", Especificaciones = "32,000 DPI · 60g · LIGHTSPEED", Precio = 1200.00m, Stock = 45, StockMinimo = 10, ImageUrl = "https://pngimg.com/uploads/mouse_pc/mouse_pc_PNG10207.png" },
                new Componente { Nombre = "Cooler Master MasterLiquid 360 ATMOS", CategoriaId = cats["Refrigeración"], MarcaId = marks["Cooler Master"], ProveedorId = provs.GetValueOrDefault("Cooler Master Latam"), Descripcion = "Refrigeración líquida AIO 360mm ARGB compatible LGA1851 y AM5.", Especificaciones = "360mm AIO · 3x120mm ARGB · 2800 RPM", Precio = 1800.00m, Stock = 22, StockMinimo = 6, ImageUrl = "https://pngimg.com/uploads/computer_fan/computer_fan_PNG82.png" },
                new Componente { Nombre = "Razer BlackWidow V4 Pro", CategoriaId = cats["Periféricos"], MarcaId = marks["Razer"], ProveedorId = provs.GetValueOrDefault("Logitech G Latam"), Descripcion = "Teclado mecánico inalámbrico switches Razer Yellow, Chroma RGB.", Especificaciones = "Razer Yellow · Wireless · Chroma RGB", Precio = 1500.00m, Stock = 30, StockMinimo = 6, ImageUrl = "https://pngimg.com/uploads/keyboard/keyboard_PNG5856.png" },
                new Componente { Nombre = "AMD Ryzen 9 9950X", CategoriaId = cats["Procesadores"], MarcaId = marks["AMD"], ProveedorId = provs.GetValueOrDefault("AMD Bolivia"), Descripcion = "16 núcleos y 32 hilos con arquitectura Zen 5, socket AM5.", Especificaciones = "16 núcleos · 5.7 GHz · AM5 · 170W TDP", Precio = 6200.00m, Stock = 15, StockMinimo = 5, ImageUrl = "https://pngimg.com/uploads/processor/processor_PNG40.png" },
                new Componente { Nombre = "ASUS ROG Crosshair X670E Hero", CategoriaId = cats["Tarjetas Madre"], MarcaId = marks["ASUS"], ProveedorId = provs.GetValueOrDefault("ASUS ROG Latam"), Descripcion = "Placa madre entusiasta AM5 para Ryzen 7000/9000.", Especificaciones = "AM5 · X670E · DDR5 · Wi-Fi 6E", Precio = 4200.00m, Stock = 12, StockMinimo = 4, EstadoObsolescencia = "PorDescontinuar", ImageUrl = "https://pngimg.com/uploads/motherboard/motherboard_PNG39.png" },
                new Componente { Nombre = "AMD Radeon RX 7900 XTX", CategoriaId = cats["Tarjetas de Video"], MarcaId = marks["AMD"], ProveedorId = provs.GetValueOrDefault("AMD Bolivia"), Descripcion = "GPU tope de gama RDNA 3, 24GB GDDR6, DisplayPort 2.1.", Especificaciones = "24 GB GDDR6 · RDNA 3 · DP 2.1", Precio = 9800.00m, Stock = 8, StockMinimo = 5, ImageUrl = "https://pngimg.com/uploads/video_card/video_card_PNG155.png" },
                new Componente { Nombre = "Corsair MP700 PRO 2TB Gen5", CategoriaId = cats["Almacenamiento"], MarcaId = marks["Corsair"], ProveedorId = provs.GetValueOrDefault("Corsair Distribución"), Descripcion = "SSD NVMe M.2 PCIe Gen5 x4 con enfriador activo.", Especificaciones = "2 TB · PCIe 5.0 · 12,400 MB/s", Precio = 2100.00m, Stock = 25, StockMinimo = 5, ImageUrl = "https://pngimg.com/uploads/hard_disc/hard_disc_PNG8436.png" },
                new Componente { Nombre = "Lian Li O11 Dynamic EVO", CategoriaId = cats["Cases"], MarcaId = marks["Lian Li"], ProveedorId = provs.GetValueOrDefault("Cooler Master Latam"), Descripcion = "Chasis Mid-Tower ATX de doble cámara con cristal templado.", Especificaciones = "ATX Mid-Tower · Cristal Templado", Precio = 1450.00m, Stock = 4, StockMinimo = 5, ImageUrl = "https://pngimg.com/uploads/computer_case/computer_case_PNG27.png" }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("[SEED] Componentes creados.");
        }

        // 9. Sembrar Combos si está vacía
        if (!await db.Combos.AnyAsync())
        {
            var dbComps = await db.Componentes.ToListAsync();
            var gpu   = dbComps.FirstOrDefault(c => c.Nombre.Contains("RTX 5080 Ti"));
            var cpu   = dbComps.FirstOrDefault(c => c.Nombre.Contains("Core Ultra 9"));
            var ram   = dbComps.FirstOrDefault(c => c.Nombre.Contains("Trident"));
            var ssd   = dbComps.FirstOrDefault(c => c.Nombre.Contains("990 Pro"));
            var mb    = dbComps.FirstOrDefault(c => c.Nombre.Contains("Maximus"));
            var psu   = dbComps.FirstOrDefault(c => c.Nombre.Contains("HX1500"));
            var cool  = dbComps.FirstOrDefault(c => c.Nombre.Contains("MasterLiquid"));
            var mon   = dbComps.FirstOrDefault(c => c.Nombre.Contains("UltraGear"));
            var mouse = dbComps.FirstOrDefault(c => c.Nombre.Contains("Superlight"));
            var kb    = dbComps.FirstOrDefault(c => c.Nombre.Contains("BlackWidow"));

            if (gpu != null && cpu != null && ram != null && ssd != null && mb != null && psu != null && cool != null)
            {
                db.Combos.Add(new Combo
                {
                    Nombre = "Gaming PC Ultimate",
                    Descripcion = "La máquina definitiva para gaming 4K: RTX 5080 Ti + Core Ultra 9 285K + 64GB DDR5.",
                    PrecioVenta = 36800.00m, Descuento = 5.15m, Stock = 5, Activo = true,
                    ImageUrl = "https://pngimg.com/uploads/computer_case/computer_case_PNG27.png",
                    ComboComponentes = new List<ComboComponente>
                    {
                        new() { ComponenteId = gpu.Id, Cantidad = 1 }, new() { ComponenteId = cpu.Id, Cantidad = 1 },
                        new() { ComponenteId = ram.Id, Cantidad = 1 }, new() { ComponenteId = ssd.Id, Cantidad = 1 },
                        new() { ComponenteId = mb.Id,  Cantidad = 1 }, new() { ComponenteId = psu.Id, Cantidad = 1 },
                        new() { ComponenteId = cool.Id,Cantidad = 1 }
                    }
                });
            }

            if (mon != null && mouse != null && kb != null)
            {
                db.Combos.Add(new Combo
                {
                    Nombre = "Setup Gaming Completo",
                    Descripcion = "Combo periféricos premium: monitor OLED 240Hz + mouse Superlight 2 + teclado BlackWidow V4.",
                    PrecioVenta = 10500.00m, Descuento = 6.25m, Stock = 8, Activo = true,
                    ImageUrl = "https://pngimg.com/uploads/monitor/monitor_PNG88.png",
                    ComboComponentes = new List<ComboComponente>
                    {
                        new() { ComponenteId = mon.Id, Cantidad = 1 },
                        new() { ComponenteId = mouse.Id, Cantidad = 1 },
                        new() { ComponenteId = kb.Id, Cantidad = 1 }
                    }
                });
            }

            if (cpu != null && mb != null && ram != null && ssd != null)
            {
                db.Combos.Add(new Combo
                {
                    Nombre = "Starter Build DDR5",
                    Descripcion = "Base perfecta para armar tu primera PC DDR5: CPU + Motherboard + RAM + SSD NVMe.",
                    PrecioVenta = 14250.00m, Descuento = 5.00m, Stock = 8, Activo = true,
                    ImageUrl = "https://pngimg.com/uploads/motherboard/motherboard_PNG39.png",
                    ComboComponentes = new List<ComboComponente>
                    {
                        new() { ComponenteId = cpu.Id, Cantidad = 1 }, new() { ComponenteId = mb.Id, Cantidad = 1 },
                        new() { ComponenteId = ram.Id, Cantidad = 1 }, new() { ComponenteId = ssd.Id, Cantidad = 1 }
                    }
                });
            }

            await db.SaveChangesAsync();
            Console.WriteLine("[SEED] Combos creados.");
        }

        // 10. Sembrar Pedidos de demo si está vacía (con FKs reales)
        if (!await db.Pedidos.AnyAsync())
        {
            var comps = await db.Componentes.Take(5).ToListAsync();
            var clienteUser  = await userManager.FindByEmailAsync("cliente@techparts.com");
            var vendedorUser = await userManager.FindByEmailAsync("vendedor@techparts.com");
            var gerenteUser  = await userManager.FindByEmailAsync("gerente.general@techparts.com");

            if (comps.Count >= 3 && clienteUser != null)
            {
                string clienteId  = clienteUser.Id;
                string vendedorId = vendedorUser?.Id ?? clienteId;
                string gerenteId  = gerenteUser?.Id  ?? clienteId;

                var datos = new[]
                {
                    (UserId: clienteId,  Fecha: DateTime.UtcNow.AddDays(-85), Metodo: MetodoPago.Tarjeta,       Estado: EstadoPedido.Entregado),
                    (UserId: clienteId,  Fecha: DateTime.UtcNow.AddDays(-68), Metodo: MetodoPago.QR,            Estado: EstadoPedido.Entregado),
                    (UserId: clienteId,  Fecha: DateTime.UtcNow.AddDays(-52), Metodo: MetodoPago.Transferencia, Estado: EstadoPedido.Devuelto),
                    (UserId: vendedorId, Fecha: DateTime.UtcNow.AddDays(-40), Metodo: MetodoPago.Tarjeta,       Estado: EstadoPedido.Entregado),
                    (UserId: gerenteId,  Fecha: DateTime.UtcNow.AddDays(-20), Metodo: MetodoPago.Efectivo,      Estado: EstadoPedido.Completado),
                    (UserId: clienteId,  Fecha: DateTime.UtcNow.AddDays(-10), Metodo: MetodoPago.Tarjeta,       Estado: EstadoPedido.Completado),
                };

                int seq = 1;
                var pedidosDemo = new List<Pedido>();
                foreach (var d in datos)
                {
                    var c1 = comps[seq % comps.Count];
                    var c2 = comps[(seq + 1) % comps.Count];
                    int qty1 = (seq % 2) + 1;

                    decimal sub = c1.Precio * qty1 + c2.Precio;
                    decimal iva = Math.Round(sub * 0.16m, 2);
                    decimal tot = sub + iva;
                    decimal com = Math.Round(sub * 0.05m, 2);

                    pedidosDemo.Add(new Pedido
                    {
                        NumeroPedido = $"TPC-{d.Fecha:yyyyMMdd}-{seq:D4}",
                        UserId       = d.UserId,
                        SucursalId   = sucursalCentro?.Id,
                        FechaPedido  = d.Fecha,
                        FechaEntrega = d.Estado == EstadoPedido.Entregado ? d.Fecha.AddDays(2) : null,
                        Subtotal     = sub,
                        Impuesto     = iva,
                        Total        = tot,
                        Comision     = com,
                        MetodoPago   = d.Metodo,
                        Estado       = d.Estado,
                        Items = new List<PedidoItem>
                        {
                            new() { ComponenteId = c1.Id, ComboId = null, ProductoNombre = c1.Nombre, PrecioUnitario = c1.Precio, Cantidad = qty1, Subtotal = c1.Precio * qty1, ImageUrl = c1.ImageUrl },
                            new() { ComponenteId = c2.Id, ComboId = null, ProductoNombre = c2.Nombre, PrecioUnitario = c2.Precio, Cantidad = 1,    Subtotal = c2.Precio,        ImageUrl = c2.ImageUrl }
                        }
                    });
                    seq++;
                }

                db.Pedidos.AddRange(pedidosDemo);
                await db.SaveChangesAsync();
                Console.WriteLine($"[SEED] {pedidosDemo.Count} pedidos de demo insertados.");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[AVISO BD] Error en inicialización/seeding: {ex.Message}");
}

app.Run();
