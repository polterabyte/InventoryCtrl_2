namespace Inventory.Shared.Constants;

public static class ApiEndpoints
{
    public const string BaseUrl = "/api";
    
    // Auth endpoints
    public const string Login = $"{BaseUrl}/auth/login";
    public const string Register = $"{BaseUrl}/auth/register";
    public const string Refresh = $"{BaseUrl}/auth/refresh";
    public const string Logout = $"{BaseUrl}/auth/logout";
    public const string ValidateToken = $"{BaseUrl}/auth/validate";
    public const string UserInfo = $"{BaseUrl}/auth/userinfo";
    
    // Product endpoints
    public const string Products = $"{BaseUrl}/products";
    public const string ProductById = $"{Products}/{{id}}";
    public const string ProductBySku = $"{Products}/sku/{{sku}}";
    public const string ProductStockAdjust = $"{Products}/{{id}}/stock/adjust";
    public const string ProductsByCategory = $"{Products}/category/{{categoryId}}";
    public const string LowStockProducts = $"{Products}/low-stock";
    public const string SearchProducts = $"{Products}/search";
    
    // Category endpoints
    public const string Categories = $"{BaseUrl}/Category";
    public const string CategoryById = $"{Categories}/{{id}}";
    public const string RootCategories = $"{Categories}/root";
    public const string SubCategories = $"{Categories}/{{parentId}}/sub";
    
    // Manufacturer endpoints
    public const string Manufacturers = $"{BaseUrl}/Manufacturer";
    public const string ManufacturerById = $"{Manufacturers}/{{id}}";
    
    // ProductGroup endpoints
    public const string ProductGroups = $"{BaseUrl}/ProductGroup";
    public const string ProductGroupById = $"{ProductGroups}/{{id}}";
    
    // ProductModel endpoints
    public const string ProductModels = $"{BaseUrl}/ProductModel";
    public const string ProductModelById = $"{ProductModels}/{{id}}";
    
    // UnitOfMeasure endpoints
    public const string UnitOfMeasures = $"{BaseUrl}/UnitOfMeasure";
    
    // Notification endpoints
    public const string Notifications = $"{BaseUrl}/Notification";
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
    public const string Warehouses = $"{BaseUrl}/warehouses";
    public const string WarehouseById = $"{Warehouses}/{{id}}";
    
    // Transaction endpoints
    public const string Transactions = $"{BaseUrl}/transactions";
    public const string TransactionById = $"{Transactions}/{{id}}";
    public const string TransactionsByProduct = $"{Transactions}/product/{{productId}}";
    
    // Dashboard endpoints
    public const string Dashboard = $"{BaseUrl}/dashboard";
    public const string DashboardStats = $"{Dashboard}/stats";
    public const string DashboardRecentActivity = $"{Dashboard}/recent-activity";
    public const string DashboardLowStockProducts = $"{Dashboard}/low-stock-products";
}
