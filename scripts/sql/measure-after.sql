-- Post-index profiling — re-run after applying EF migration with new indexes
\timing on
SET random_page_cost = 1.1;
SET enable_seqscan = on;

-- Same queries as measure-before.sql for apples-to-apples comparison
EXPLAIN (ANALYZE, BUFFERS, VERBOSE) 
SELECT *
FROM "InventoryTransactions"
WHERE "ProductId" = 123 AND "Date" BETWEEN now() - interval '90 days' AND now()
ORDER BY "Date" DESC
LIMIT 100;

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT count(*)
FROM "InventoryTransactions"
WHERE "Type" = 0 AND "Date" >= now() - interval '30 days';

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT *
FROM "InventoryTransactions"
WHERE "WarehouseId" = 1 AND "Type" IN (0,1,2)
ORDER BY "Date" DESC
LIMIT 100;

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT *
FROM "InventoryTransactions"
WHERE "UserId" = 'some-user-id' AND "Date" >= now() - interval '7 days'
ORDER BY "Date" DESC
LIMIT 100;

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT "Id","Name","SKU"
FROM "Products"
WHERE "CategoryId" = 1 AND "IsActive" = true
ORDER BY "Name"
LIMIT 50;

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT "Id","Name","SKU"
FROM "Products"
WHERE lower("Name") LIKE lower('%widget%') OR lower("SKU") LIKE lower('%abc%')
ORDER BY "Name"
LIMIT 50;

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT *
FROM "AuditLogs"
WHERE "EntityName" = 'Product' AND "EntityId" = '42'
ORDER BY "Timestamp" DESC
LIMIT 100;

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT *
FROM "AuditLogs"
WHERE "UserId" = 'some-user-id' AND "Timestamp" >= now() - interval '30 days'
ORDER BY "Timestamp" DESC
LIMIT 100;

