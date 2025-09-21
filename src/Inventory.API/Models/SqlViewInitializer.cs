using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Models;

public static class SqlViewInitializer
{
    public static async Task EnsureViewsAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        // vw_product_pending
        await db.Database.ExecuteSqlRawAsync(@"
CREATE OR REPLACE VIEW vw_product_pending AS
SELECT
  p.id            AS product_id,
  p.name          AS product_name,
  p.""SKU""        AS sku,
  COALESCE(SUM(t.""Quantity""), 0) AS pending_qty,
  MIN(t.""Date"") AS first_pending_date,
  MAX(t.""Date"") AS last_pending_date
FROM ""Products"" p
LEFT JOIN ""InventoryTransactions"" t ON t.""ProductId"" = p.id AND t.""Type"" = 3
GROUP BY p.id, p.name, p.""SKU"";
", cancellationToken);

        // vw_product_on_hand
        await db.Database.ExecuteSqlRawAsync(@"
CREATE OR REPLACE VIEW vw_product_on_hand AS
SELECT
  p.id AS product_id,
  p.name AS product_name,
  p.""SKU"" AS sku,
  COALESCE(SUM(
    CASE
      WHEN t.""Type"" = 0 THEN t.""Quantity""
      WHEN t.""Type"" IN (1,2) THEN -t.""Quantity""
      ELSE 0
    END
  ), 0) AS on_hand_qty
FROM ""Products"" p
LEFT JOIN ""InventoryTransactions"" t ON t.""ProductId"" = p.id
GROUP BY p.id, p.name, p.""SKU"";
", cancellationToken);

        // vw_product_installed
        await db.Database.ExecuteSqlRawAsync(@"
CREATE OR REPLACE VIEW vw_product_installed AS
SELECT
  p.id AS product_id,
  p.name AS product_name,
  p.""SKU"" AS sku,
  l.id AS location_id,
  l.name AS location_name,
  COALESCE(SUM(t.""Quantity""), 0) AS installed_qty,
  MIN(t.""Date"") AS first_install_date,
  MAX(t.""Date"") AS last_install_date
FROM ""InventoryTransactions"" t
JOIN ""Products"" p ON p.id = t.""ProductId""
LEFT JOIN ""Locations"" l ON l.id = t.""LocationId""
WHERE t.""Type"" = 2
GROUP BY p.id, p.name, p.""SKU"", l.id, l.name;
", cancellationToken);
    }
}


