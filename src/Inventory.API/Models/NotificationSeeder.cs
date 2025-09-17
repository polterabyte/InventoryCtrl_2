using Inventory.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Models;

public static class NotificationSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Seed notification templates
        await SeedNotificationTemplatesAsync(context);
        
        // Seed notification rules
        await SeedNotificationRulesAsync(context);
        
        await context.SaveChangesAsync();
    }

    private static async Task SeedNotificationTemplatesAsync(AppDbContext context)
    {
        if (await context.NotificationTemplates.AnyAsync())
            return;

        var templates = new List<NotificationTemplate>
        {
            new()
            {
                Name = "Stock Low Template",
                EventType = "STOCK_LOW",
                SubjectTemplate = "Low Stock Alert: {{Product.Name}}",
                BodyTemplate = "Product '{{Product.Name}}' (SKU: {{Product.SKU}}) is running low on stock. Current quantity: {{Product.Quantity}}, Minimum required: {{Product.MinStock}}",
                NotificationType = "WARNING",
                Category = "STOCK",
                IsActive = true
            },
            new()
            {
                Name = "Stock Out Template",
                EventType = "STOCK_OUT",
                SubjectTemplate = "Out of Stock: {{Product.Name}}",
                BodyTemplate = "Product '{{Product.Name}}' (SKU: {{Product.SKU}}) is now out of stock. Please reorder immediately.",
                NotificationType = "ERROR",
                Category = "STOCK",
                IsActive = true
            },
            new()
            {
                Name = "Transaction Created Template",
                EventType = "TRANSACTION_CREATED",
                SubjectTemplate = "New Transaction: {{Transaction.ProductName}}",
                BodyTemplate = "A new {{Transaction.Type}} transaction has been created for '{{Transaction.ProductName}}' with quantity {{Transaction.Quantity}}",
                NotificationType = "INFO",
                Category = "TRANSACTION",
                IsActive = true
            },
            new()
            {
                Name = "System Maintenance Template",
                EventType = "SYSTEM_MAINTENANCE",
                SubjectTemplate = "System Maintenance: {{Title}}",
                BodyTemplate = "{{Message}}",
                NotificationType = "INFO",
                Category = "SYSTEM",
                IsActive = true
            }
        };

        context.NotificationTemplates.AddRange(templates);
    }

    private static async Task SeedNotificationRulesAsync(AppDbContext context)
    {
        if (await context.NotificationRules.AnyAsync())
            return;

        var rules = new List<NotificationRule>
        {
            new()
            {
                Name = "Stock Low Alert",
                Description = "Triggers when product quantity falls below minimum stock level",
                EventType = "STOCK_LOW",
                NotificationType = "WARNING",
                Category = "STOCK",
                Condition = """{"Product.Quantity": {"operator": "<=", "value": "{{Product.MinStock}}"}, "Product.IsActive": true}""",
                Template = "Product '{{Product.Name}}' (SKU: {{Product.SKU}}) is running low on stock. Current quantity: {{Product.Quantity}}, Minimum required: {{Product.MinStock}}",
                IsActive = true,
                Priority = 5,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "Stock Out Alert",
                Description = "Triggers when product quantity reaches zero",
                EventType = "STOCK_OUT",
                NotificationType = "ERROR",
                Category = "STOCK",
                Condition = """{"Product.Quantity": {"operator": "==", "value": 0}, "Product.IsActive": true}""",
                Template = "Product '{{Product.Name}}' (SKU: {{Product.SKU}}) is now out of stock. Please reorder immediately.",
                IsActive = true,
                Priority = 8,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "High Value Transaction Alert",
                Description = "Triggers for high-value transactions",
                EventType = "TRANSACTION_CREATED",
                NotificationType = "INFO",
                Category = "TRANSACTION",
                Condition = """{"Transaction.Quantity": {"operator": ">=", "value": 100}}""",
                Template = "High-value transaction created: {{Transaction.ProductName}} with quantity {{Transaction.Quantity}}",
                IsActive = true,
                Priority = 3,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "System Error Alert",
                Description = "Triggers for system errors",
                EventType = "SYSTEM_ERROR",
                NotificationType = "ERROR",
                Category = "SYSTEM",
                Condition = """{"Error.Severity": {"operator": "==", "value": "High"}}""",
                Template = "System error occurred: {{Error.Message}}",
                IsActive = true,
                Priority = 9,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.NotificationRules.AddRange(rules);
    }
}
