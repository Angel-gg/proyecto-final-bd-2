using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Models;

namespace EcommerceApp.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        // ── Esquema catalogo (3FN) ──
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Componente> Componentes { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboComponente> ComboComponentes { get; set; }

        // ── Esquema ventas ──
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoItem> PedidoItems { get; set; }
        public DbSet<CarritoAbandonado> CarritosAbandonados { get; set; }

        // ── Esquema operaciones ──
        public DbSet<Region> Regiones { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }

        // ── Esquema auditoria ──
        public DbSet<AuditoriaComponente> AuditoriaComponentes { get; set; }
        public DbSet<AuditoriaPedido> AuditoriaPedidos { get; set; }
        public DbSet<MermaInventario> MermasInventario { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── public: AspNetUsers extendida con FKs hacia operaciones ──
            modelBuilder.Entity<ApplicationUser>(e =>
            {
                e.HasOne(u => u.Sucursal)
                    .WithMany()
                    .HasForeignKey(u => u.SucursalId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(u => u.Region)
                    .WithMany()
                    .HasForeignKey(u => u.RegionId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(u => u.CodigoCliente)
                    .IsUnique()
                    .HasDatabaseName("uq_users_codigo_cliente");
            });

            // ── operaciones.Regiones ──
            modelBuilder.Entity<Region>(e =>
            {
                e.ToTable("Regiones", "operaciones");
                e.Property(r => r.Nombre).HasMaxLength(100).IsRequired();
                e.Property(r => r.Codigo).HasMaxLength(10).IsRequired();
                e.HasIndex(r => r.Nombre).IsUnique().HasDatabaseName("uq_regiones_nombre");
                e.HasIndex(r => r.Codigo).IsUnique().HasDatabaseName("uq_regiones_codigo");
            });

            // ── operaciones.Sucursales ──
            modelBuilder.Entity<Sucursal>(e =>
            {
                e.ToTable("Sucursales", "operaciones");
                e.Property(s => s.Nombre).HasMaxLength(150).IsRequired();
                e.Property(s => s.Ciudad).HasMaxLength(100).IsRequired();
                e.HasIndex(s => s.Nombre).IsUnique().HasDatabaseName("uq_sucursales_nombre");
                e.HasOne(s => s.Region)
                    .WithMany(r => r.Sucursales)
                    .HasForeignKey(s => s.RegionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── operaciones.Proveedores ──
            modelBuilder.Entity<Proveedor>(e =>
            {
                e.ToTable("Proveedores", "operaciones");
                e.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
                e.HasIndex(p => p.Nombre).IsUnique().HasDatabaseName("uq_proveedores_nombre");
                e.HasIndex(p => p.Email).IsUnique().HasDatabaseName("uq_proveedores_email");
            });

            // ── catalogo.Categorias ──
            modelBuilder.Entity<Categoria>(e =>
            {
                e.ToTable("Categorias", "catalogo");
                e.HasIndex(c => c.Nombre).IsUnique().HasDatabaseName("uq_categorias_nombre");
                e.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("uq_categorias_slug");
            });

            // ── catalogo.Marcas ──
            modelBuilder.Entity<Marca>(e =>
            {
                e.ToTable("Marcas", "catalogo");
                e.HasIndex(m => m.Nombre).IsUnique().HasDatabaseName("uq_marcas_nombre");
            });

            // ── catalogo.Componentes ──
            modelBuilder.Entity<Componente>(e =>
            {
                e.ToTable("Componentes", "catalogo", t =>
                {
                    t.HasCheckConstraint("ck_componentes_precio", "\"Precio\" > 0");
                    t.HasCheckConstraint("ck_componentes_stock", "\"Stock\" >= 0");
                    t.HasCheckConstraint("ck_componentes_min", "\"StockMinimo\" >= 0");
                    t.HasCheckConstraint("ck_componentes_obsol",
                        "\"EstadoObsolescencia\" IN ('Vigente', 'PorDescontinuar', 'Obsoleto', 'Merma')");
                });

                e.Property(c => c.Precio).HasPrecision(18, 2);
                e.HasIndex(c => c.Nombre).IsUnique().HasDatabaseName("uq_componentes_nombre");
                e.HasIndex(c => c.CategoriaId).HasDatabaseName("idx_comp_categoria");
                e.HasIndex(c => c.MarcaId).HasDatabaseName("idx_comp_marca");
                e.HasIndex(c => c.Precio).HasDatabaseName("idx_comp_precio");

                e.HasOne(c => c.Categoria)
                    .WithMany(cat => cat.Componentes)
                    .HasForeignKey(c => c.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(c => c.Marca)
                    .WithMany(m => m.Componentes)
                    .HasForeignKey(c => c.MarcaId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(c => c.Proveedor)
                    .WithMany(p => p.Componentes)
                    .HasForeignKey(c => c.ProveedorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── catalogo.Combos ──
            modelBuilder.Entity<Combo>(e =>
            {
                e.ToTable("Combos", "catalogo", t =>
                {
                    t.HasCheckConstraint("ck_combos_precio", "\"PrecioVenta\" > 0");
                    t.HasCheckConstraint("ck_combos_descuento", "\"Descuento\" >= 0 AND \"Descuento\" <= 100");
                    t.HasCheckConstraint("ck_combos_stock", "\"Stock\" >= 0");
                });

                e.Property(c => c.PrecioVenta).HasPrecision(18, 2);
                e.Property(c => c.Descuento).HasPrecision(5, 2);
                e.HasIndex(c => c.Nombre).IsUnique().HasDatabaseName("uq_combos_nombre");
            });

            // ── catalogo.ComboComponentes (PK compuesta) ──
            modelBuilder.Entity<ComboComponente>(e =>
            {
                e.ToTable("ComboComponentes", "catalogo", t =>
                    t.HasCheckConstraint("ck_cc_cant", "\"Cantidad\" > 0"));

                e.HasKey(cc => new { cc.ComboId, cc.ComponenteId });

                e.HasOne(cc => cc.Combo)
                    .WithMany(c => c.ComboComponentes)
                    .HasForeignKey(cc => cc.ComboId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(cc => cc.Componente)
                    .WithMany(c => c.ComboComponentes)
                    .HasForeignKey(cc => cc.ComponenteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── ventas.Pedidos ──
            modelBuilder.Entity<Pedido>(e =>
            {
                e.ToTable("Pedidos", "ventas", t =>
                {
                    t.HasCheckConstraint("ck_ped_subtotal", "\"Subtotal\" >= 0");
                    t.HasCheckConstraint("ck_ped_impuesto", "\"Impuesto\" >= 0");
                    t.HasCheckConstraint("ck_ped_total", "\"Total\" >= 0");
                    t.HasCheckConstraint("ck_ped_comision", "\"Comision\" >= 0");
                    t.HasCheckConstraint("ck_ped_metodo", "\"MetodoPago\" IN (0, 1, 2, 3)");
                    t.HasCheckConstraint("ck_ped_estado", "\"Estado\" IN (0, 1, 2, 3, 4)");
                });

                e.Property(p => p.Subtotal).HasPrecision(18, 2);
                e.Property(p => p.Impuesto).HasPrecision(18, 2);
                e.Property(p => p.Total).HasPrecision(18, 2);
                e.Property(p => p.Comision).HasPrecision(18, 2);

                e.HasIndex(p => p.NumeroPedido).IsUnique().HasDatabaseName("uq_pedidos_numero");
                e.HasIndex(p => p.UserId).HasDatabaseName("idx_pedidos_user");
                e.HasIndex(p => p.Estado).HasDatabaseName("idx_pedidos_estado");
                e.HasIndex(p => p.FechaPedido).IsDescending().HasDatabaseName("idx_pedidos_fecha");

                e.HasOne(p => p.Usuario)
                    .WithMany()
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.Sucursal)
                    .WithMany()
                    .HasForeignKey(p => p.SucursalId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasMany(p => p.Items)
                    .WithOne(i => i.Pedido)
                    .HasForeignKey(i => i.PedidoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── ventas.PedidoItems (FKs exclusivas) ──
            modelBuilder.Entity<PedidoItem>(e =>
            {
                e.ToTable("PedidoItems", "ventas", t =>
                {
                    t.HasCheckConstraint("ck_pi_cantidad", "\"Cantidad\" > 0");
                    t.HasCheckConstraint("ck_pi_precio", "\"PrecioUnitario\" > 0");
                    t.HasCheckConstraint("ck_pi_subtotal", "\"Subtotal\" > 0");
                    t.HasCheckConstraint("ck_pi_exclusivo",
                        "(\"ComponenteId\" IS NOT NULL AND \"ComboId\" IS NULL) OR (\"ComboId\" IS NOT NULL AND \"ComponenteId\" IS NULL)");
                });

                e.Property(p => p.PrecioUnitario).HasPrecision(18, 2);
                e.Property(p => p.Subtotal).HasPrecision(18, 2);

                e.HasOne(i => i.Componente)
                    .WithMany()
                    .HasForeignKey(i => i.ComponenteId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                e.HasOne(i => i.Combo)
                    .WithMany()
                    .HasForeignKey(i => i.ComboId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });

            // ── ventas.CarritosAbandonados (JSONB + GIN) ──
            modelBuilder.Entity<CarritoAbandonado>(e =>
            {
                e.ToTable("CarritosAbandonados", "ventas", t =>
                {
                    t.HasCheckConstraint("ck_carr_total", "\"TotalEstimado\" >= 0");
                    t.HasCheckConstraint("ck_carr_items", "\"TotalItems\" >= 0");
                });

                e.Property(c => c.ItemsJson).HasColumnType("jsonb");
                e.Property(c => c.TotalEstimado).HasPrecision(18, 2);

                e.HasIndex(c => c.ItemsJson)
                    .HasMethod("gin")
                    .HasDatabaseName("idx_carritos_json");

                e.HasOne(c => c.Usuario)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── auditoria.AuditoriaComponentes ──
            modelBuilder.Entity<AuditoriaComponente>(e =>
            {
                e.ToTable("AuditoriaComponentes", "auditoria", t =>
                    t.HasCheckConstraint("ck_audit_op", "\"Operacion\" IN ('I', 'U', 'D')"));
                e.Property(a => a.PrecioAntes).HasPrecision(18, 2);
                e.Property(a => a.PrecioDespues).HasPrecision(18, 2);
            });

            // ── auditoria.AuditoriaPedidos ──
            modelBuilder.Entity<AuditoriaPedido>(e =>
            {
                e.ToTable("AuditoriaPedidos", "auditoria");
            });

            // ── auditoria.MermasInventario (ODS 12) ──
            modelBuilder.Entity<MermaInventario>(e =>
            {
                e.ToTable("MermasInventario", "auditoria", t =>
                {
                    t.HasCheckConstraint("ck_merma_cant", "\"Cantidad\" > 0");
                    t.HasCheckConstraint("ck_merma_costo", "\"CostoEstimado\" >= 0");
                    t.HasCheckConstraint("ck_merma_motivo",
                        "\"Motivo\" IN ('Obsolescencia', 'DanoFisico', 'Garantia', 'ReciclajeRAEE', 'Otro')");
                    t.HasCheckConstraint("ck_merma_disp",
                        "\"DisposicionFinal\" IN ('ReciclajeCertificado', 'Reacondicionado', 'Donacion', 'BajaDefinitiva')");
                });

                e.Property(m => m.CostoEstimado).HasPrecision(18, 2);

                e.HasOne(m => m.Componente)
                    .WithMany()
                    .HasForeignKey(m => m.ComponenteId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(m => m.Sucursal)
                    .WithMany()
                    .HasForeignKey(m => m.SucursalId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
