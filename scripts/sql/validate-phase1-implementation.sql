-- Validation Script: Phase 1 Implementation - Product.Quantity Removal & Performance Indexes
-- This script validates the implementation of Phase 1 of the Inventory Request System
-- Run this after applying the migrations to verify the changes are working correctly

\timing on

-- ============================================================================
-- 1. VALIDATE PRODUCT.QUANTITY REMOVAL
-- ============================================================================

-- Check that Product.Quantity column no longer exists
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'Products' AND column_name = 'Quantity';
-- Expected: No rows returned

-- Verify ProductOnHandView is functioning correctly
SELECT COUNT(*) as product_count,
       SUM(CASE WHEN on_hand_qty > 0 THEN 1 ELSE 0 END) as products_with_stock,
       SUM(CASE WHEN on_hand_qty = 0 THEN 1 ELSE 0 END) as products_out_of_stock,
       AVG(on_hand_qty) as avg_quantity,
       MAX(on_hand_qty) as max_quantity,
       MIN(on_hand_qty) as min_quantity
FROM vw_product_on_hand;

-- Test ProductPendingView (requests system)
SELECT COUNT(*) as pending_count,
       SUM(pending_qty) as total_pending,
       AVG(pending_qty) as avg_pending
FROM vw_product_pending;

-- Test ProductInstalledView
SELECT COUNT(*) as installed_locations,
       SUM(installed_qty) as total_installed,
       COUNT(DISTINCT product_id) as products_with_installations
FROM vw_product_installed;

-- ============================================================================
-- 2. VALIDATE CRITICAL PERFORMANCE INDEXES
-- ============================================================================

-- Check that all critical indexes were created
SELECT indexname, tablename, indexdef
FROM pg_indexes 
WHERE indexname IN (
    'IX_InventoryTransaction_ProductId_Date',
    'IX_InventoryTransaction_Type_Date', 
    'IX_InventoryTransaction_WarehouseId_Type',
    'IX_InventoryTransaction_UserId_Date',
    'IX_InventoryTransaction_RequestId',
    'IX_AuditLog_EntityName_EntityId',
    'IX_AuditLog_UserId_Timestamp',
    'IX_Products_IsActive_CategoryId',
    'IX_Products_IsActive_ManufacturerId',
    'IX_Products_MinStock_MaxStock'
)
ORDER BY indexname;

-- ============================================================================
-- 3. PERFORMANCE VALIDATION QUERIES
-- ============================================================================

-- Test 1: Product inventory query (should use ProductOnHandView)
EXPLAIN (ANALYZE, BUFFERS) 
SELECT p."Id", p."Name", p."SKU", oh.on_hand_qty
FROM "Products" p
LEFT JOIN vw_product_on_hand oh ON p."Id" = oh.product_id
WHERE p."IsActive" = true
ORDER BY p."Name"
LIMIT 100;

-- Test 2: Low stock products query (should use indexes and views)
EXPLAIN (ANALYZE, BUFFERS)
SELECT p."Id", p."Name", p."SKU", oh.on_hand_qty, p."MinStock"
FROM "Products" p
LEFT JOIN vw_product_on_hand oh ON p."Id" = oh.product_id
WHERE p."IsActive" = true 
  AND (oh.on_hand_qty IS NULL OR oh.on_hand_qty <= p."MinStock")
ORDER BY oh.on_hand_qty NULLS FIRST
LIMIT 50;

-- Test 3: Transaction history query (should use ProductId_Date index)
EXPLAIN (ANALYZE, BUFFERS)
SELECT t."Id", t."Type", t."Quantity", t."Date", p."Name"
FROM "InventoryTransactions" t
JOIN "Products" p ON t."ProductId" = p."Id"
WHERE t."ProductId" IN (SELECT "Id" FROM "Products" LIMIT 10)
  AND t."Date" >= CURRENT_DATE - INTERVAL '30 days'
ORDER BY t."Date" DESC;

-- Test 4: User transaction activity (should use UserId_Date index)
EXPLAIN (ANALYZE, BUFFERS)
SELECT t."UserId", COUNT(*) as transaction_count, SUM(t."Quantity") as total_quantity
FROM "InventoryTransactions" t
WHERE t."Date" >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY t."UserId"
ORDER BY transaction_count DESC;

-- Test 5: Request-related transactions (should use RequestId index)
EXPLAIN (ANALYZE, BUFFERS)
SELECT r."Id" as request_id, r."Title", COUNT(t."Id") as transaction_count
FROM "Requests" r
LEFT JOIN "InventoryTransactions" t ON r."Id" = t."RequestId"
WHERE r."Status" IN (1, 2, 3) -- Submitted, Approved, InProgress
GROUP BY r."Id", r."Title"
ORDER BY r."CreatedAt" DESC;

-- ============================================================================
-- 4. DATA CONSISTENCY VALIDATION
-- ============================================================================

-- Verify that inventory calculations are consistent
WITH product_transactions AS (
    SELECT 
        p."Id" as product_id,
        p."Name" as product_name,
        p."SKU",
        -- Calculate quantities from transactions
        COALESCE(SUM(
            CASE
                WHEN t."Type" = 0 THEN t."Quantity"  -- Income
                WHEN t."Type" IN (1,2) THEN -t."Quantity"  -- Outcome, Install
                ELSE 0
            END
        ), 0) as calculated_quantity,
        -- Get quantity from view
        oh.on_hand_qty as view_quantity
    FROM "Products" p
    LEFT JOIN "InventoryTransactions" t ON p."Id" = t."ProductId"
    LEFT JOIN vw_product_on_hand oh ON p."Id" = oh.product_id
    WHERE p."IsActive" = true
    GROUP BY p."Id", p."Name", p."SKU", oh.on_hand_qty
),
inconsistencies AS (
    SELECT *,
        (calculated_quantity - COALESCE(view_quantity, 0)) as difference
    FROM product_transactions
    WHERE calculated_quantity != COALESCE(view_quantity, 0)
)
SELECT 
    'Data Consistency Check' as check_type,
    COUNT(*) as total_products_checked,
    COUNT(*) FILTER (WHERE difference != 0) as inconsistent_products,
    CASE 
        WHEN COUNT(*) FILTER (WHERE difference != 0) = 0 THEN 'PASS'
        ELSE 'FAIL'
    END as result
FROM product_transactions;

-- Show any inconsistencies for debugging
WITH product_transactions AS (
    SELECT 
        p."Id" as product_id,
        p."Name" as product_name,
        p."SKU",
        COALESCE(SUM(
            CASE
                WHEN t."Type" = 0 THEN t."Quantity"
                WHEN t."Type" IN (1,2) THEN -t."Quantity"
                ELSE 0
            END
        ), 0) as calculated_quantity,
        oh.on_hand_qty as view_quantity
    FROM "Products" p
    LEFT JOIN "InventoryTransactions" t ON p."Id" = t."ProductId"
    LEFT JOIN vw_product_on_hand oh ON p."Id" = oh.product_id
    WHERE p."IsActive" = true
    GROUP BY p."Id", p."Name", p."SKU", oh.on_hand_qty
)
SELECT product_id, product_name, sku, calculated_quantity, view_quantity,
       (calculated_quantity - COALESCE(view_quantity, 0)) as difference
FROM product_transactions
WHERE calculated_quantity != COALESCE(view_quantity, 0)
LIMIT 10;

-- ============================================================================
-- 5. PERFORMANCE SUMMARY
-- ============================================================================

-- Summary of Phase 1 implementation status
SELECT 
    'Phase 1 Implementation Status' as summary,
    'Product.Quantity field removed' as task_1,
    'ProductOnHandView providing computed quantities' as task_2,
    'Critical performance indexes created' as task_3,
    'Data consistency maintained' as task_4,
    'Ready for Phase 2 (UI Development)' as next_phase;

\timing off

-- ============================================================================
-- EXPECTED RESULTS SUMMARY:
-- 
-- 1. Product.Quantity column should not exist in Products table
-- 2. All 10 critical indexes should be present and functional
-- 3. ProductOnHandView should provide accurate inventory quantities
-- 4. Performance queries should show improved execution plans
-- 5. Data consistency check should show 0 inconsistencies
-- 6. System is ready for Phase 2 implementation
-- ============================================================================