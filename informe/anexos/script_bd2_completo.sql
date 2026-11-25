-- ============================================================================
--  SCRIPT BD2 COMPLETO - TechParts ERP
--  Materia: Base de Datos II - UPDS Cochabamba
--  Autor: Angel Gabriel Rojas Hinojosa
--  Docente: Ing. Lineth Coca Cindy
--  Fecha: 25/11/2026
-- ============================================================================
-- INSTRUCCIONES:
--   Ejecutar completo en Supabase SQL Editor o psql sobre una instancia que
--   ya contiene las tablas de ASP.NET Core Identity en el esquema public.
--   Script 100% idempotente (re-ejecutable sin errores).
-- ============================================================================

-- ============================================================================
-- 0. LIMPIEZA DE TABLA LEGACY EN INGLES
-- ============================================================================
DROP TABLE IF EXISTS public."Products" CASCADE;

-- ============================================================================
-- 1. ESQUEMAS DE NEGOCIO (Identity se mantiene en public)
-- ============================================================================
CREATE SCHEMA IF NOT EXISTS operaciones;
CREATE SCHEMA IF NOT EXISTS catalogo;
CREATE SCHEMA IF NOT EXISTS ventas;
CREATE SCHEMA IF NOT EXISTS auditoria;

-- ============================================================================
-- 2. ESQUEMA OPERACIONES (Regiones, Sucursales, Proveedores)
-- ============================================================================
CREATE TABLE IF NOT EXISTS operaciones."Regiones" (
  "Id"     SERIAL PRIMARY KEY,
  "Nombre" VARCHAR(100) NOT NULL,
  "Codigo" VARCHAR(10)  NOT NULL,
  "Activa" BOOLEAN      NOT NULL DEFAULT TRUE,
  CONSTRAINT uq_regiones_nombre UNIQUE ("Nombre"),
  CONSTRAINT uq_regiones_codigo UNIQUE ("Codigo")
);

CREATE TABLE IF NOT EXISTS operaciones."Sucursales" (
  "Id"          SERIAL PRIMARY KEY,
  "Nombre"      VARCHAR(150) NOT NULL,
  "Direccion"   VARCHAR(300),
  "Ciudad"      VARCHAR(100) NOT NULL,
  "Estado"      VARCHAR(100),
  "RegionId"    INTEGER,
  "Telefono"    VARCHAR(30),
  "Email"       VARCHAR(150),
  "Responsable" VARCHAR(150),
  "Activa"      BOOLEAN      NOT NULL DEFAULT TRUE,
  "CreadaEn"    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  CONSTRAINT uq_sucursales_nombre UNIQUE ("Nombre"),
  CONSTRAINT fk_suc_region
    FOREIGN KEY ("RegionId")
    REFERENCES operaciones."Regiones"("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS operaciones."Proveedores" (
  "Id"                    SERIAL PRIMARY KEY,
  "Nombre"                VARCHAR(150) NOT NULL,
  "Contacto"              VARCHAR(150),
  "Email"                 VARCHAR(150),
  "Telefono"              VARCHAR(30),
  "Direccion"             VARCHAR(300),
  "Ciudad"                VARCHAR(100),
  "Pais"                  VARCHAR(80),
  "CategoriaEspecialidad" VARCHAR(100),
  "SitioWeb"              VARCHAR(250),
  "Activo"                BOOLEAN      NOT NULL DEFAULT TRUE,
  "CreadoEn"              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  CONSTRAINT uq_proveedores_nombre UNIQUE ("Nombre"),
  CONSTRAINT uq_proveedores_email  UNIQUE ("Email")
);

-- ============================================================================
-- 3. ESQUEMA CATALOGO (3FN: Categorias, Marcas, Componentes, Combos)
-- ============================================================================
CREATE TABLE IF NOT EXISTS catalogo."Categorias" (
  "Id"          SERIAL PRIMARY KEY,
  "Nombre"      VARCHAR(80)  NOT NULL,
  "Slug"        VARCHAR(100) NOT NULL,
  "Descripcion" VARCHAR(300),
  "Activa"      BOOLEAN      NOT NULL DEFAULT TRUE,
  CONSTRAINT uq_categorias_nombre UNIQUE ("Nombre"),
  CONSTRAINT uq_categorias_slug   UNIQUE ("Slug")
);

CREATE TABLE IF NOT EXISTS catalogo."Marcas" (
  "Id"     SERIAL PRIMARY KEY,
  "Nombre" VARCHAR(80) NOT NULL,
  "Pais"   VARCHAR(80),
  "Activa" BOOLEAN     NOT NULL DEFAULT TRUE,
  CONSTRAINT uq_marcas_nombre UNIQUE ("Nombre")
);

CREATE TABLE IF NOT EXISTS catalogo."Componentes" (
  "Id"                  SERIAL PRIMARY KEY,
  "Nombre"              VARCHAR(150)  NOT NULL,
  "Descripcion"         VARCHAR(800)  NOT NULL,
  "CategoriaId"         INTEGER       NOT NULL,
  "MarcaId"             INTEGER       NOT NULL,
  "ProveedorId"         INTEGER,
  "Precio"              NUMERIC(18,2) NOT NULL,
  "Stock"               INTEGER       NOT NULL DEFAULT 0,
  "StockMinimo"         INTEGER       NOT NULL DEFAULT 5,
  "ImageUrl"            VARCHAR(500),
  "Especificaciones"    TEXT,
  "EstadoObsolescencia" VARCHAR(20)   NOT NULL DEFAULT 'Vigente',
  "Activo"              BOOLEAN       NOT NULL DEFAULT TRUE,
  "CreadoEn"            TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
  "ActualizadoEn"       TIMESTAMPTZ,
  CONSTRAINT uq_componentes_nombre UNIQUE ("Nombre"),
  CONSTRAINT ck_componentes_precio CHECK ("Precio" > 0),
  CONSTRAINT ck_componentes_stock  CHECK ("Stock" >= 0),
  CONSTRAINT ck_componentes_min    CHECK ("StockMinimo" >= 0),
  CONSTRAINT ck_componentes_obsol  CHECK (
    "EstadoObsolescencia" IN ('Vigente', 'PorDescontinuar', 'Obsoleto', 'Merma')
  ),
  CONSTRAINT fk_comp_categoria
    FOREIGN KEY ("CategoriaId")
    REFERENCES catalogo."Categorias"("Id") ON DELETE RESTRICT,
  CONSTRAINT fk_comp_marca
    FOREIGN KEY ("MarcaId")
    REFERENCES catalogo."Marcas"("Id") ON DELETE RESTRICT,
  CONSTRAINT fk_comp_proveedor
    FOREIGN KEY ("ProveedorId")
    REFERENCES operaciones."Proveedores"("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS catalogo."Combos" (
  "Id"            SERIAL PRIMARY KEY,
  "Nombre"        VARCHAR(150)  NOT NULL,
  "Descripcion"   VARCHAR(1000) NOT NULL,
  "PrecioVenta"   NUMERIC(18,2) NOT NULL,
  "Descuento"     NUMERIC(5,2)  NOT NULL DEFAULT 0,
  "ImageUrl"      VARCHAR(500),
  "Stock"         INTEGER       NOT NULL DEFAULT 0,
  "Activo"        BOOLEAN       NOT NULL DEFAULT TRUE,
  "CreadoEn"      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
  "ActualizadoEn" TIMESTAMPTZ,
  CONSTRAINT uq_combos_nombre    UNIQUE ("Nombre"),
  CONSTRAINT ck_combos_precio    CHECK ("PrecioVenta" > 0),
  CONSTRAINT ck_combos_descuento CHECK ("Descuento" >= 0 AND "Descuento" <= 100),
  CONSTRAINT ck_combos_stock     CHECK ("Stock" >= 0)
);

CREATE TABLE IF NOT EXISTS catalogo."ComboComponentes" (
  "ComboId"      INTEGER NOT NULL,
  "ComponenteId" INTEGER NOT NULL,
  "Cantidad"     INTEGER NOT NULL DEFAULT 1,
  CONSTRAINT pk_combo_comp PRIMARY KEY ("ComboId", "ComponenteId"),
  CONSTRAINT ck_cc_cant    CHECK ("Cantidad" > 0),
  CONSTRAINT fk_cc_combo
    FOREIGN KEY ("ComboId")
    REFERENCES catalogo."Combos"("Id") ON DELETE CASCADE,
  CONSTRAINT fk_cc_comp
    FOREIGN KEY ("ComponenteId")
    REFERENCES catalogo."Componentes"("Id") ON DELETE RESTRICT
);

-- ============================================================================
-- 4. ESQUEMA VENTAS (Pedidos, PedidoItems con FKs exclusivas, Carritos JSONB)
-- ============================================================================
CREATE TABLE IF NOT EXISTS ventas."Pedidos" (
  "Id"           SERIAL PRIMARY KEY,
  "NumeroPedido" VARCHAR(30)   NOT NULL,
  "UserId"       TEXT          NOT NULL,
  "SucursalId"   INTEGER,
  "FechaPedido"  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
  "FechaEntrega" TIMESTAMPTZ,
  "Subtotal"     NUMERIC(18,2) NOT NULL DEFAULT 0,
  "Impuesto"     NUMERIC(18,2) NOT NULL DEFAULT 0,
  "Total"        NUMERIC(18,2) NOT NULL DEFAULT 0,
  "Comision"     NUMERIC(18,2) NOT NULL DEFAULT 0,
  "MetodoPago"   SMALLINT      NOT NULL DEFAULT 1,
  "Estado"       SMALLINT      NOT NULL DEFAULT 0,
  "Notas"        TEXT,
  CONSTRAINT uq_pedidos_numero UNIQUE ("NumeroPedido"),
  CONSTRAINT ck_ped_subtotal   CHECK ("Subtotal" >= 0),
  CONSTRAINT ck_ped_impuesto   CHECK ("Impuesto" >= 0),
  CONSTRAINT ck_ped_total      CHECK ("Total" >= 0),
  CONSTRAINT ck_ped_comision   CHECK ("Comision" >= 0),
  CONSTRAINT ck_ped_metodo     CHECK ("MetodoPago" IN (0, 1, 2, 3)),
  CONSTRAINT ck_ped_estado     CHECK ("Estado" IN (0, 1, 2, 3, 4)),
  CONSTRAINT fk_ped_user
    FOREIGN KEY ("UserId")
    REFERENCES public."AspNetUsers"("Id") ON DELETE RESTRICT,
  CONSTRAINT fk_ped_sucursal
    FOREIGN KEY ("SucursalId")
    REFERENCES operaciones."Sucursales"("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS ventas."PedidoItems" (
  "Id"             SERIAL        PRIMARY KEY,
  "PedidoId"       INTEGER       NOT NULL,
  "ComponenteId"   INTEGER,
  "ComboId"        INTEGER,
  "ProductoNombre" VARCHAR(200)  NOT NULL,
  "PrecioUnitario" NUMERIC(18,2) NOT NULL,
  "Cantidad"       INTEGER       NOT NULL DEFAULT 1,
  "Subtotal"       NUMERIC(18,2) NOT NULL,
  "ImageUrl"       VARCHAR(500),
  CONSTRAINT ck_pi_cantidad  CHECK ("Cantidad" > 0),
  CONSTRAINT ck_pi_precio    CHECK ("PrecioUnitario" > 0),
  CONSTRAINT ck_pi_subtotal  CHECK ("Subtotal" > 0),
  CONSTRAINT ck_pi_exclusivo CHECK (
    ("ComponenteId" IS NOT NULL AND "ComboId" IS NULL) OR
    ("ComboId"      IS NOT NULL AND "ComponenteId" IS NULL)
  ),
  CONSTRAINT fk_pi_pedido
    FOREIGN KEY ("PedidoId")
    REFERENCES ventas."Pedidos"("Id") ON DELETE CASCADE,
  CONSTRAINT fk_pi_comp
    FOREIGN KEY ("ComponenteId")
    REFERENCES catalogo."Componentes"("Id") ON DELETE RESTRICT,
  CONSTRAINT fk_pi_combo
    FOREIGN KEY ("ComboId")
    REFERENCES catalogo."Combos"("Id") ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS ventas."CarritosAbandonados" (
  "Id"                   SERIAL        PRIMARY KEY,
  "UserId"               TEXT          NOT NULL,
  "FechaCreacion"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
  "FechaUltimaActividad" TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
  "FechaExpira"          TIMESTAMPTZ,
  "ItemsJson"            JSONB         NOT NULL DEFAULT '[]'::jsonb,
  "TotalEstimado"        NUMERIC(18,2) NOT NULL DEFAULT 0,
  "TotalItems"           INTEGER       NOT NULL DEFAULT 0,
  "Convertido"           BOOLEAN       NOT NULL DEFAULT FALSE,
  CONSTRAINT ck_carr_total CHECK ("TotalEstimado" >= 0),
  CONSTRAINT ck_carr_items CHECK ("TotalItems" >= 0),
  CONSTRAINT fk_carr_user
    FOREIGN KEY ("UserId")
    REFERENCES public."AspNetUsers"("Id") ON DELETE CASCADE
);

-- ============================================================================
-- 5. ESQUEMA AUDITORIA Y CONTROL DE MERMAS (ODS 12)
-- ============================================================================
CREATE TABLE IF NOT EXISTS auditoria."AuditoriaComponentes" (
  "Id"            SERIAL PRIMARY KEY,
  "Operacion"     CHAR(1)       NOT NULL,
  "ComponenteId"  INTEGER,
  "StockAntes"    INTEGER,
  "StockDespues"  INTEGER,
  "PrecioAntes"   NUMERIC(18,2),
  "PrecioDespues" NUMERIC(18,2),
  "EstadoAntes"   VARCHAR(20),
  "EstadoDespues" VARCHAR(20),
  "FechaHora"     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
  "Usuario"       TEXT          NOT NULL DEFAULT current_user,
  CONSTRAINT ck_audit_op CHECK ("Operacion" IN ('I', 'U', 'D'))
);

CREATE TABLE IF NOT EXISTS auditoria."AuditoriaPedidos" (
  "Id"            SERIAL PRIMARY KEY,
  "PedidoId"      INTEGER,
  "EstadoAntes"   SMALLINT,
  "EstadoDespues" SMALLINT,
  "FechaHora"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  "Usuario"       TEXT        NOT NULL DEFAULT current_user
);

CREATE TABLE IF NOT EXISTS auditoria."MermasInventario" (
  "Id"               SERIAL PRIMARY KEY,
  "ComponenteId"     INTEGER       NOT NULL,
  "SucursalId"       INTEGER,
  "Cantidad"         INTEGER       NOT NULL,
  "Motivo"           VARCHAR(50)   NOT NULL,
  "Observaciones"    TEXT,
  "CostoEstimado"    NUMERIC(18,2) NOT NULL DEFAULT 0,
  "DisposicionFinal" VARCHAR(50)   NOT NULL DEFAULT 'ReciclajeCertificado',
  "FechaRegistro"    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
  "UsuarioRegistro"  TEXT          NOT NULL DEFAULT current_user,
  CONSTRAINT ck_merma_cant   CHECK ("Cantidad" > 0),
  CONSTRAINT ck_merma_costo  CHECK ("CostoEstimado" >= 0),
  CONSTRAINT ck_merma_motivo CHECK (
    "Motivo" IN ('Obsolescencia', 'DanoFisico', 'Garantia', 'ReciclajeRAEE', 'Otro')
  ),
  CONSTRAINT ck_merma_disp   CHECK (
    "DisposicionFinal" IN ('ReciclajeCertificado', 'Reacondicionado', 'Donacion', 'BajaDefinitiva')
  ),
  CONSTRAINT fk_merma_comp
    FOREIGN KEY ("ComponenteId")
    REFERENCES catalogo."Componentes"("Id") ON DELETE RESTRICT,
  CONSTRAINT fk_merma_suc
    FOREIGN KEY ("SucursalId")
    REFERENCES operaciones."Sucursales"("Id") ON DELETE SET NULL
);

-- ============================================================================
-- 6. INDICES (6 B-TREE + 1 GIN SOBRE JSONB)
-- ============================================================================
CREATE INDEX IF NOT EXISTS idx_comp_categoria
  ON catalogo."Componentes"("CategoriaId");
CREATE INDEX IF NOT EXISTS idx_comp_marca
  ON catalogo."Componentes"("MarcaId");
CREATE INDEX IF NOT EXISTS idx_comp_precio
  ON catalogo."Componentes"("Precio");
CREATE INDEX IF NOT EXISTS idx_pedidos_user
  ON ventas."Pedidos"("UserId");
CREATE INDEX IF NOT EXISTS idx_pedidos_estado
  ON ventas."Pedidos"("Estado");
CREATE INDEX IF NOT EXISTS idx_pedidos_fecha
  ON ventas."Pedidos"("FechaPedido" DESC);
CREATE INDEX IF NOT EXISTS idx_carritos_json
  ON ventas."CarritosAbandonados" USING GIN ("ItemsJson");

-- ============================================================================
-- 7. VISTAS ANALITICAS (4 VISTAS)
-- ============================================================================
CREATE OR REPLACE VIEW ventas.vw_top_productos AS
SELECT
  c."Id",
  c."Nombre"                      AS producto,
  cat."Nombre"                    AS categoria,
  m."Nombre"                      AS marca,
  SUM(pi."Cantidad")              AS unidades_vendidas,
  SUM(pi."Subtotal")              AS ingreso_total
FROM ventas."PedidoItems" pi
JOIN catalogo."Componentes" c   ON pi."ComponenteId" = c."Id"
JOIN catalogo."Categorias"  cat ON c."CategoriaId"   = cat."Id"
JOIN catalogo."Marcas"      m   ON c."MarcaId"       = m."Id"
JOIN ventas."Pedidos"       p   ON pi."PedidoId"     = p."Id"
WHERE p."Estado" IN (1, 2)
GROUP BY c."Id", c."Nombre", cat."Nombre", m."Nombre"
ORDER BY unidades_vendidas DESC;

CREATE OR REPLACE VIEW ventas.vw_ventas_mensuales AS
SELECT
  DATE_TRUNC('month', p."FechaPedido") AS mes,
  COUNT(DISTINCT p."Id")               AS total_pedidos,
  SUM(p."Subtotal")                    AS subtotal_mensual,
  SUM(p."Impuesto")                    AS iva_mensual,
  SUM(p."Total")                       AS facturacion,
  ROUND(AVG(p."Total"), 2)             AS ticket_promedio
FROM ventas."Pedidos" p
WHERE p."Estado" IN (1, 2)
GROUP BY mes
ORDER BY mes DESC;

CREATE OR REPLACE VIEW ventas.vw_combos_rentabilidad AS
SELECT
  co."Id",
  co."Nombre"                                      AS combo,
  co."PrecioVenta",
  COALESCE(SUM(c."Precio" * cc."Cantidad"), 0)     AS precio_componentes,
  co."Descuento"                                   AS descuento_pct,
  COALESCE(SUM(c."Precio" * cc."Cantidad"), 0) - co."PrecioVenta" AS ahorro_cliente
FROM catalogo."Combos"                co
LEFT JOIN catalogo."ComboComponentes" cc ON cc."ComboId"      = co."Id"
LEFT JOIN catalogo."Componentes"      c  ON cc."ComponenteId" = c."Id"
GROUP BY co."Id", co."Nombre", co."PrecioVenta", co."Descuento";

CREATE OR REPLACE VIEW ventas.vw_clientes_activos AS
SELECT
  u."Id",
  u."NombreCompleto",
  u."Email",
  u."CodigoCliente",
  COUNT(p."Id")        AS total_pedidos,
  SUM(p."Total")       AS gasto_total,
  MAX(p."FechaPedido") AS ultima_compra
FROM public."AspNetUsers" u
JOIN ventas."Pedidos"     p ON p."UserId" = u."Id"
WHERE p."Estado" IN (1, 2)
GROUP BY u."Id", u."NombreCompleto", u."Email", u."CodigoCliente"
ORDER BY gasto_total DESC;

-- ============================================================================
-- 8. FUNCIONES ALMACENADAS (2 FUNCIONES)
-- ============================================================================
CREATE OR REPLACE FUNCTION ventas.fn_top_clientes(p_limite INTEGER DEFAULT 10)
RETURNS TABLE (
  nombre      TEXT,
  email       TEXT,
  num_pedidos BIGINT,
  gasto_total NUMERIC
) LANGUAGE sql STABLE AS $$
  SELECT
    COALESCE(u."NombreCompleto", u."UserName"),
    u."Email",
    COUNT(p."Id"),
    SUM(p."Total")
  FROM public."AspNetUsers" u
  JOIN ventas."Pedidos"     p ON p."UserId" = u."Id"
  WHERE p."Estado" IN (1, 2)
  GROUP BY u."Id", u."NombreCompleto", u."UserName", u."Email"
  ORDER BY SUM(p."Total") DESC
  LIMIT p_limite;
$$;

CREATE OR REPLACE FUNCTION catalogo.fn_recalcular_stock_combo(p_combo_id INTEGER)
RETURNS INTEGER LANGUAGE sql STABLE AS $$
  SELECT COALESCE(
    MIN(FLOOR(c."Stock"::NUMERIC / cc."Cantidad")::INTEGER),
    0
  )
  FROM catalogo."ComboComponentes" cc
  JOIN catalogo."Componentes"      c ON cc."ComponenteId" = c."Id"
  WHERE cc."ComboId" = p_combo_id;
$$;

-- ============================================================================
-- 9. TRIGGERS DE AUDITORIA (2 TRIGGERS)
-- ============================================================================
CREATE OR REPLACE FUNCTION auditoria.fn_audit_componentes()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    INSERT INTO auditoria."AuditoriaComponentes"
      ("Operacion", "ComponenteId", "StockDespues", "PrecioDespues", "EstadoDespues")
    VALUES
      ('I', NEW."Id", NEW."Stock", NEW."Precio", NEW."EstadoObsolescencia");
  ELSIF TG_OP = 'UPDATE' THEN
    INSERT INTO auditoria."AuditoriaComponentes"
      ("Operacion", "ComponenteId",
       "StockAntes", "StockDespues",
       "PrecioAntes", "PrecioDespues",
       "EstadoAntes", "EstadoDespues")
    VALUES
      ('U', NEW."Id",
       OLD."Stock", NEW."Stock",
       OLD."Precio", NEW."Precio",
       OLD."EstadoObsolescencia", NEW."EstadoObsolescencia");
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO auditoria."AuditoriaComponentes"
      ("Operacion", "ComponenteId", "StockAntes", "PrecioAntes", "EstadoAntes")
    VALUES
      ('D', OLD."Id", OLD."Stock", OLD."Precio", OLD."EstadoObsolescencia");
  END IF;
  RETURN COALESCE(NEW, OLD);
END;
$$;

DROP TRIGGER IF EXISTS tg_audit_componentes ON catalogo."Componentes";
CREATE TRIGGER tg_audit_componentes
AFTER INSERT OR UPDATE OR DELETE
ON catalogo."Componentes"
FOR EACH ROW EXECUTE FUNCTION auditoria.fn_audit_componentes();

CREATE OR REPLACE FUNCTION auditoria.fn_audit_pedidos()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP = 'UPDATE' AND OLD."Estado" IS DISTINCT FROM NEW."Estado" THEN
    INSERT INTO auditoria."AuditoriaPedidos"
      ("PedidoId", "EstadoAntes", "EstadoDespues")
    VALUES (NEW."Id", OLD."Estado", NEW."Estado");
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tg_audit_pedidos ON ventas."Pedidos";
CREATE TRIGGER tg_audit_pedidos
AFTER UPDATE OF "Estado"
ON ventas."Pedidos"
FOR EACH ROW EXECUTE FUNCTION auditoria.fn_audit_pedidos();

-- ============================================================================
-- 10. PROCEDIMIENTO TRANSACCIONAL ATOMICO (BLOQUEO PESIMISTA FOR UPDATE)
-- ============================================================================
CREATE OR REPLACE PROCEDURE ventas.sp_confirmar_venta(
  IN    p_user_id       TEXT,
  IN    p_numero_pedido VARCHAR(30),
  IN    p_items         JSONB,
  IN    p_metodo_pago   SMALLINT,
  IN    p_sucursal_id   INTEGER DEFAULT NULL,
  IN    p_notas         TEXT DEFAULT NULL,
  INOUT p_pedido_id     INTEGER DEFAULT NULL
)
LANGUAGE plpgsql AS $$
DECLARE
  v_pedido_id   INTEGER;
  v_subtotal    NUMERIC(18,2) := 0;
  v_iva         NUMERIC(18,2) := 0;
  v_total       NUMERIC(18,2) := 0;
  v_comision    NUMERIC(18,2) := 0;
  v_item        JSONB;
  v_tipo        TEXT;
  v_prod_id     INTEGER;
  v_cantidad    INTEGER;
  v_nombre      VARCHAR(200);
  v_precio      NUMERIC(18,2);
  v_img         VARCHAR(500);
  v_stock_act   INTEGER;
  v_item_sub    NUMERIC(18,2);
  v_cc          RECORD;
BEGIN
  IF p_items IS NULL OR jsonb_typeof(p_items) <> 'array' OR jsonb_array_length(p_items) = 0 THEN
    RAISE EXCEPTION 'El carrito de compra no contiene items validos.';
  END IF;

  INSERT INTO ventas."Pedidos"
    ("NumeroPedido", "UserId", "SucursalId", "FechaPedido", "Subtotal", "Impuesto", "Total", "Comision", "MetodoPago", "Estado", "Notas")
  VALUES
    (p_numero_pedido, p_user_id, p_sucursal_id, NOW(), 0, 0, 0, 0, p_metodo_pago, 0, p_notas)
  RETURNING "Id" INTO v_pedido_id;

  FOR v_item IN SELECT * FROM jsonb_array_elements(p_items) LOOP
    v_tipo     := LOWER(COALESCE(v_item->>'tipo', 'componente'));
    v_prod_id  := (v_item->>'id')::INTEGER;
    v_cantidad := (v_item->>'cantidad')::INTEGER;

    IF v_cantidad IS NULL OR v_cantidad <= 0 THEN
      RAISE EXCEPTION 'Cantidad invalida para producto ID %.', v_prod_id;
    END IF;

    IF v_tipo = 'combo' THEN
      SELECT "Nombre", "PrecioVenta", "Stock", "ImageUrl"
        INTO v_nombre, v_precio, v_stock_act, v_img
      FROM catalogo."Combos"
      WHERE "Id" = v_prod_id AND "Activo" = TRUE
      FOR UPDATE;

      IF NOT FOUND THEN
        RAISE EXCEPTION 'Combo ID % no existe o esta inactivo.', v_prod_id;
      END IF;

      IF v_stock_act < v_cantidad THEN
        RAISE EXCEPTION 'Stock insuficiente para combo "%": disponible %, requerido %.',
          v_nombre, v_stock_act, v_cantidad;
      END IF;

      FOR v_cc IN
        SELECT cc."ComponenteId", cc."Cantidad" * v_cantidad AS req_qty, c."Nombre", c."Stock"
        FROM catalogo."ComboComponentes" cc
        JOIN catalogo."Componentes"      c ON c."Id" = cc."ComponenteId"
        WHERE cc."ComboId" = v_prod_id
        ORDER BY cc."ComponenteId"
        FOR UPDATE OF c
      LOOP
        IF v_cc."Stock" < v_cc.req_qty THEN
          RAISE EXCEPTION 'Stock insuficiente del componente "%" incluido en el combo "%": disponible %, requerido %.',
            v_cc."Nombre", v_nombre, v_cc."Stock", v_cc.req_qty;
        END IF;

        UPDATE catalogo."Componentes"
           SET "Stock" = "Stock" - v_cc.req_qty,
               "ActualizadoEn" = NOW()
         WHERE "Id" = v_cc."ComponenteId";
      END LOOP;

      UPDATE catalogo."Combos"
         SET "Stock" = "Stock" - v_cantidad,
             "ActualizadoEn" = NOW()
       WHERE "Id" = v_prod_id;

      v_item_sub := ROUND(v_precio * v_cantidad, 2);

      INSERT INTO ventas."PedidoItems"
        ("PedidoId", "ComponenteId", "ComboId", "ProductoNombre", "PrecioUnitario", "Cantidad", "Subtotal", "ImageUrl")
      VALUES
        (v_pedido_id, NULL, v_prod_id, v_nombre, v_precio, v_cantidad, v_item_sub, v_img);

    ELSE
      SELECT "Nombre", "Precio", "Stock", "ImageUrl"
        INTO v_nombre, v_precio, v_stock_act, v_img
      FROM catalogo."Componentes"
      WHERE "Id" = v_prod_id AND "Activo" = TRUE
      FOR UPDATE;

      IF NOT FOUND THEN
        RAISE EXCEPTION 'Componente ID % no existe o esta inactivo.', v_prod_id;
      END IF;

      IF v_stock_act < v_cantidad THEN
        RAISE EXCEPTION 'Stock insuficiente para componente "%": disponible %, requerido %.',
          v_nombre, v_stock_act, v_cantidad;
      END IF;

      UPDATE catalogo."Componentes"
         SET "Stock" = "Stock" - v_cantidad,
             "ActualizadoEn" = NOW()
       WHERE "Id" = v_prod_id;

      v_item_sub := ROUND(v_precio * v_cantidad, 2);

      INSERT INTO ventas."PedidoItems"
        ("PedidoId", "ComponenteId", "ComboId", "ProductoNombre", "PrecioUnitario", "Cantidad", "Subtotal", "ImageUrl")
      VALUES
        (v_pedido_id, v_prod_id, NULL, v_nombre, v_precio, v_cantidad, v_item_sub, v_img);
    END IF;

    v_subtotal := v_subtotal + v_item_sub;
  END LOOP;

  v_iva      := ROUND(v_subtotal * 0.16, 2);
  v_total    := v_subtotal + v_iva;
  v_comision := ROUND(v_subtotal * 0.05, 2);

  UPDATE ventas."Pedidos"
     SET "Subtotal" = v_subtotal,
         "Impuesto" = v_iva,
         "Total"    = v_total,
         "Comision" = v_comision,
         "Estado"   = 1
   WHERE "Id" = v_pedido_id;

  UPDATE ventas."CarritosAbandonados"
     SET "Convertido" = TRUE,
         "FechaUltimaActividad" = NOW()
   WHERE "UserId" = p_user_id AND NOT "Convertido";

  p_pedido_id := v_pedido_id;
END;
$$;

-- ============================================================================
-- 11. DATOS SEMILLA REALISTAS (100% IDEMPOTENTES)
-- ============================================================================
INSERT INTO operaciones."Regiones" ("Nombre", "Codigo") VALUES
  ('Cochabamba', 'CBB'),
  ('La Paz',     'LPZ'),
  ('Santa Cruz', 'SCZ'),
  ('Oruro',      'ORU'),
  ('Tarija',     'TJA')
ON CONFLICT ("Codigo") DO NOTHING;

INSERT INTO operaciones."Sucursales"
  ("Nombre", "Direccion", "Ciudad", "Estado", "RegionId", "Telefono", "Email", "Responsable", "Activa")
VALUES
  ('Sucursal Centro',      'Plaza Principal 14 de Septiembre', 'Cochabamba', 'Cochabamba',
    (SELECT "Id" FROM operaciones."Regiones" WHERE "Codigo" = 'CBB'), '44556677', 'centro@techparts.com.bo',      'Roberto Jimenez Cruz', TRUE),
  ('Sucursal Norte',       'Av. America y Libertador',         'Cochabamba', 'Cochabamba',
    (SELECT "Id" FROM operaciones."Regiones" WHERE "Codigo" = 'CBB'), '44556678', 'norte@techparts.com.bo',       'Laura Sanchez Morales', TRUE),
  ('Sucursal Sur',         'Av. Panamericana esq. 6 de Agosto','Cochabamba', 'Cochabamba',
    (SELECT "Id" FROM operaciones."Regiones" WHERE "Codigo" = 'CBB'), '44556679', 'sur@techparts.com.bo',         'Marco Antonio Torrico', TRUE),
  ('Sucursal La Paz',      'Calle Comercio 1230',              'La Paz',     'La Paz',
    (SELECT "Id" FROM operaciones."Regiones" WHERE "Codigo" = 'LPZ'), '22445566', 'lapaz@techparts.com.bo',       'Maria Fernandez Lopez', TRUE),
  ('Sucursal Santa Cruz',  'Av. Cristo Redentor 890',          'Santa Cruz', 'Santa Cruz',
    (SELECT "Id" FROM operaciones."Regiones" WHERE "Codigo" = 'SCZ'), '33445566', 'santacruz@techparts.com.bo',   'Diego Reyes Castillo', TRUE)
ON CONFLICT ("Nombre") DO NOTHING;

INSERT INTO operaciones."Proveedores"
  ("Nombre", "Contacto", "Email", "Telefono", "Ciudad", "Pais", "CategoriaEspecialidad", "SitioWeb", "Activo")
VALUES
  ('NVIDIA Latin America', 'Carlos Mendoza', 'ventas@nvidia.la',       '800123456', 'Miami',      'Estados Unidos', 'Tarjetas de Video',    'https://www.nvidia.com', TRUE),
  ('AMD Bolivia',          'Ana Suarez',     'distribucion@amd.bo',    '800654321', 'Cochabamba', 'Bolivia',        'Procesadores',         'https://www.amd.com', TRUE),
  ('Intel Andina',         'Luis Fernandez', 'sales@intel.com.bo',     '800111222', 'La Paz',     'Bolivia',        'Procesadores',         'https://www.intel.com', TRUE),
  ('ASUS ROG Latam',       'Pedro Gomez',    'rog@asus.la',            '800333444', 'Taipei',     'Taiwan',         'Tarjetas Madre',       'https://rog.asus.com', TRUE),
  ('G.SKILL International', 'Mei Lin',       'latam@gskill.com',       '800555666', 'Taipei',     'Taiwan',         'Memoria RAM',          'https://www.gskill.com', TRUE),
  ('Corsair Distribucion', 'Miguel Rojas',   'b2b@corsair.com',        '800999000', 'Fremont',    'Estados Unidos', 'Fuentes de Poder',     'https://www.corsair.com', TRUE),
  ('LG Electronics BO',    'Pedro Mamani',   'gaming@lg.com.bo',       '800444666', 'Santa Cruz', 'Bolivia',        'Monitores',            'https://www.lg.com', TRUE),
  ('Samsung Electronics',  'Roberto Diaz',   'ssd@samsung.com',        '800444555', 'Seul',       'Corea del Sur',  'Almacenamiento',       'https://www.samsung.com', TRUE),
  ('Logitech G Latam',     'Carla Ruiz',     'gaming@logitech.bo',     '800666777', 'Lausana',    'Suiza',          'Perifericos',          'https://www.logitechg.com', TRUE),
  ('Cooler Master Latam',  'Rodrigo Lagos',  'cm@latam.com',           '800777888', 'Santiago',   'Chile',          'Refrigeracion',        'https://www.coolermaster.com', TRUE)
ON CONFLICT ("Nombre") DO NOTHING;

INSERT INTO catalogo."Categorias" ("Nombre", "Slug", "Descripcion") VALUES
  ('Tarjetas de Video', 'tarjetas-de-video', 'GPUs de alto rendimiento para gaming 4K e inteligencia artificial'),
  ('Procesadores',      'procesadores',      'Unidades centrales de procesamiento Intel y AMD'),
  ('Almacenamiento',    'almacenamiento',    'Unidades de estado solido NVMe PCIe Gen4 y Gen5'),
  ('Tarjetas Madre',    'tarjetas-madre',    'Placas base entusiastas con soporte DDR5 y Wi-Fi 7'),
  ('Memoria RAM',       'memoria-ram',       'Kits de memoria DDR5 de baja latencia'),
  ('Fuentes de Poder',  'fuentes-de-poder',  'Fuentes modulares ATX 3.1 con certificacion 80 Plus'),
  ('Monitores',         'monitores',         'Pantallas gamer OLED e IPS de alta tasa de refresco'),
  ('Perifericos',       'perifericos',       'Mouses, teclados mecanicos y accesorios esports'),
  ('Refrigeracion',     'refrigeracion',     'Sistemas de enfriamiento liquido AIO y ventiladores'),
  ('Cases',             'cases',             'Gabinetes ATX con flujo de aire optimizado')
ON CONFLICT ("Nombre") DO NOTHING;

INSERT INTO catalogo."Marcas" ("Nombre", "Pais") VALUES
  ('NVIDIA',        'Estados Unidos'),
  ('Intel',         'Estados Unidos'),
  ('Samsung',       'Corea del Sur'),
  ('ASUS',          'Taiwan'),
  ('G.Skill',       'Taiwan'),
  ('Corsair',       'Estados Unidos'),
  ('LG',            'Corea del Sur'),
  ('Logitech',      'Suiza'),
  ('Cooler Master', 'Taiwan'),
  ('Razer',         'Estados Unidos'),
  ('AMD',           'Estados Unidos'),
  ('Lian Li',       'Taiwan')
ON CONFLICT ("Nombre") DO NOTHING;

INSERT INTO catalogo."Componentes"
  ("Nombre", "Descripcion", "CategoriaId", "MarcaId", "ProveedorId", "Precio", "Stock", "StockMinimo", "ImageUrl", "Especificaciones", "EstadoObsolescencia")
VALUES
  ('NVIDIA RTX 5080 Ti',
   'GPU de ultima generacion con 24 GB GDDR7, ray tracing y DLSS 4.0 para gaming 4K extremo.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Tarjetas de Video'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'NVIDIA'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'NVIDIA Latin America'),
   19500.00, 15, 5, 'https://pngimg.com/uploads/video_card/video_card_PNG155.png',
   '24 GB GDDR7 | PCIe 5.0 | 450W TDP | DLSS 4.0 | Ray Tracing Gen 4', 'Vigente'),
  ('Intel Core Ultra 9 285K',
   'Procesador de 24 nucleos (8P+16E) a 5.7 GHz boost, socket LGA1851.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Procesadores'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'Intel'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'Intel Andina'),
   6500.00, 28, 5, 'https://pngimg.com/uploads/processor/processor_PNG40.png',
   '24 nucleos | 5.7 GHz boost | LGA1851 | DDR5-6400 | 125W TDP', 'Vigente'),
  ('Samsung 990 Pro 2TB NVMe',
   'SSD PCIe 5.0 con velocidades de lectura hasta 14,800 MB/s. Ideal para workstations.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Almacenamiento'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'Samsung'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'Samsung Electronics'),
   1600.00, 52, 10, 'https://pngimg.com/uploads/hard_disc/hard_disc_PNG8436.png',
   '2 TB | PCIe 5.0 | 14,800 MB/s lectura | M.2 2280', 'Vigente'),
  ('ASUS ROG Maximus Z890 Hero',
   'Tarjeta madre ATX LGA1851, DDR5 hasta 8000 MHz OC, 5x M.2 PCIe 5.0, Wi-Fi 7.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Tarjetas Madre'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'ASUS'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'ASUS ROG Latam'),
   4500.00, 10, 4, 'https://pngimg.com/uploads/motherboard/motherboard_PNG39.png',
   'LGA1851 | DDR5-8000 OC | 5x M.2 | Wi-Fi 7 | Thunderbolt 4', 'Vigente'),
  ('G.SKILL Trident Z5 RGB 64GB DDR5',
   'Kit de memoria DDR5-6400 CL32, 2x32 GB, con iluminacion RGB y XMP 3.0.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Memoria RAM'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'G.Skill'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'G.SKILL International'),
   2400.00, 34, 8, 'https://pngimg.com/uploads/ram/ram_PNG46.png',
   '64 GB (2x32) | DDR5-6400 | CL32 | XMP 3.0 | ARGB', 'Vigente'),
  ('Corsair HX1500i Platinum',
   'Fuente modular 1500W 80+ Platinum con iCUE, proteccion completa y cableado sleeved.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Fuentes de Poder'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'Corsair'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'Corsair Distribucion'),
   2500.00, 20, 5, 'https://pngimg.com/uploads/computer_case/computer_case_PNG27.png',
   '1500W | 80+ Platinum | Full Modular | ATX 3.0 | PCIe 5.0', 'Vigente'),
  ('LG UltraGear 27GS95QE OLED 240Hz',
   'Monitor OLED QHD 27 pulgadas a 240Hz con 0.03ms GTG, HDR True Black 400, HDMI 2.1.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Monitores'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'LG'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'LG Electronics BO'),
   8500.00, 8, 4, 'https://pngimg.com/uploads/monitor/monitor_PNG88.png',
   '27 OLED QHD | 240Hz | 0.03ms | HDR True Black 400 | HDMI 2.1', 'Vigente'),
  ('Logitech G Pro X Superlight 2',
   'Mouse gaming inalambrico 32,000 DPI, 60h de bateria, 60g con sensor HERO 2.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Perifericos'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'Logitech'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'Logitech G Latam'),
   1200.00, 45, 10, 'https://pngimg.com/uploads/mouse_pc/mouse_pc_PNG10207.png',
   '32,000 DPI | 60h bateria | 60g | USB-C | LIGHTSPEED', 'Vigente'),
  ('Cooler Master MasterLiquid 360 ATMOS',
   'Refrigeracion liquida AIO 360mm, 3x ventiladores ARGB 120mm, soporte LGA1851 y AM5.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Refrigeracion'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'Cooler Master'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'Cooler Master Latam'),
   1800.00, 22, 6, 'https://pngimg.com/uploads/computer_fan/computer_fan_PNG82.png',
   '360mm AIO | 3x120mm ARGB | LGA1851 / AM5 | 2800 RPM', 'Vigente'),
  ('Razer BlackWidow V4 Pro',
   'Teclado mecanico inalambrico, switches Razer Yellow, Chroma RGB y dial multimedia.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Perifericos'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'Razer'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'Logitech G Latam'),
   1500.00, 30, 6, 'https://pngimg.com/uploads/keyboard/keyboard_PNG5856.png',
   'Razer Yellow | Wireless | Chroma RGB | Dial multimedia', 'Vigente'),
  ('AMD Ryzen 9 9950X',
   '16 nucleos y 32 hilos con arquitectura Zen 5, socket AM5, PCIe 5.0.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Procesadores'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'AMD'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'AMD Bolivia'),
   6200.00, 15, 5, 'https://pngimg.com/uploads/processor/processor_PNG40.png',
   '16 nucleos | 5.7 GHz boost | AM5 | 170W TDP', 'Vigente'),
  ('ASUS ROG Crosshair X670E Hero',
   'Placa madre entusiasta AM5 para Ryzen serie 7000/9000, 2x PCIe 5.0 x16, Wi-Fi 6E.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Tarjetas Madre'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'ASUS'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'ASUS ROG Latam'),
   4200.00, 12, 4, 'https://pngimg.com/uploads/motherboard/motherboard_PNG39.png',
   'AM5 | X670E | DDR5-6400+ | Wi-Fi 6E | Dual USB4', 'PorDescontinuar'),
  ('AMD Radeon RX 7900 XTX',
   'GPU tope de gama RDNA 3, 24GB GDDR6, DisplayPort 2.1, ideal para 4K.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Tarjetas de Video'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'AMD'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'AMD Bolivia'),
   9800.00, 8, 5, 'https://pngimg.com/uploads/video_card/video_card_PNG155.png',
   '24 GB GDDR6 | RDNA 3 | DP 2.1 | 355W TDP', 'Vigente'),
  ('Corsair MP700 PRO 2TB Gen5',
   'SSD NVMe M.2 PCIe Gen5 x4 con enfriador activo, velocidades hasta 12,400 MB/s.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Almacenamiento'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'Corsair'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'Corsair Distribucion'),
   2100.00, 25, 5, 'https://pngimg.com/uploads/hard_disc/hard_disc_PNG8436.png',
   '2 TB | PCIe 5.0 | 12,400 MB/s | Activo', 'Vigente'),
  ('Lian Li O11 Dynamic EVO',
   'Chasis Mid-Tower ATX de doble camara, paneles de cristal templado reversibles.',
   (SELECT "Id" FROM catalogo."Categorias" WHERE "Nombre" = 'Cases'),
   (SELECT "Id" FROM catalogo."Marcas" WHERE "Nombre" = 'Lian Li'),
   (SELECT "Id" FROM operaciones."Proveedores" WHERE "Nombre" = 'Cooler Master Latam'),
   1450.00, 4, 5, 'https://pngimg.com/uploads/computer_case/computer_case_PNG27.png',
   'ATX Mid-Tower | Cristal Templado | Dual Chamber', 'Vigente')
ON CONFLICT ("Nombre") DO NOTHING;

INSERT INTO catalogo."Combos"
  ("Nombre", "Descripcion", "PrecioVenta", "Descuento", "Stock", "Activo", "ImageUrl")
VALUES
  ('Gaming PC Ultimate',
   'La maquina definitiva para gaming 4K: RTX 5080 Ti + Core Ultra 9 285K + 64GB DDR5 + 2TB NVMe.',
   36800.00, 5.15, 5, TRUE, 'https://pngimg.com/uploads/computer_case/computer_case_PNG27.png'),
  ('Setup Gaming Completo',
   'Combo perifericos premium: monitor OLED 240Hz + mouse Superlight 2 + teclado BlackWidow V4.',
   10500.00, 6.25, 8, TRUE, 'https://pngimg.com/uploads/monitor/monitor_PNG88.png'),
  ('Starter Build DDR5',
   'Base perfecta para armar tu primera PC DDR5: CPU Intel Ultra 9 + Z890 + 64GB DDR5 + SSD NVMe.',
   14250.00, 5.00, 8, TRUE, 'https://pngimg.com/uploads/motherboard/motherboard_PNG39.png')
ON CONFLICT ("Nombre") DO NOTHING;

-- ComboComponentes (resolviendo IDs reales por Nombre)
INSERT INTO catalogo."ComboComponentes" ("ComboId", "ComponenteId", "Cantidad")
SELECT co."Id", c."Id", 1
FROM catalogo."Combos" co
JOIN catalogo."Componentes" c ON c."Nombre" IN (
  'NVIDIA RTX 5080 Ti', 'Intel Core Ultra 9 285K', 'G.SKILL Trident Z5 RGB 64GB DDR5',
  'Samsung 990 Pro 2TB NVMe', 'ASUS ROG Maximus Z890 Hero', 'Corsair HX1500i Platinum',
  'Cooler Master MasterLiquid 360 ATMOS'
)
WHERE co."Nombre" = 'Gaming PC Ultimate'
ON CONFLICT ("ComboId", "ComponenteId") DO NOTHING;

INSERT INTO catalogo."ComboComponentes" ("ComboId", "ComponenteId", "Cantidad")
SELECT co."Id", c."Id", 1
FROM catalogo."Combos" co
JOIN catalogo."Componentes" c ON c."Nombre" IN (
  'LG UltraGear 27GS95QE OLED 240Hz', 'Logitech G Pro X Superlight 2', 'Razer BlackWidow V4 Pro'
)
WHERE co."Nombre" = 'Setup Gaming Completo'
ON CONFLICT ("ComboId", "ComponenteId") DO NOTHING;

INSERT INTO catalogo."ComboComponentes" ("ComboId", "ComponenteId", "Cantidad")
SELECT co."Id", c."Id", 1
FROM catalogo."Combos" co
JOIN catalogo."Componentes" c ON c."Nombre" IN (
  'Intel Core Ultra 9 285K', 'ASUS ROG Maximus Z890 Hero',
  'G.SKILL Trident Z5 RGB 64GB DDR5', 'Samsung 990 Pro 2TB NVMe'
)
WHERE co."Nombre" = 'Starter Build DDR5'
ON CONFLICT ("ComboId", "ComponenteId") DO NOTHING;

-- Mermas iniciales de respaldo ODS 12 (idempotentes)
INSERT INTO auditoria."MermasInventario"
  ("ComponenteId", "SucursalId", "Cantidad", "Motivo", "Observaciones", "CostoEstimado", "DisposicionFinal")
SELECT
  (SELECT "Id" FROM catalogo."Componentes" WHERE "Nombre" = 'ASUS ROG Crosshair X670E Hero'),
  (SELECT "Id" FROM operaciones."Sucursales" WHERE "Nombre" = 'Sucursal Centro'),
  1,
  'Obsolescencia',
  'Unidad de exhibicion reemplazada por generacion X870E.',
  4200.00,
  'Reacondicionado'
WHERE NOT EXISTS (
  SELECT 1 FROM auditoria."MermasInventario" WHERE "Motivo" = 'Obsolescencia'
);

INSERT INTO auditoria."MermasInventario"
  ("ComponenteId", "SucursalId", "Cantidad", "Motivo", "Observaciones", "CostoEstimado", "DisposicionFinal")
SELECT
  (SELECT "Id" FROM catalogo."Componentes" WHERE "Nombre" = 'Logitech G Pro X Superlight 2'),
  (SELECT "Id" FROM operaciones."Sucursales" WHERE "Nombre" = 'Sucursal Norte'),
  1,
  'ReciclajeRAEE',
  'Bateria degradada gestionada mediante punto certificado RAEE Cochabamba.',
  1200.00,
  'ReciclajeCertificado'
WHERE NOT EXISTS (
  SELECT 1 FROM auditoria."MermasInventario" WHERE "Motivo" = 'ReciclajeRAEE'
);

-- ============================================================================
-- 12. NORMALIZACION DE public."AspNetUsers" (ELIMINAR DUPLICADO Y AGREGAR FKs)
-- ============================================================================
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'AspNetUsers' AND column_name = 'FullName'
  ) THEN
    EXECUTE '
      UPDATE public."AspNetUsers"
         SET "NombreCompleto" = COALESCE("NombreCompleto", "FullName")
       WHERE "NombreCompleto" IS NULL AND "FullName" IS NOT NULL;
    ';
    ALTER TABLE public."AspNetUsers" DROP COLUMN IF EXISTS "FullName";
  END IF;

  -- Limpiar referencias huerfanas antes de crear claves foraneas
  UPDATE public."AspNetUsers" u
     SET "SucursalId" = NULL
   WHERE "SucursalId" IS NOT NULL
     AND NOT EXISTS (SELECT 1 FROM operaciones."Sucursales" s WHERE s."Id" = u."SucursalId");

  UPDATE public."AspNetUsers" u
     SET "RegionId" = NULL
   WHERE "RegionId" IS NOT NULL
     AND NOT EXISTS (SELECT 1 FROM operaciones."Regiones" r WHERE r."Id" = u."RegionId");

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_users_sucursal') THEN
    ALTER TABLE public."AspNetUsers"
      ADD CONSTRAINT fk_users_sucursal
      FOREIGN KEY ("SucursalId") REFERENCES operaciones."Sucursales"("Id") ON DELETE SET NULL;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_users_region') THEN
    ALTER TABLE public."AspNetUsers"
      ADD CONSTRAINT fk_users_region
      FOREIGN KEY ("RegionId") REFERENCES operaciones."Regiones"("Id") ON DELETE SET NULL;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_users_codigo_cliente') THEN
    ALTER TABLE public."AspNetUsers"
      ADD CONSTRAINT uq_users_codigo_cliente UNIQUE ("CodigoCliente");
  END IF;
END $$;
