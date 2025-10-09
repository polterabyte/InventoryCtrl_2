-- Reconciliation report: Products.Quantity vs computed on-hand from transactions
-- Uses existing view vw_product_on_hand (created by SqlViewInitializer)
\timing on

WITH on_hand AS (
  SELECT v.product_id, v.on_hand_qty
  FROM vw_product_on_hand v
)
SELECT 
  p."Id"              AS product_id,
  p."Name"            AS product_name,

  p."Quantity"        AS product_quantity,
  COALESCE(o.on_hand_qty, 0) AS computed_on_hand,
  (COALESCE(o.on_hand_qty, 0) - p."Quantity") AS delta
FROM "Products" p
LEFT JOIN on_hand o ON o.product_id = p."Id"
ORDER BY ABS(COALESCE(o.on_hand_qty, 0) - p."Quantity") DESC, p."Name"
LIMIT 200;

