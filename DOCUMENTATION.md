# Inventory Control System - Complete Documentation

## Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Design System](#design-system)
4. [Notification System](#notification-system)
5. [Port Configuration](#port-configuration)
6. [Launch Instructions](#launch-instructions)
7. [Testing Guide](#testing-guide)
8. [Development Guidelines](#development-guidelines)

---

## Project Overview

### Description
Веб-приложение для управления инвентарем на ASP.NET Core 9 + Blazor WebAssembly

### Project Structure
- `src/Inventory.API` — серверная часть (ASP.NET Core Web API, PostgreSQL)
- `src/Inventory.Web.Client` — клиентская часть (Blazor WebAssembly)
- `src/Inventory.UI` — Razor Class Library (компоненты, стили, страницы)
- `src/Inventory.Shared` — общие компоненты, модели и сервисы

### Quick Start

#### Automatic Launch (Recommended)
```powershell
# Full launch with port checks
.\start-apps.ps1

# Quick launch without checks
.\start-apps.ps1 -Quick

# Show help
.\start-apps.ps1 -Help
```

#### Manual Launch
1. Install .NET 9 SDK
2. Configure PostgreSQL connection string in `src/Inventory.API/appsettings.json`
3. Start API server:
   ```bash
   dotnet run --project "src/Inventory.API/Inventory.API.csproj"
   ```
4. In new terminal, start Web client:
   ```bash
   dotnet run --project "src/Inventory.Web.Client/Inventory.Web.Client.csproj"
   ```

### Application Access
- **Web Application**: https://localhost:5001
- **API Documentation**: https://localhost:7000/swagger

### Technologies

#### Backend
- **ASP.NET Core 9.0** — web framework
- **Entity Framework Core** — ORM for database
- **PostgreSQL** — main database
- **ASP.NET Core Identity** — authentication and authorization system
- **JWT Authentication** — token authentication
- **Serilog** — structured logging
- **Swagger/OpenAPI** — API documentation

#### Frontend
- **Blazor WebAssembly** — client-side web platform
- **Blazored.LocalStorage** — local data storage
- **Bootstrap** — CSS framework for UI
- **Microsoft.AspNetCore.Components.Authorization** — authorization

#### Shared
- **.NET 9.0 Standard Library** — common components
- **HTTP Client** — API clients
- **Common models and DTOs** — typed contracts

### Architecture Features
- **Centralized package management** through `Directory.Packages.props`
- **Shared project** for code reuse between clients
- **Entity Framework Core migration support**
- **Global exception handling** through middleware
- **Structured logging** with Serilog
- **JWT authentication** with role model (Admin, User, Manager)
- **CORS settings** for multiple client support
- **Preparation for mobile application** (.NET MAUI)

---

## Architecture

### Project Structure
```
InventoryCtrl_2/
├── src/
│   ├── Inventory.API/          # ASP.NET Core Web API
│   │   ├── Controllers/        # API controllers
│   │   ├── Models/            # Entity Framework models
│   │   ├── Migrations/        # Database migrations
│   │   └── Program.cs         # Server configuration
│   │
│   ├── Inventory.Web.Client/   # Blazor WebAssembly client
│   │   ├── Pages/             # Razor pages
│   │   ├── Services/          # Client services
│   │   └── Program.cs         # Client configuration
│   │
│   ├── Inventory.UI/           # Razor Class Library
│   │   ├── Components/        # Reusable Razor components
│   │   ├── Layout/            # Layout components
│   │   ├── Pages/             # Page components
│   │   └── wwwroot/           # Static assets
│   │
│   └── Inventory.Shared/       # Common components
│       ├── Models/            # Common data models
│       ├── DTOs/              # Data Transfer Objects
│       ├── Interfaces/        # Service interfaces
│       ├── Services/          # API services
│       └── Constants/         # Constants and settings
```

### Shared Project - Common Components

#### Models/ - Data Models
**Note**: API project uses models from `Inventory.API.Models` namespace, while Shared project uses `Inventory.Shared.Models` namespace.

**API Models** (used in database context):
- `Product.cs` - Product entity
- `Category.cs` - Category entity  
- `Manufacturer.cs` - Manufacturer entity
- `ProductModel.cs` - Product model entity
- `ProductGroup.cs` - Product group entity
- `Warehouse.cs` - Warehouse entity
- `Location.cs` - Location entity
- `InventoryTransaction.cs` - Inventory transaction entity
- `User.cs` - User entity (extends IdentityUser)

**Shared Models** (used in DTOs and client communication):
- `Product.cs` - Product model for client
- `Category.cs` - Category model for client
- `Manufacturer.cs` - Manufacturer model for client
- `ProductModel.cs` - Product model for client
- `ProductGroup.cs` - Product group model for client
- `Warehouse.cs` - Warehouse model for client
- `Location.cs` - Location model for client
- `InventoryTransaction.cs` - Inventory transaction model for client

#### DTOs/ - Data Transfer Objects
- `AuthDto.cs` - Authentication (LoginRequest, RegisterRequest, etc.)
- `ProductDto.cs` - Product DTO
- `CategoryDto.cs` - Category DTO
- `ApiResponse<T>.cs` - Common API response
- `PagedResponse<T>.cs` - Paginated response

#### Interfaces/ - Service Interfaces
- `IAuthService.cs` - Authentication service
- `IProductService.cs` - Product service
- `ICategoryService.cs` - Category service

#### Services/ - API Services
- `BaseApiService.cs` - Base API service
- `AuthApiService.cs` - Authentication implementation
- `ProductApiService.cs` - Product implementation

#### Constants/ - Constants
- `ApiEndpoints.cs` - API endpoints
- `TransactionTypes.cs` - Transaction types
- `StorageKeys.cs` - Storage keys

### UI Project - Razor Class Library

#### Components/ - Reusable Razor Components (located in Inventory.UI)
- `Forms/LoginForm.razor` - Login form
- `Forms/RegisterForm.razor` - Registration form
- `Forms/ProductForm.razor` - Product form
- `Layout/MainLayout.razor` - Main layout
- `Layout/NavigationMenu.razor` - Navigation menu
- `Notifications/ToastNotification.razor` - Toast notifications
- `ProductCard.razor` - Product card
- `ProductList.razor` - Product list

### Web Client - Blazor WebAssembly

#### Pages/ - Application Pages (located in Inventory.Web.Client)
- `Home.razor` - Home page
- `Login.razor` - Login page
- `Products.razor` - Products page
- `Register.razor` - Registration page

#### Services/ - Client Services
- `PortConfigurationService.cs` - Port configuration service

### Architecture Benefits

#### 1. Code Reuse
- All common models, DTOs, and services are in Shared project
- UI components are reusable across different client applications
- Client applications use the same interfaces and models
- Simplified synchronization between clients

#### 2. Mobile App Preparation
- Shared project ready for .NET MAUI use
- UI components can be reused in Blazor Hybrid (MAUI)
- API services can be used in both Blazor and mobile app
- Common constants and settings

#### 3. Type Safety
- Strongly-typed API clients
- Common data models
- Compiler checks type compatibility

#### 4. Scalability
- Easy to add new client applications
- Centralized API contract management
- Reusable UI components
- Uniform error handling

#### 5. Separation of Concerns
- UI components separated from client logic
- Clear boundaries between presentation and business logic
- Easy to maintain and test individual components

### Database Structure

**Note**: The database uses Entity Framework models from `Inventory.API.Models` namespace. The structure below reflects the actual database schema.

#### Product — Products (Inventory.API.Models.Product)
- Id (PK)
- Name
- SKU
- Description
- Quantity
- Unit
- IsActive (Admin only)
- CategoryId (FK)
- ManufacturerId (FK)
- ProductModelId (FK)
- ProductGroupId (FK)
- MinStock
- MaxStock
- Note
- CreatedAt
- UpdatedAt

#### Manufacturer — Manufacturers
- Id (PK)
- Name

#### ProductModel — Product Models
- Id (PK)
- Name
- ManufacturerId (FK)

#### ProductGroup — Product Groups
- Id (PK)
- Name

#### Category — Categories and Subcategories
- Id (PK)
- Name
- Description
- ParentCategoryId (FK, nullable)
- IsActive (Admin only)

#### Warehouse — Warehouses
- Id (PK)
- Name
- Location
- IsActive (Admin only)

#### InventoryTransaction — All Product Operations (Income/Outcome/Install)
- Id (PK)
- ProductId (FK)
- WarehouseId (FK)
- Type (Income/Outcome/Install)
- Quantity
- Date
- UserId (FK)
- LocationId (FK, nullable) — for installation operations
- Description

#### User — Users (Inventory.API.Models.User)
- Id (PK) - string (GUID)
- UserName - string
- Email - string
- EmailConfirmed - bool
- PasswordHash - string (managed by Identity)
- Role - string (custom property)
- NormalizedUserName - string (managed by Identity)
- NormalizedEmail - string (managed by Identity)
- Transactions - ICollection<InventoryTransaction>
- ProductHistories - ICollection<ProductHistory>

#### Location — Installation Location (Hierarchy)
- Id (PK)
- Name
- Description
- ParentLocationId (FK, nullable)
- IsActive

#### ProductHistory (Optional)
- Id (PK)
- ProductId (FK)
- Date
- OldQuantity
- NewQuantity
- UserId (FK)
- Description

Archive/hide operations available only to Admin.

### API Endpoints

#### Authentication
- `POST /api/auth/login` - Login
- `POST /api/auth/register` - Registration
- `POST /api/auth/refresh` - Token refresh
- `POST /api/auth/logout` - Logout

#### Products
- `GET /api/products` - Product list
- `GET /api/products/{id}` - Product by ID
- `GET /api/products/sku/{sku}` - Product by SKU
- `POST /api/products` - Create product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product
- `POST /api/products/{id}/stock/adjust` - Stock adjustment

#### Categories
- `GET /api/categories` - Category list
- `GET /api/categories/{id}` - Category by ID
- `GET /api/categories/root` - Root categories
- `GET /api/categories/{parentId}/sub` - Subcategories

---

## Design System

### Overview
The project implements a comprehensive design system that provides consistent styling, theming, and component patterns across the entire application. The design system is built on CSS custom properties (variables) and follows modern design principles.

### Design System Files
- **Main File**: `src/Inventory.Shared/wwwroot/css/design-system.css`
- **Components**: `src/Inventory.Shared/wwwroot/css/components/`
- **Themes**: `src/Inventory.Shared/wwwroot/css/themes/`
- **Import Order**: Bootstrap → Design System → App Styles → Component Styles

### Unified CSS Architecture
The project uses a **unified CSS architecture** where all styling is centralized in the Shared project:
- **Single Source of Truth**: All CSS files are in `Inventory.Shared/wwwroot/css/`
- **No Duplication**: Web and future clients reference the same files
- **Modular Structure**: Components are separated into individual CSS files
- **Theme Support**: Light/dark themes with easy customization
- **Future-Ready**: Ready for Mobile (MAUI) and Desktop (Electron/WPF) clients

### Design Tokens

#### Color Palette
```css
/* Primary Colors */
--color-primary: #1b6ec2;
--color-primary-dark: #1861ac;
--color-primary-light: #258cfb;

/* Semantic Colors */
--color-success: #10b981;
--color-error: #ef4444;
--color-warning: #f59e0b;
--color-info: #3b82f6;

/* Neutral Colors */
--color-gray-50 to --color-gray-900: Full grayscale palette
```

#### Typography
```css
/* Font Family */
--font-family-primary: 'Helvetica Neue', Helvetica, Arial, sans-serif;

/* Font Sizes */
--font-size-xs: 0.75rem (12px)
--font-size-sm: 0.875rem (14px)
--font-size-base: 1rem (16px)
--font-size-lg: 1.125rem (18px)
--font-size-xl: 1.25rem (20px)
--font-size-2xl: 1.5rem (24px)
--font-size-3xl: 1.875rem (30px)
--font-size-4xl: 2.25rem (36px)

/* Font Weights */
--font-weight-normal: 400
--font-weight-medium: 500
--font-weight-semibold: 600
--font-weight-bold: 700
```

#### Spacing Scale
```css
--spacing-1: 0.25rem (4px)
--spacing-2: 0.5rem (8px)
--spacing-3: 0.75rem (12px)
--spacing-4: 1rem (16px)
--spacing-5: 1.25rem (20px)
--spacing-6: 1.5rem (24px)
--spacing-8: 2rem (32px)
--spacing-10: 2.5rem (40px)
--spacing-12: 3rem (48px)
--spacing-16: 4rem (64px)
--spacing-20: 5rem (80px)
```

#### Layout Variables
```css
--sidebar-width: 250px
--topbar-height: 3.5rem
--content-padding: var(--spacing-6)

/* Z-Index Scale */
--z-dropdown: 1000
--z-sticky: 1020
--z-fixed: 1030
--z-modal: 1050
--z-notification: 9999
```

#### Transitions & Animations
```css
--transition-fast: 0.15s ease-in-out
--transition-normal: 0.2s ease-in-out
--transition-slow: 0.3s ease-in-out
```

### Component Styles

#### Buttons
The design system provides comprehensive button styles:
- `.btn` - Base button class
- `.btn-primary` - Primary action button
- `.btn-secondary` - Secondary action button
- `.btn-outline` - Outline style button
- `.btn-sm` - Small button variant
- `.btn-lg` - Large button variant

#### Cards
Card components with consistent styling:
- `.card` - Base card container
- `.card-header` - Card header section
- `.card-body` - Main card content
- `.card-footer` - Card footer section

#### Forms
Form styling with validation states:
- `.form-control` - Input field styling
- `.form-label` - Label styling
- `.validation-message` - Error message styling
- `.valid.modified` - Valid field styling
- `.invalid` - Invalid field styling

### Utility Classes

#### Layout Utilities
```css
.flex, .flex-col, .flex-row
.items-center, .items-start, .items-end
.justify-center, .justify-between, .justify-end
```

#### Spacing Utilities
```css
.p-1 to .p-8 (padding)
.px-2, .px-4, .px-6 (horizontal padding)
.py-2, .py-4, .py-6 (vertical padding)
.m-1 to .m-8 (margin)
.mb-2, .mb-4, .mb-6 (bottom margin)
.mt-2, .mt-4, .mt-6 (top margin)
```

#### Typography Utilities
```css
.text-xs to .text-xl (font sizes)
.text-center, .text-left, .text-right (alignment)
.font-normal to .font-bold (font weights)
.text-primary, .text-secondary, .text-muted (colors)
```

#### Display & Position Utilities
```css
.block, .inline-block, .inline, .hidden
.relative, .absolute, .fixed, .sticky
.w-full, .h-full, .w-auto, .h-auto
```

### Dark Theme Support

The design system includes automatic dark theme support through CSS media queries:

```css
@media (prefers-color-scheme: dark) {
  :root {
    --color-text-primary: #f9fafb;
    --color-text-secondary: #d1d5db;
    --color-bg-primary: #1f2937;
    --color-bg-secondary: #111827;
    /* ... more dark theme variables */
  }
}
```

### Responsive Design

#### Mobile-First Approach
- Breakpoints defined as CSS variables
- Mobile-specific spacing adjustments
- Responsive button and card layouts

#### Breakpoints
```css
--breakpoint-sm: 640px
--breakpoint-md: 768px
--breakpoint-lg: 1024px
--breakpoint-xl: 1280px
--breakpoint-2xl: 1536px
```

### Animation System

#### Built-in Animations
- `.fade-in` / `.fade-out` - Opacity transitions
- `.slide-in-right` / `.slide-out-right` - Slide animations
- `.spin` - Loading spinner animation

#### Keyframe Animations
- `@keyframes fadeIn/fadeOut` - Fade transitions
- `@keyframes slideInRight/slideOutRight` - Slide transitions
- `@keyframes spin` - Rotation animation

### Usage Guidelines

#### 1. Always Use Design Tokens
```css
/* ✅ Good */
.my-component {
  color: var(--color-primary);
  padding: var(--spacing-4);
  border-radius: var(--radius-md);
}

/* ❌ Avoid */
.my-component {
  color: #1b6ec2;
  padding: 16px;
  border-radius: 6px;
}
```

#### 2. Leverage Utility Classes
```html
<!-- ✅ Good -->
<div class="flex items-center justify-between p-4">
  <h2 class="text-xl font-semibold">Title</h2>
  <button class="btn btn-primary">Action</button>
</div>

<!-- ❌ Avoid -->
<div style="display: flex; align-items: center; justify-content: space-between; padding: 1rem;">
  <h2 style="font-size: 1.25rem; font-weight: 600;">Title</h2>
  <button style="background: #1b6ec2; color: white; padding: 0.5rem 1rem;">Action</button>
</div>
```

#### 3. Component-Specific Styling
When creating new components, follow this pattern:
1. Use design tokens for colors, spacing, typography
2. Leverage utility classes for layout
3. Add component-specific styles only when necessary
4. Use CSS custom properties for component theming

#### 4. Responsive Design
```css
/* Mobile-first approach */
.component {
  padding: var(--spacing-2);
}

@media (min-width: 641px) {
  .component {
    padding: var(--spacing-4);
  }
}
```

### File Organization

```
src/
├── Inventory.Shared/wwwroot/css/          # Unified CSS source
│   ├── design-system.css                 # Main design system file
│   ├── app.css                           # Application base styles
│   ├── components/                       # Modular component styles
│   │   ├── buttons.css                   # Button components
│   │   ├── cards.css                     # Card components
│   │   ├── forms.css                     # Form components
│   │   └── notifications.css             # Notification components
│   ├── themes/                           # Theme variations
│   │   ├── light.css                     # Light theme
│   │   └── dark.css                      # Dark theme
│   └── README.md                         # CSS architecture documentation
├── Inventory.UI/                          # UI components
│   └── Component-specific CSS files      # Scoped component styles
│       ├── Layout/MainLayout.razor.css
│       ├── Layout/NavMenu.razor.css
│       ├── Components/Notifications/
│       └── Pages/Home.razor.css
├── Inventory.Web.Client/                  # Web client
│   └── wwwroot/                          # Client-specific assets
├── Inventory.Mobile/                     # Future MAUI client
└── Inventory.Desktop/                    # Future Electron/WPF client
```

### Multi-Client Support

#### Web Client (Current)
```html
<!-- References shared CSS files -->
<link rel="stylesheet" href="_content/Inventory.Shared/css/design-system.css" />
<link rel="stylesheet" href="_content/Inventory.Shared/css/app.css" />
```

#### Mobile Client (Future MAUI)
```xml
<!-- Will reference the same shared CSS files -->
@import url('_content/Inventory.Shared/css/design-system.css');
@import url('_content/Inventory.Shared/css/app.css');
```

#### Desktop Client (Future Electron/WPF)
```html
<!-- Will reference the same shared CSS files -->
<link rel="stylesheet" href="css/design-system.css" />
<link rel="stylesheet" href="css/app.css" />
```

### Maintenance

#### Adding New Design Tokens
1. Add the token to `design-system.css` in Shared project
2. Document the token in this section
3. Use the token consistently across components
4. Update theme files if needed

#### Updating Colors/Themes
1. Modify CSS custom properties in `:root` or theme files
2. Test both light and dark themes
3. Update component styles if needed
4. Verify accessibility compliance

#### Adding New Components
1. Create new file in `components/` directory
2. Use design tokens consistently
3. Add to main `design-system.css` imports
4. Document in CSS README

#### Multi-Client Considerations
1. Test changes across all current clients
2. Ensure compatibility with future mobile/desktop clients
3. Consider platform-specific styling needs
4. Maintain backward compatibility

#### Performance Considerations
- CSS custom properties are efficient and cached by browsers
- Utility classes reduce CSS bundle size
- Component-scoped styles prevent style conflicts
- Design tokens enable runtime theming without rebuilds

----

## Notification System

### Overview
Implemented comprehensive UX improvement system for errors, including:

1. **Toast notifications** - popup notifications for users
2. **Retry logic** - automatic retry of operations
3. **Debug logs** - special page for superusers
4. **Enhanced error handling** - detailed error processing and logging

### Components

#### 1. Toast Notifications

##### NotificationService
```csharp
// Show success notification
NotificationService.ShowSuccess("Success", "Operation completed successfully");

// Show error with retry option
NotificationService.ShowError("Error", "Operation failed", () => RetryOperation(), "Retry");

// Show warning
NotificationService.ShowWarning("Warning", "Please check your input");

// Show info message
NotificationService.ShowInfo("Info", "New feature available");

// Show debug message (development only)
NotificationService.ShowDebug("Debug", "Debug information");
```

##### ToastNotification.razor
- Automatic show/hide animations
- Support for different notification types
- Retry button for errors
- Progress bar for automatic hiding
- Responsive design for mobile devices

#### 2. Retry Logic

##### RetryService
```csharp
// Execute operation with retry logic
var result = await RetryService.ExecuteWithRetryAsync(
    async () => await SomeOperation(),
    "OperationName",
    maxRetries: 3
);

// Configure retry policy
var policy = new RetryPolicy
{
    MaxRetries = 5,
    BaseDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromSeconds(10),
    BackoffMultiplier = 2
};

var result = await RetryService.ExecuteWithRetryAsync(
    async () => await SomeOperation(),
    "OperationName",
    policy
);
```

#### 3. Debug Logs

##### DebugLogsService
- Real-time log retrieval
- Filtering by level, time, search query
- Automatic updates (live streaming)
- Detailed error information

##### DebugLogs.razor
- Special `/debug` page for superusers
- Log level, count, search filters
- Automatic scrolling
- Modal windows with detailed information

#### 4. Enhanced Error Handling

##### ErrorHandlingService
```csharp
// Handle errors with notifications
await ErrorHandlingService.HandleErrorAsync(exception, "Operation context");

// Execute with retry logic
var result = await ErrorHandlingService.TryExecuteWithRetryAsync(
    async () => await SomeOperation(),
    "OperationName",
    maxRetries: 3
);

// Handle API errors
await ErrorHandlingService.HandleApiErrorAsync(response, "API Operation");
```

##### GlobalExceptionMiddleware
- Detailed error logging with context
- User-friendly error messages
- Integration with debug logs
- Client IP detection

### Usage

#### In Blazor Components
```razor
@inject INotificationService NotificationService
@inject IRetryService RetryService
@inject IErrorHandlingService ErrorHandlingService

@code {
    private async Task LoadData()
    {
        try
        {
            var data = await RetryService.ExecuteWithRetryAsync(
                async () => await DataService.GetDataAsync(),
                "LoadData"
            );
            
            NotificationService.ShowSuccess("Data Loaded", $"Loaded {data.Count} items");
        }
        catch (Exception ex)
        {
            await ErrorHandlingService.HandleErrorAsync(ex, "Loading data");
        }
    }
}
```

#### In API Services
```csharp
public class MyApiService
{
    private readonly IRetryService _retryService;
    private readonly INotificationService _notificationService;

    public async Task<List<Data>> GetDataAsync()
    {
        return await _retryService.ExecuteWithRetryAsync(
            async () =>
            {
                var response = await _httpClient.GetAsync("/api/data");
                return await response.Content.ReadFromJsonAsync<List<Data>>();
            },
            "GetData"
        );
    }
}
```

#### Setup in Program.cs
```csharp
// API project
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IDebugLogsService, DebugLogsService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();

// Web project
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<IDebugLogsService, DebugLogsService>();
```

### Key Features

#### Toast Notifications
- **Asynchronous**: Don't block UI
- **Automatic**: Disappear after set time
- **Interactive**: Retry buttons for errors
- **Responsive**: Adapt to screen size

#### Retry Logic
- **Exponential delay**: Increases with each attempt
- **Configurable**: Number of attempts, delays, policies
- **Notifications**: Shows retry operation progress
- **Logging**: Records all attempts in logs

#### Debug Logs
- **Real-time**: Update in real time
- **Filtering**: By level, time, search query
- **Security**: Only for superusers
- **Performance**: Limit logs in memory

#### Error Handling
- **User messages**: Understandable error messages
- **Context information**: Details about operation and environment
- **Automatic logging**: All errors recorded in logs
- **Integration**: With notifications and retry logic

### Scalability
- **Services**: Easy to add new notification types
- **Components**: Reusable components
- **Configuration**: Configurable policies and parameters
- **Monitoring**: Centralized logging and debugging

---

## Port Configuration

### Overview
All port configurations are centralized in the `ports.json` file at the project root. This eliminates port conflicts and makes it easy to change ports across all components.

### Configuration File Structure

#### ports.json
```json
{
  "api": {
    "http": 5000,
    "https": 7000,
    "urls": "https://localhost:7000;http://localhost:5000"
  },
  "web": {
    "http": 5001,
    "https": 7001,
    "urls": "https://localhost:7001;http://localhost:5001"
  },
  "database": {
    "port": 5432
  },
  "cors": {
    "allowedOrigins": [
      "http://localhost:5000",
      "https://localhost:7000",
      "http://localhost:5001",
      "https://localhost:7001",
      "http://10.0.2.2:8080",
      "capacitor://localhost",
      "https://yourmobileapp.com"
    ]
  },
  "launchUrls": {
    "api": "https://localhost:7000",
    "web": "https://localhost:7001"
  }
}
```

### Port Assignment
- **API Server**: 
  - HTTP: 5000
  - HTTPS: 7000
- **Web Client**: 
  - HTTP: 5001
  - HTTPS: 7001
- **Database**: 
  - Port: 5432 (PostgreSQL)

### Usage

#### Changing Ports
To change ports, simply edit the `ports.json` file and update the desired values. All scripts and configurations will automatically use the new ports.

#### Scripts
Single unified launcher script with multiple modes:
- `start-apps.ps1` - Unified launcher with full and quick modes
  - `.\start-apps.ps1` - Full launch with port checks
  - `.\start-apps.ps1 -Quick` - Quick launch without checks
  - `.\start-apps.ps1 -Help` - Show usage help

#### Configuration Files
The following files are automatically updated to use the centralized configuration:
- `src/Inventory.API/Properties/launchSettings.json`
- `src/Inventory.Web/Properties/launchSettings.json`
- `src/Inventory.API/appsettings.json` (CORS settings)

### Benefits
1. **Centralized Management**: All ports defined in one place
2. **No Conflicts**: API and Web use different port ranges
3. **Easy Updates**: Change ports in one file, affects all components
4. **Consistency**: All scripts and configurations stay in sync
5. **Documentation**: Clear port assignment and usage

### Clean Architecture Implementation

The port configuration system follows clean architecture principles:

#### Service Layer
- **`PortConfigurationService`** - Centralized service for reading and managing port configuration
- **Separation of concerns** - Port logic is isolated from application startup
- **Dependency injection** - Services are properly registered and injected

#### Extension Methods
- **`ServiceCollectionExtensions`** - Clean registration of services
- **`WebApplicationExtensions`** - Pipeline configuration methods
- **Fluent API** - Readable and maintainable configuration

#### File Structure
```
src/
├── Inventory.API/
│   ├── Services/
│   │   └── PortConfigurationService.cs    # API port configuration service
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # Extension methods
│   └── Program.cs                         # Clean startup code
└── Inventory.Web/
    ├── Services/
    │   └── PortConfigurationService.cs    # Web port configuration service
    └── Program.cs                         # Clean startup code
```

### Implementation Details

#### API Application
```csharp
// Add port configuration service
builder.Services.AddPortConfiguration();

// CORS with port configuration
builder.Services.AddCorsWithPorts();

// Configure CORS with port configuration (uses ports from ports.json)
app.ConfigureCorsWithPorts();
```

#### Web Application
```csharp
// Add port configuration service
builder.Services.AddScoped<PortConfigurationService>();

// Configure HTTP client to point to API server
var portService = new PortConfigurationService(...);
var apiUrl = portService.GetApiUrl();
```

### Troubleshooting

#### Ports Not Applied
1. Check if `ports.json` exists and is valid JSON
2. Run `.\scripts\Sync-Ports.ps1 -ToAppSettings` manually
3. Verify PowerShell execution policy allows script execution

#### Build Failures
1. Check if all required files exist
2. Verify port values are valid numbers
3. Ensure no port conflicts with other applications

#### Runtime Issues
1. Verify `appsettings.Ports.json` is in the correct location
2. Check if applications are reading from the correct configuration
3. Restart applications after port changes

### Best Practices
1. **Always use `ports.json`** as the single source of truth
2. **Run sync script** after changing ports manually
3. **Test port changes** before committing to version control
4. **Use different ports** for different environments (dev/staging/prod)
5. **Document port assignments** for team members

---

## Launch Instructions

### Automatic Launch

#### Windows (PowerShell) - Recommended
```powershell
# Full launch with port checks
.\start-apps.ps1

# Quick launch without checks
.\start-apps.ps1 -Quick

# Show help and usage options
.\start-apps.ps1 -Help
```

### Manual Launch

#### 1. Start API Server
```bash
cd src/Inventory.API
dotnet run
```
**Ports:**
- HTTPS: https://localhost:7000
- HTTP: http://localhost:5000

#### 2. Start Web Client (in new terminal)
```bash
cd src/Inventory.Web.Client
dotnet run
```
**Ports:**
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5001

### Application Access

After launch, open browser and navigate to:
- **Web Application**: https://localhost:7001
- **API Documentation**: https://localhost:7000/swagger (if configured)

### Stopping Applications

- **Automatic Launch**: Press `Ctrl+C` in terminal
- **Manual Launch**: Press `Ctrl+C` in each terminal

### Requirements

- .NET 9.0 SDK
- PostgreSQL (for database)
- Browser with HTTPS support

### Troubleshooting

1. **Port Busy**: Ensure ports 5000, 5001, 7000, 7001 are free
2. **CORS Errors**: Check CORS settings in `appsettings.json`
3. **Database**: Ensure PostgreSQL is running and accessible

---

## Testing Guide

### Test Data

#### Valid User (Created Automatically on Launch)
- **Username**: `superadmin`
- **Password**: `SuperAdmin123!`
- **Email**: `admin@inventory.com`
- **Roles**: `SuperUser`, `Admin`
- **Note**: User is created automatically by `DbInitializer` on first launch

#### Invalid Data for Error Testing
- **Username**: `testuser`
- **Password**: `wrongpassword`

### Test Scenarios

#### 1. Successful Login with Valid Data ✅
**Steps:**
1. Open https://localhost:5001/login
2. Enter `superadmin` in Username field
3. Enter `SuperAdmin123!` in Password field
4. Click "Sign In" button or press Enter

**Expected Result:**
- Shows "Login successful! Redirecting..." message
- Button shows "Redirecting..." with spinner
- After 1.5 seconds, redirects to main page (/)
- Token saved in LocalStorage

#### 2. Failed Login with Invalid Data 🔄
**Steps:**
1. Open https://localhost:5001/login
2. Enter `testuser` in Username field
3. Enter `wrongpassword` in Password field
4. Click "Sign In" button

**Expected Result:**
- Shows error message "Login failed. Please check your credentials."
- Message has red color and warning icon
- Button returns to normal state

#### 3. Real-time Validation 🔄
**Steps:**
1. Open https://localhost:5001/login
2. Start typing in Username field:
   - When entering 1-2 characters: field highlights red
   - When entering 3+ characters: field highlights green
3. Start typing in Password field:
   - When entering 1-5 characters: field highlights red
   - When entering 6+ characters: field highlights green

**Expected Result:**
- Fields change color in real time
- Validation triggers on input, not just on submit

#### 4. Redirect After Successful Login 🔄
**Steps:**
1. Perform successful login (Test 1)
2. Observe redirect process

**Expected Result:**
- Shows "Login successful! Redirecting..." message
- Button shows "Redirecting..." with spinner
- After 1.5 seconds, redirects to main page
- URL changes to https://localhost:5001/

#### 5. Close Message Button 🔄
**Steps:**
1. Perform failed login (Test 2)
2. Click "X" button in error message

**Expected Result:**
- Error message disappears
- Form returns to original state

#### 6. Login on Enter Press 🔄
**Steps:**
1. Open https://localhost:5001/login
2. Enter valid data
3. Press Enter in any field

**Expected Result:**
- Login attempt occurs (same as clicking button)
- Works same as clicking "Sign In" button

#### 7. Token Storage in LocalStorage 🔄
**Steps:**
1. Perform successful login
2. Open Developer Tools (F12)
3. Go to Application/Storage → Local Storage
4. Check for "authToken" key

**Expected Result:**
- "authToken" key exists in LocalStorage with JWT token
- Token contains user information and roles

### Additional Tests

#### 8. Empty Fields Test
**Steps:**
1. Leave fields empty
2. Click "Sign In"

**Expected Result:**
- Shows validation messages
- Button remains active but form not submitted

#### 9. Network Error Test
**Steps:**
1. Stop API server
2. Try to login with valid data

**Expected Result:**
- Shows network error message
- Button returns to normal state

### Testing Status

- ✅ Test 1: Successful Login - PASSED
- 🔄 Test 2: Failed Login - IN PROGRESS
- ⏳ Test 3: Real-time Validation - PENDING
- ⏳ Test 4: Redirect - PENDING
- ⏳ Test 5: Close Messages - PENDING
- ⏳ Test 6: Login on Enter - PENDING
- ⏳ Test 7: Token Storage - PENDING

---

## Development Guidelines

### Project Structure
- **Models/** — common data models
- **DTOs/** — Data Transfer Objects for API
- **Interfaces/** — service interfaces
- **Services/** — API service implementations
- **Constants/** — constants and settings
- **Components/** — common Razor components

### Developer Instructions
See `.ai-agent-prompts` file for permanent preferences and development instructions.

### Requirements
- **.NET 9.0 SDK** — for development and building
- **PostgreSQL** — database (configured in `appsettings.json`)
- **Modern Browser** — with WebAssembly and HTTPS support

### Troubleshooting
1. **Port Busy**: Ensure ports 5000, 5001, 7000, 7001 are free
2. **CORS Errors**: Check CORS settings in `src/Inventory.API/appsettings.json`
3. **Database**: Ensure PostgreSQL is running and accessible
4. **HTTPS Issues**: If needed, disable HTTPS in `Properties/launchSettings.json`

### Future Improvements

#### Mobile Application (.NET MAUI)
- Use Shared project
- Blazor Hybrid for UI (reuse Razor components)
- Native UI components for platform-specific features
- Offline synchronization
- Unified code for all platforms

#### Additional Clients
- Desktop application (.NET MAUI)
- Console application for administration
- Web API for integrations

#### Extensions
- SignalR for real-time updates
- Caching with Redis
- Message Queues for asynchronous processing
- Microservices architecture
