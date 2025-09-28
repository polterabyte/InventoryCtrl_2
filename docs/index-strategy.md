Index Strategy Proposal

Scope
- Tables: InventoryTransactions, Products, AuditLogs
- DB: PostgreSQL (per appsettings + Npgsql in migrations)

Observed read/write patterns (from code)
- InventoryTransactions
  - Filters: ProductId, WarehouseId, Type, Date range, UserId, LocationId
  - Sorting: Date DESC
  - Endpoints: src/Inventory.API/Controllers/TransactionController.cs
- Products
  - Filters: CategoryId, ManufacturerId, IsActive (default true for non-admin), search by Name/SKU (LIKE), pagination, OrderBy Name
  - Endpoints: src/Inventory.API/Controllers/ProductController.cs
- AuditLogs
  - Filters: UserId, EntityName + EntityId, Timestamp range; cleanup by age
  - Sorting: Timestamp DESC
  - Service: src/Inventory.API/Services/AuditService.cs

Existing indexes (from migrations)
- InventoryTransactions: single-column indexes on ProductId, WarehouseId, UserId, LocationId (and FK-related)
- Products: FK indexes (CategoryId, ManufacturerId, ProductGroupId, ProductModelId, UnitOfMeasureId)
- AuditLogs: index on UserId

Recommended composite/unique indexes
- InventoryTransactions
  1) (ProductId, Date)
  2) (Type, Date)
  3) (WarehouseId, Type)
  4) (UserId, Date)
  Rationale: aligns with frequent filters and Date sort; improves range scans and recent-activity queries.

- Products
  1) UNIQUE (SKU)
  2) (CategoryId, IsActive)
  3) (ManufacturerId, IsActive)
  Optional (depending on search usage and extension):
  4) GIN trigram index on lower(Name), lower(SKU) for LIKE/ILIKE search (pg_trgm)
  Rationale: SKU uniqueness and faster filtering by category/manufacturer + active flag; better search performance.

- AuditLogs
  1) (EntityName, EntityId)
  2) (UserId, Timestamp)
  3) (Timestamp) if large scans by time window are common
  Rationale: common lookup patterns and descending time queries.

Views and reporting
- Current views aggregate by ProductId with SUM/CASE and joins. No direct index changes required, but the above indexes support join/selectivity. If heavy read pressure: consider materialized views later (see task #12) with refresh strategy.

Measurement plan
- Capture baselines for key endpoints/queries before adding indexes (avg/95p latency, rows examined, buffers)
- Add indexes via EF migration (task #2)
- Re-test under similar load and report delta; rollback if regressions appear.

How to run profiling (PostgreSQL)
- Prereqs: psql client; DB connection matches appsettings/Compose. You can set env: `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`.
- Run baseline profiling queries with buffers and timing:
  - Option A (psql one-liner):
    `psql "host=$Env:PGHOST port=$Env:PGPORT dbname=$Env:PGDATABASE user=$Env:PGUSER password=$Env:PGPASSWORD" -v ON_ERROR_STOP=1 -f scripts/sql/measure-before.sql`
  - Option B (Docker): `docker exec -e PGPASSWORD=$POSTGRES_PASSWORD <postgres_container> psql -h localhost -U $POSTGRES_USER -d $POSTGRES_DB -f /work/scripts/sql/measure-before.sql`

Files added
- scripts/sql/measure-before.sql — EXPLAIN (ANALYZE, BUFFERS) for typical filters/sorts across InventoryTransactions, Products, AuditLogs.
- scripts/sql/measure-after.sql — same set to re-run after indexes are applied (task #2).


Risks / notes
- Over-indexing increases write cost; choose minimal set matching observed patterns.
- If LIKE searches are critical and frequent, enable pg_trgm and add GIN only then.
