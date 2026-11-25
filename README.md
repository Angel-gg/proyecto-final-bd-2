# TechParts ERP — Proyecto Final Base de Datos II (UPDS Cochabamba)

Aplicación web de comercio electrónico y gestión comercial de hardware gamer desarrollada con **ASP.NET Core MVC (.NET 10 LTS)**, **Entity Framework Core 10 (Npgsql)** y **PostgreSQL en Supabase**.

- **Asignatura:** Base de Datos II — Universidad Privada Domingo Savio (UPDS), Sede Cochabamba
- **Docente:** Ing. Lineth Coca Cindy
- **Autor:** Angel Gabriel Rojas Hinojosa (Trabajo Individual)

## 🚀 Tecnologías Utilizadas

- **Framework:** .NET 10 (C# 14 / ASP.NET Core MVC)
- **Base de Datos:** PostgreSQL en la nube alojada en [Supabase](https://supabase.com)
- **ORM:** Entity Framework Core 10 con Npgsql PostgreSQL Provider
- **Arquitectura de Datos (3FN Multiesquema):**
  - `catalogo`: `Categorias`, `Marcas`, `Componentes`, `Combos`, `ComboComponentes`
  - `ventas`: `Pedidos`, `PedidoItems`, `CarritosAbandonados` (`JSONB` + índice `GIN`) + procedimiento transaccional `ventas.sp_confirmar_venta`
  - `operaciones`: `Regiones`, `Sucursales`, `Proveedores`
  - `auditoria`: `AuditoriaComponentes`, `AuditoriaPedidos`, `MermasInventario` (ODS 12) + triggers automáticos
  - `public`: Tablas de ASP.NET Core Identity (`AspNetUsers`, `AspNetRoles`, etc.) enlazadas con `Sucursales` y `Regiones`
- **Autenticación y Seguridad:** ASP.NET Core Identity con Roles (`GerenteGeneral`, `GerenteRegional`, `GerenteSucursal`, `Vendedor`, `Comercial`, `Cliente`, `Admin`).
- **Frontend:** Razor Views, Bootstrap 5, Vanilla CSS responsivo y Web Speech API.

## 🛠️ Requisitos Previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Cuenta y proyecto en Supabase con PostgreSQL activo.

## ⚙️ Configuración y Ejecución

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/Angel-gg/proyecto-final-bd-2.git
   cd proyecto-final-bd-2/EcommerceApp
   ```

2. Configurar la cadena de conexión en `appsettings.json` con las credenciales de Supabase:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=aws-0-us-west-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.tu-usuario;Password=tu-password"
     }
   }
   ```

3. Ejecutar el script SQL normalizado e idempotente en Supabase SQL Editor:
   - Ejecutar `informe/anexos/script_bd2_completo.sql`
   - (Opcional) Ejecutar las consultas analíticas y pruebas transaccionales en `informe/anexos/consultas_y_pruebas_bd2.sql`

4. Ejecutar el proyecto:
   ```bash
   dotnet run
   ```

5. Abrir en el navegador: `http://localhost:5134`

## 👥 Roles y Permisos

- **GerenteGeneral / Admin:** Acceso total a inventario, combos, sucursales, personal, reportes y auditoría.
- **GerenteRegional / GerenteSucursal:** Supervisión operativa por región o sucursal y reportes gerenciales.
- **Comercial:** Gestión de proveedores, catálogo de componentes, combos y reposición de stock.
- **Vendedor:** Consulta de inventario, ventas y atención al cliente.
- **Cliente:** Navegación en la tienda web, carrito de compras, checkout transaccional e historial de pedidos.
