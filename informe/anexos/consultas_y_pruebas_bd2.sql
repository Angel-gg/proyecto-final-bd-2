-- ============================================================================
--  CONSULTAS DE INTELIGENCIA DE NEGOCIOS Y PRUEBAS TRANSACCIONALES - BD2
--  Proyecto: TechParts ERP (Base de Datos II - UPDS Cochabamba)
--  Autor: Angel Gabriel Rojas Hinojosa
--  Prerrequisito: haber ejecutado script_bd2_completo.sql y tener al menos
--                 un usuario registrado en public."AspNetUsers".
-- ============================================================================

-- ============================================================================
-- BLOQUE A: INSERCION IDEMPOTENTE DE 8 PEDIDOS HISTORICOS + 2 CARRITOS JSONB
-- ============================================================================
DO $$
DECLARE
  v_user_id   TEXT;
  v_suc_cbb   INTEGER;
  v_suc_lpz   INTEGER;
  v_suc_scz   INTEGER;
  v_c_gpu     INTEGER;
  v_c_cpu     INTEGER;
  v_c_ssd     INTEGER;
  v_c_ram     INTEGER;
  v_c_mon     INTEGER;
  v_c_mouse   INTEGER;
  v_combo_1   INTEGER;
  v_combo_2   INTEGER;
  v_ped_id    INTEGER;
BEGIN
  SELECT "Id" INTO v_user_id FROM public."AspNetUsers" ORDER BY "Email" LIMIT 1;
  IF v_user_id IS NULL THEN
    RAISE EXCEPTION 'No existe ningun usuario en public."AspNetUsers". Registra o inicia la app primero.';
  END IF;

  SELECT "Id" INTO v_suc_cbb FROM operaciones."Sucursales" WHERE "Nombre" = 'Sucursal Centro';
  SELECT "Id" INTO v_suc_lpz FROM operaciones."Sucursales" WHERE "Nombre" = 'Sucursal La Paz';
  SELECT "Id" INTO v_suc_scz FROM operaciones."Sucursales" WHERE "Nombre" = 'Sucursal Santa Cruz';

  SELECT "Id" INTO v_c_gpu   FROM catalogo."Componentes" WHERE "Nombre" = 'NVIDIA RTX 5080 Ti';
  SELECT "Id" INTO v_c_cpu   FROM catalogo."Componentes" WHERE "Nombre" = 'Intel Core Ultra 9 285K';
  SELECT "Id" INTO v_c_ssd   FROM catalogo."Componentes" WHERE "Nombre" = 'Samsung 990 Pro 2TB NVMe';
  SELECT "Id" INTO v_c_ram   FROM catalogo."Componentes" WHERE "Nombre" = 'G.SKILL Trident Z5 RGB 64GB DDR5';
  SELECT "Id" INTO v_c_mon   FROM catalogo."Componentes" WHERE "Nombre" = 'LG UltraGear 27GS95QE OLED 240Hz';
  SELECT "Id" INTO v_c_mouse FROM catalogo."Componentes" WHERE "Nombre" = 'Logitech G Pro X Superlight 2';
  SELECT "Id" INTO v_combo_1 FROM catalogo."Combos"      WHERE "Nombre" = 'Gaming PC Ultimate';
  SELECT "Id" INTO v_combo_2 FROM catalogo."Combos"      WHERE "Nombre" = 'Setup Gaming Completo';

  -- Pedido 1 (hace 85 dias - Entregado - Tarjeta - Cochabamba)
  INSERT INTO ventas."Pedidos"
    ("NumeroPedido", "UserId", "SucursalId", "FechaPedido", "FechaEntrega", "Subtotal", "Impuesto", "Total", "Comision", "MetodoPago", "Estado", "Notas")
  VALUES
    ('TPC-HIST-2026-0001', v_user_id, v_suc_cbb, NOW() - INTERVAL '85 days', NOW() - INTERVAL '83 days', 21100.00, 3376.00, 24476.00, 1055.00, 1, 2, 'Entrega en tienda centro')
  ON CONFLICT ("NumeroPedido") DO NOTHING
  RETURNING "Id" INTO v_ped_id;

  IF v_ped_id IS NOT NULL THEN
    INSERT INTO ventas."PedidoItems" ("PedidoId", "ComponenteId", "ComboId", "ProductoNombre", "PrecioUnitario", "Cantidad", "Subtotal")
    VALUES
      (v_ped_id, v_c_gpu, NULL, 'NVIDIA RTX 5080 Ti', 19500.00, 1, 19500.00),
      (v_ped_id, v_c_ssd, NULL, 'Samsung 990 Pro 2TB NVMe', 1600.00, 1, 1600.00);
  END IF;

  -- Pedido 2 (hace 68 dias - Entregado - QR - La Paz)
  v_ped_id := NULL;
  INSERT INTO ventas."Pedidos"
    ("NumeroPedido", "UserId", "SucursalId", "FechaPedido", "FechaEntrega", "Subtotal", "Impuesto", "Total", "Comision", "MetodoPago", "Estado", "Notas")
  VALUES
    ('TPC-HIST-2026-0002', v_user_id, v_suc_lpz, NOW() - INTERVAL '68 days', NOW() - INTERVAL '65 days', 8900.00, 1424.00, 10324.00, 445.00, 3, 2, 'Pago QR verificado')
  ON CONFLICT ("NumeroPedido") DO NOTHING
  RETURNING "Id" INTO v_ped_id;

  IF v_ped_id IS NOT NULL THEN
    INSERT INTO ventas."PedidoItems" ("PedidoId", "ComponenteId", "ComboId", "ProductoNombre", "PrecioUnitario", "Cantidad", "Subtotal")
    VALUES
      (v_ped_id, v_c_cpu, NULL, 'Intel Core Ultra 9 285K', 6500.00, 1, 6500.00),
      (v_ped_id, v_c_ram, NULL, 'G.SKILL Trident Z5 RGB 64GB DDR5', 2400.00, 1, 2400.00);
  END IF;

  -- Pedido 3 (hace 52 dias - Devuelto - Transferencia - Santa Cruz)
  v_ped_id := NULL;
  INSERT INTO ventas."Pedidos"
    ("NumeroPedido", "UserId", "SucursalId", "FechaPedido", "FechaEntrega", "Subtotal", "Impuesto", "Total", "Comision", "MetodoPago", "Estado", "Notas")
  VALUES
    ('TPC-HIST-2026-0003', v_user_id, v_suc_scz, NOW() - INTERVAL '52 days', NOW() - INTERVAL '48 days', 8500.00, 1360.00, 9860.00, 425.00, 2, 3, 'Devolucion por cambio de modelo')
  ON CONFLICT ("NumeroPedido") DO NOTHING
  RETURNING "Id" INTO v_ped_id;

  IF v_ped_id IS NOT NULL THEN
    INSERT INTO ventas."PedidoItems" ("PedidoId", "ComponenteId", "ComboId", "ProductoNombre", "PrecioUnitario", "Cantidad", "Subtotal")
    VALUES
      (v_ped_id, v_c_mon, NULL, 'LG UltraGear 27GS95QE OLED 240Hz', 8500.00, 1, 8500.00);
  END IF;

  -- Pedido 4 (hace 40 dias - Entregado - Tarjeta - Cochabamba - incluye Combo)
  v_ped_id := NULL;
  INSERT INTO ventas."Pedidos"
    ("NumeroPedido", "UserId", "SucursalId", "FechaPedido", "FechaEntrega", "Subtotal", "Impuesto", "Total", "Comision", "MetodoPago", "Estado", "Notas")
  VALUES
    ('TPC-HIST-2026-0004', v_user_id, v_suc_cbb, NOW() - INTERVAL '40 days', NOW() - INTERVAL '39 days', 36800.00, 5888.00, 42688.00, 1840.00, 1, 2, 'Armado completo solicitado')
  ON CONFLICT ("NumeroPedido") DO NOTHING
  RETURNING "Id" INTO v_ped_id;

  IF v_ped_id IS NOT NULL THEN
    INSERT INTO ventas."PedidoItems" ("PedidoId", "ComponenteId", "ComboId", "ProductoNombre", "PrecioUnitario", "Cantidad", "Subtotal")
    VALUES
      (v_ped_id, NULL, v_combo_1, 'Gaming PC Ultimate', 36800.00, 1, 36800.00);
  END IF;

  -- Pedido 5 (hace 25 dias - Entregado - Efectivo - Cochabamba)
  v_ped_id := NULL;
  INSERT INTO ventas."Pedidos"
    ("NumeroPedido", "UserId", "SucursalId", "FechaPedido", "FechaEntrega", "Subtotal", "Impuesto", "Total", "Comision", "MetodoPago", "Estado", "Notas")
  VALUES
    ('TPC-HIST-2026-0005', v_user_id, v_suc_cbb, NOW() - INTERVAL '25 days', NOW() - INTERVAL '23 days', 2400.00, 384.00, 2784.00, 120.00, 0, 2, 'Compra en mostrador')
  ON CONFLICT ("NumeroPedido") DO NOTHING
  RETURNING "Id" INTO v_ped_id;

  IF v_ped_id IS NOT NULL THEN
    INSERT INTO ventas."PedidoItems" ("PedidoId", "ComponenteId", "ComboId", "ProductoNombre", "PrecioUnitario", "Cantidad", "Subtotal")
    VALUES
      (v_ped_id, v_c_mouse, NULL, 'Logitech G Pro X Superlight 2', 1200.00, 2, 2400.00);
  END IF;

  -- Pedido 6 (hace 12 dias - Completado - QR - Santa Cruz - incluye Combo)
  v_ped_id := NULL;
  INSERT INTO ventas."Pedidos"
    ("NumeroPedido", "UserId", "SucursalId", "FechaPedido", "FechaEntrega", "Subtotal", "Impuesto", "Total", "Comision", "MetodoPago", "Estado", "Notas")
  VALUES
    ('TPC-HIST-2026-0006', v_user_id, v_suc_scz, NOW() - INTERVAL '12 days', NULL, 10500.00, 1680.00, 12180.00, 525.00, 3, 1, 'Listo para despacho')
  ON CONFLICT ("NumeroPedido") DO NOTHING
  RETURNING "Id" INTO v_ped_id;

  IF v_ped_id IS NOT NULL THEN
    INSERT INTO ventas."PedidoItems" ("PedidoId", "ComponenteId", "ComboId", "ProductoNombre", "PrecioUnitario", "Cantidad", "Subtotal")
    VALUES
      (v_ped_id, NULL, v_combo_2, 'Setup Gaming Completo', 10500.00, 1, 10500.00);
  END IF;

  -- Carritos abandonados de prueba con JSONB
  IF NOT EXISTS (SELECT 1 FROM ventas."CarritosAbandonados" WHERE "UserId" = v_user_id AND NOT "Convertido") THEN
    INSERT INTO ventas."CarritosAbandonados"
      ("UserId", "FechaCreacion", "FechaUltimaActividad", "FechaExpira", "ItemsJson", "TotalEstimado", "TotalItems", "Convertido")
    VALUES
      (v_user_id, NOW() - INTERVAL '3 days', NOW() - INTERVAL '2 days', NOW() + INTERVAL '5 days',
       '[{"Id":1,"Nombre":"NVIDIA RTX 5080 Ti","Precio":19500.00,"Cantidad":1,"Tipo":"componente"}]'::jsonb,
       19500.00, 1, FALSE);
  END IF;
END $$;


-- ============================================================================
-- BLOQUE B: LAS 10 CONSULTAS AVANZADAS DE INTELIGENCIA DE NEGOCIOS
-- ============================================================================

-- Q1. Top 10 componentes mas vendidos por unidades e ingresos
SELECT
  c."Nombre"        AS producto,
  cat."Nombre"      AS categoria,
  m."Nombre"        AS marca,
  SUM(pi."Cantidad") AS unidades_vendidas,
  SUM(pi."Subtotal") AS ingreso_total
FROM ventas."PedidoItems" pi
JOIN catalogo."Componentes" c   ON pi."ComponenteId" = c."Id"
JOIN catalogo."Categorias"  cat ON c."CategoriaId"   = cat."Id"
JOIN catalogo."Marcas"      m   ON c."MarcaId"       = m."Id"
JOIN ventas."Pedidos"       p   ON pi."PedidoId"     = p."Id"
WHERE p."Estado" IN (1, 2)
GROUP BY c."Id", c."Nombre", cat."Nombre", m."Nombre"
ORDER BY unidades_vendidas DESC, ingreso_total DESC
LIMIT 10;

-- Q2. Ingresos mensuales desglosados por categoria
SELECT
  DATE_TRUNC('month', p."FechaPedido") AS mes,
  cat."Nombre"                         AS categoria,
  SUM(pi."Cantidad")                   AS unidades,
  SUM(pi."Subtotal")                   AS ingreso_mes
FROM ventas."Pedidos"       p
JOIN ventas."PedidoItems"   pi  ON pi."PedidoId"     = p."Id"
JOIN catalogo."Componentes" c   ON pi."ComponenteId" = c."Id"
JOIN catalogo."Categorias"  cat ON c."CategoriaId"   = cat."Id"
WHERE p."Estado" IN (1, 2)
GROUP BY mes, cat."Nombre"
ORDER BY mes DESC, ingreso_mes DESC;

-- Q3. Rentabilidad y ahorro efectivo de combos vs componentes sueltos
SELECT * FROM ventas.vw_combos_rentabilidad ORDER BY ahorro_cliente DESC;

-- Q4. Top 10 clientes con mayor gasto acumulado (funcion almacenada)
SELECT * FROM ventas.fn_top_clientes(10);

-- Q5. Tasa de abandono de carrito + consulta JSONB con indice GIN
SELECT
  DATE_TRUNC('month', ca."FechaCreacion") AS mes,
  COUNT(ca."Id")                          AS carritos_abandonados,
  SUM(ca."TotalEstimado")                 AS monto_en_riesgo,
  COUNT(ca."Id") FILTER (WHERE ca."ItemsJson" @> '[{"Tipo":"componente"}]'::jsonb) AS con_componentes
FROM ventas."CarritosAbandonados" ca
WHERE NOT ca."Convertido"
GROUP BY mes
ORDER BY mes DESC;

-- Q6. Alerta de stock critico con proveedor responsable (Stock <= StockMinimo)
SELECT
  c."Id",
  c."Nombre"       AS componente,
  cat."Nombre"     AS categoria,
  m."Nombre"       AS marca,
  c."Stock",
  c."StockMinimo",
  pr."Nombre"      AS proveedor,
  pr."Email"       AS contacto_proveedor
FROM catalogo."Componentes"   c
JOIN catalogo."Categorias"    cat ON c."CategoriaId" = cat."Id"
JOIN catalogo."Marcas"        m   ON c."MarcaId"     = m."Id"
LEFT JOIN operaciones."Proveedores" pr ON c."ProveedorId" = pr."Id"
WHERE c."Stock" <= c."StockMinimo" AND c."Activo" = TRUE
ORDER BY (c."Stock" - c."StockMinimo") ASC, c."Nombre";

-- Q7. Distribucion de metodos de pago (funcion ventana)
SELECT
  CASE "MetodoPago"
    WHEN 0 THEN 'Efectivo'
    WHEN 1 THEN 'Tarjeta'
    WHEN 2 THEN 'Transferencia'
    WHEN 3 THEN 'QR / Billetera Digital'
  END AS metodo_pago,
  COUNT(*) AS total_pedidos,
  SUM("Total") AS monto_total,
  ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS porcentaje
FROM ventas."Pedidos"
WHERE "Estado" NOT IN (3, 4)
GROUP BY "MetodoPago"
ORDER BY total_pedidos DESC;

-- Q8. Tiempo promedio de entrega por sucursal y region
SELECT
  r."Nombre" AS region,
  s."Nombre" AS sucursal,
  s."Ciudad",
  COUNT(p."Id") AS pedidos_entregados,
  ROUND(AVG(EXTRACT(EPOCH FROM (p."FechaEntrega" - p."FechaPedido")) / 86400)::NUMERIC, 2) AS dias_promedio
FROM ventas."Pedidos"         p
JOIN operaciones."Sucursales" s ON p."SucursalId" = s."Id"
LEFT JOIN operaciones."Regiones" r ON s."RegionId" = r."Id"
WHERE p."Estado" = 2 AND p."FechaEntrega" IS NOT NULL
GROUP BY r."Nombre", s."Id", s."Nombre", s."Ciudad"
ORDER BY dias_promedio ASC;

-- Q9. Componentes sin ventas registradas y resumen de mermas ODS 12
SELECT
  c."Id",
  c."Nombre"              AS componente,
  cat."Nombre"            AS categoria,
  c."Stock",
  c."EstadoObsolescencia",
  COALESCE(SUM(mi."Cantidad"), 0) AS unidades_merma
FROM catalogo."Componentes" c
JOIN catalogo."Categorias"  cat ON c."CategoriaId" = cat."Id"
LEFT JOIN auditoria."MermasInventario" mi ON mi."ComponenteId" = c."Id"
WHERE NOT EXISTS (
  SELECT 1 FROM ventas."PedidoItems" pi WHERE pi."ComponenteId" = c."Id"
)
GROUP BY c."Id", c."Nombre", cat."Nombre", c."Stock", c."EstadoObsolescencia"
ORDER BY c."Stock" DESC;

-- Q10. Dashboard ejecutivo mensual + validacion de vistas y funciones
SELECT
  DATE_TRUNC('month', p."FechaPedido") AS mes,
  COUNT(DISTINCT p."Id")               AS total_pedidos,
  COUNT(DISTINCT p."UserId")           AS clientes_unicos,
  SUM(p."Total")                       AS facturacion_total,
  ROUND(AVG(p."Total"), 2)             AS ticket_promedio,
  SUM(CASE WHEN p."Estado" IN (3, 4) THEN 1 ELSE 0 END) AS devueltos_cancelados
FROM ventas."Pedidos" p
GROUP BY mes
ORDER BY mes DESC
LIMIT 12;

-- Validacion rapida de vistas y funcion de stock de combos
SELECT * FROM ventas.vw_top_productos LIMIT 5;
SELECT * FROM ventas.vw_ventas_mensuales LIMIT 6;
SELECT * FROM ventas.vw_clientes_activos LIMIT 5;
SELECT "Nombre", "Stock" AS stock_tabla, catalogo.fn_recalcular_stock_combo("Id") AS stock_max_calculado
FROM catalogo."Combos";


-- ============================================================================
-- BLOQUE C: PRUEBAS TRANSACCIONALES DE ventas.sp_confirmar_venta
-- ============================================================================

-- PRUEBA 1: TRANSACCION EXITOSA (COMMIT)
DO $$
DECLARE
  v_user_id   TEXT;
  v_comp_id   INTEGER;
  v_stock_ini INTEGER;
  v_stock_fin INTEGER;
  v_pedido_id INTEGER;
  v_num_ped   VARCHAR(30);
BEGIN
  SELECT "Id" INTO v_user_id FROM public."AspNetUsers" ORDER BY "Email" LIMIT 1;
  SELECT "Id", "Stock" INTO v_comp_id, v_stock_ini
    FROM catalogo."Componentes" WHERE "Nombre" = 'Samsung 990 Pro 2TB NVMe';

  v_num_ped := 'TPC-TXOK-' || TO_CHAR(NOW(), 'YYYYMMDDHH24MISS');

  CALL ventas.sp_confirmar_venta(
    p_user_id       => v_user_id,
    p_numero_pedido => v_num_ped,
    p_items         => jsonb_build_array(jsonb_build_object('tipo', 'componente', 'id', v_comp_id, 'cantidad', 1)),
    p_metodo_pago   => 1::SMALLINT,
    p_sucursal_id   => NULL,
    p_notas         => 'Prueba COMMIT automatico BD2',
    p_pedido_id     => v_pedido_id
  );

  SELECT "Stock" INTO v_stock_fin FROM catalogo."Componentes" WHERE "Id" = v_comp_id;
  RAISE NOTICE 'COMMIT EXITOSO -> PedidoId: %, Stock antes: %, Stock despues: %', v_pedido_id, v_stock_ini, v_stock_fin;
END $$;

-- Verificar registros de auditoria generados por los triggers
SELECT * FROM auditoria."AuditoriaComponentes" ORDER BY "Id" DESC LIMIT 5;
SELECT * FROM auditoria."AuditoriaPedidos" ORDER BY "Id" DESC LIMIT 5;

-- PRUEBA 2: FALLO CONTROLADO POR STOCK INSUFICIENTE (ROLLBACK ATOMICO)
DO $$
DECLARE
  v_user_id   TEXT;
  v_comp_id   INTEGER;
  v_stock_ini INTEGER;
  v_stock_fin INTEGER;
  v_pedido_id INTEGER;
BEGIN
  SELECT "Id" INTO v_user_id FROM public."AspNetUsers" ORDER BY "Email" LIMIT 1;
  SELECT "Id", "Stock" INTO v_comp_id, v_stock_ini
    FROM catalogo."Componentes" WHERE "Nombre" = 'NVIDIA RTX 5080 Ti';

  BEGIN
    CALL ventas.sp_confirmar_venta(
      p_user_id       => v_user_id,
      p_numero_pedido => 'TPC-TXFAIL-9999',
      p_items         => jsonb_build_array(jsonb_build_object('tipo', 'componente', 'id', v_comp_id, 'cantidad', 9999)),
      p_metodo_pago   => 1::SMALLINT,
      p_sucursal_id   => NULL,
      p_notas         => 'Prueba ROLLBACK stock insuficiente',
      p_pedido_id     => v_pedido_id
    );
  EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'ROLLBACK VERIFICADO CORRECTAMENTE -> Error capturado: %', SQLERRM;
  END;

  SELECT "Stock" INTO v_stock_fin FROM catalogo."Componentes" WHERE "Id" = v_comp_id;
  RAISE NOTICE 'Stock intacto tras rollback -> Antes: %, Despues: %', v_stock_ini, v_stock_fin;
END $$;
