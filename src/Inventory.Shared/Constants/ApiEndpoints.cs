namespace Inventory.Shared.Constants;

public static class ApiEndpoints
{
    public const string BaseUrl = "/api";
    
    // Health endpoint
    public const string Health = "/health";
    
    // Auth endpoints
    public const string Login = "/auth/login";
    public const string Register = "/auth/register";
    public const string Refresh = "/auth/refresh";
    public const string Logout = "/auth/logout";
    public const string ValidateToken = "/auth/validate";
    public const string UserInfo = "/auth/userinfo";
    
    // Product endpoints
    public const string Products = "/products";
    public const string ProductById = $"{Products}/{{id}}";
    public const string ProductBySku = $"{Products}/sku/{{sku}}";
    public const string ProductStockAdjust = $"{Products}/{{id}}/stock/adjust";
    public const string ProductsByCategory = $"{Products}/category/{{categoryId}}";
    public const string LowStockProducts = $"{Products}/low-stock";
    public const string SearchProducts = $"{Products}/search";
    
    // Category endpoints
    public const string Categories = "/Category";
    public const string CategoryById = $"{Categories}/{{id}}";
    public const string RootCategories = $"{Categories}/root";
    public const string SubCategories = $"{Categories}/{{parentId}}/sub";
    
    // Manufacturer endpoints
    public const string Manufacturers = "/Manufacturer";
    public const string ManufacturerById = $"{Manufacturers}/{{id}}";
    
    // ProductGroup endpoints
    public const string ProductGroups = "/ProductGroup";
    public const string ProductGroupById = $"{ProductGroups}/{{id}}";
    public const string ProductGroupAll = $"{ProductGroups}/all";
    
    // ProductModel endpoints
    public const string ProductModels = "/ProductModel";
    public const string ProductModelById = $"{ProductModels}/{{id}}";
    
    // UnitOfMeasure endpoints
    public const string UnitOfMeasures = "/UnitOfMeasure";
    
    // Notification endpoints
    public const string Notifications = "/Notification";
    public const string NotificationById = $"{Notifications}/{{id}}";
    public const string NotificationStats = $"{Notifications}/stats";
    public const string NotificationPreferences = $"{Notifications}/preferences";
    public const string NotificationRules = $"{Notifications}/rules";
    public const string NotificationRuleById = $"{NotificationRules}/{{id}}";
    public const string NotificationBulk = $"{Notifications}/bulk";
    public const string NotificationCleanup = $"{Notifications}/cleanup";
    public const string UnitOfMeasureById = $"{UnitOfMeasures}/{{id}}";
    public const string UnitOfMeasureAll = $"{UnitOfMeasures}/all";
    public const string UnitOfMeasureExists = $"{UnitOfMeasures}/exists";
    public const string UnitOfMeasureCount = $"{UnitOfMeasures}/count";
    
    // Warehouse endpoints
    public const string Warehouses = "/Warehouse";
    public const string WarehouseById = $"{Warehouses}/{{id}}";

    // KanbanCard endpoints
    public const string KanbanCards = "/kanban-cards";
    public const string KanbanCardById = $"{KanbanCards}/{{id}}";
    public const string KanbanCardsByProduct = $"{KanbanCards}/product/{{productId}}";
    public const string KanbanCardsByWarehouse = $"{KanbanCards}/warehouse/{{warehouseId}}";
    
    // Location endpoints
    public const string Locations = "/Location";
    public const string LocationById = $"{Locations}/{{id}}";
    public const string LocationByParentId = $"{Locations}/parent/{{parentId}}";
    public const string RootLocations = $"{Locations}/root";
    public const string AllLocations = $"{Locations}/all";
    
    // Transaction endpoints
    public const string Transactions = "/transactions";
    public const string TransactionById = $"{Transactions}/{{id}}";
    public const string TransactionsByProduct = $"{Transactions}/product/{{productId}}";
    
    // Dashboard endpoints
    public const string Dashboard = "/dashboard";
    public const string DashboardStats = $"{Dashboard}/stats";
    public const string DashboardRecentActivity = $"{Dashboard}/recent-activity";
    public const string DashboardLowStockProducts = $"{Dashboard}/low-stock-products";
    public const string DashboardLowStockKanban = $"{Dashboard}/low-stock-kanban";
    
    // Request endpoints
    public const string Requests = "/requests";
    public const string RequestById = $"{Requests}/{{id}}";
    public const string RequestSubmit = $"{Requests}/{{id}}/submit";
    public const string RequestApprove = $"{Requests}/{{id}}/approve";
    public const string RequestReceived = $"{Requests}/{{id}}/received";
    public const string RequestInstalled = $"{Requests}/{{id}}/installed";
    public const string RequestComplete = $"{Requests}/{{id}}/complete";
    public const string RequestCancel = $"{Requests}/{{id}}/cancel";
    public const string RequestReject = $"{Requests}/{{id}}/reject";
    public const string RequestItems = $"{Requests}/{{id}}/items";
    
    // Audit endpoints
    public const string Audit = "/audit";
    public const string AuditExport = $"{Audit}/export";
    public const string AuditEntity = $"{Audit}/entity/{{entityType}}/{{entityId}}";
    public const string AuditUser = $"{Audit}/user/{{userId}}";
    public const string AuditTrace = $"{Audit}/trace/{{requestId}}";
    public const string AuditById = $"{Audit}/{{id}}";
}
