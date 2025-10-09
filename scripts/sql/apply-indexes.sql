-- Apply performance indexes (manual DDL for staging in Docker)
-- NOTE: Ensure this matches EF migration 20250926153500_AddPerformanceIndexes

-- InventoryTransactions composite indexes
DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind='i' AND c.relname='IX_InventoryTransactions_ProductId_Date' AND n.nspname = 'public'
    ) THEN
        CREATE INDEX "IX_InventoryTransactions_ProductId_Date"
            ON "InventoryTransactions" ("ProductId", "Date");
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind='i' AND c.relname='IX_InventoryTransactions_Type_Date' AND n.nspname = 'public'
    ) THEN
        CREATE INDEX "IX_InventoryTransactions_Type_Date"
            ON "InventoryTransactions" ("Type", "Date");
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind='i' AND c.relname='IX_InventoryTransactions_WarehouseId_Type' AND n.nspname = 'public'
    ) THEN
        CREATE INDEX "IX_InventoryTransactions_WarehouseId_Type"
            ON "InventoryTransactions" ("WarehouseId", "Type");
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind='i' AND c.relname='IX_InventoryTransactions_UserId_Date' AND n.nspname = 'public'
    ) THEN
        CREATE INDEX "IX_InventoryTransactions_UserId_Date"
            ON "InventoryTransactions" ("UserId", "Date");
    END IF;
END $$;

-- Products indexes (SKU index removed as SKU column was dropped)

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind='i' AND c.relname='IX_Products_CategoryId_IsActive' AND n.nspname = 'public'
    ) THEN
        CREATE INDEX "IX_Products_CategoryId_IsActive"
            ON "Products" ("CategoryId", "IsActive");
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind='i' AND c.relname='IX_Products_ManufacturerId_IsActive' AND n.nspname = 'public'
    ) THEN
        CREATE INDEX "IX_Products_ManufacturerId_IsActive"
            ON "Products" ("ManufacturerId", "IsActive");
    END IF;
END $$;

-- AuditLogs indexes
DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind='i' AND c.relname='IX_AuditLogs_EntityName_EntityId' AND n.nspname = 'public'
    ) THEN
        CREATE INDEX "IX_AuditLogs_EntityName_EntityId"
            ON "AuditLogs" ("EntityName", "EntityId");
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind='i' AND c.relname='IX_AuditLogs_UserId_Timestamp' AND n.nspname = 'public'
    ) THEN
        CREATE INDEX "IX_AuditLogs_UserId_Timestamp"
            ON "AuditLogs" ("UserId", "Timestamp");
    END IF;
END $$;

