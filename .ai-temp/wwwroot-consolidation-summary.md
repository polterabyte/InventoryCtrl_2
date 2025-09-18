# wwwroot Consolidation Summary

## Problem
The `wwwroot` folders were duplicated across three projects:
- `Inventory.Shared/wwwroot/` - Bootstrap, favicon, sample-data
- `Inventory.UI/wwwroot/` - CSS, JavaScript, index.html
- `Inventory.Web.Client/wwwroot/` - Bootstrap (duplicate), CSS (duplicate), favicon (duplicate), index.html

This caused:
- **File duplication** and maintenance issues
- **Inconsistent versions** of Bootstrap and other libraries
- **Wasted disk space** with duplicate resources
- **Complexity** for future MAUI project integration

## Solution
Centralized all static resources in `Inventory.Shared/wwwroot/` since it's the common library that will be used by all projects including the future MAUI project.

## Changes Made

### 1. Moved JavaScript Files
**From**: `src/Inventory.UI/wwwroot/js/`
**To**: `src/Inventory.Shared/wwwroot/js/`
- `api-config.js`
- `audit-logs.js`
- `signalr-notifications.js`
- `test-signalr.js`
- `signalr.min.js`

### 2. Moved CSS Files
**From**: `src/Inventory.UI/wwwroot/css/`
**To**: `src/Inventory.Shared/wwwroot/css/`
- `app.css`
- `design-system.css`
- `components/` folder with all component styles
- `themes/` folder with dark/light themes

### 3. Updated HTML References

#### Inventory.UI/index.html
**Before**:
```html
<link rel="stylesheet" href="css/design-system.css" />
<link rel="stylesheet" href="css/app.css" />
<script src="js/api-config.js"></script>
<script src="js/signalr-notifications.js"></script>
```

**After**:
```html
<link rel="stylesheet" href="_content/Inventory.Shared/css/design-system.css" />
<link rel="stylesheet" href="_content/Inventory.Shared/css/app.css" />
<script src="_content/Inventory.Shared/js/api-config.js"></script>
<script src="_content/Inventory.Shared/js/signalr-notifications.js"></script>
```

#### Inventory.Web.Client/index.html
**Before**:
```html
<link rel="stylesheet" href="lib/bootstrap/dist/css/bootstrap.min.css" />
<link rel="stylesheet" href="_content/Inventory.UI/css/design-system.css" />
<link rel="stylesheet" href="css/app.css" />
<link rel="icon" type="image/png" href="favicon.png" />
<script src="_content/Inventory.UI/js/api-config.js"></script>
```

**After**:
```html
<link rel="stylesheet" href="_content/Inventory.Shared/lib/bootstrap/dist/css/bootstrap.min.css" />
<link rel="stylesheet" href="_content/Inventory.Shared/css/design-system.css" />
<link rel="stylesheet" href="_content/Inventory.Shared/css/app.css" />
<link rel="icon" type="image/png" href="_content/Inventory.Shared/favicon.png" />
<script src="_content/Inventory.Shared/js/api-config.js"></script>
```

### 4. Removed Duplicate Files
- **Deleted**: `src/Inventory.UI/wwwroot/js/` (moved to Shared)
- **Deleted**: `src/Inventory.UI/wwwroot/css/` (moved to Shared)
- **Deleted**: `src/Inventory.Web.Client/wwwroot/lib/` (Bootstrap duplicate)
- **Deleted**: `src/Inventory.Web.Client/wwwroot/css/` (CSS duplicate)
- **Deleted**: `src/Inventory.Web.Client/wwwroot/favicon.png` (duplicate)
- **Deleted**: `src/Inventory.Web.Client/wwwroot/icon-192.png` (duplicate)

## Final Structure

### Inventory.Shared/wwwroot/ (Centralized Resources)
```
wwwroot/
├── css/
│   ├── app.css
│   ├── design-system.css
│   ├── components/
│   │   ├── buttons.css
│   │   ├── cards.css
│   │   ├── forms.css
│   │   ├── layout.css
│   │   ├── navigation.css
│   │   ├── notifications.css
│   │   └── reference-data-widget.css
│   └── themes/
│       ├── dark.css
│       └── light.css
├── js/
│   ├── api-config.js
│   ├── audit-logs.js
│   ├── signalr-notifications.js
│   ├── test-signalr.js
│   └── signalr.min.js
├── lib/
│   └── bootstrap/ (complete Bootstrap library)
├── favicon.png
├── icon-192.png
└── sample-data/
```

### Inventory.UI/wwwroot/ (Minimal)
```
wwwroot/
├── index.html (references Shared resources)
└── sample-data/
```

### Inventory.Web.Client/wwwroot/ (Minimal)
```
wwwroot/
├── index.html (references Shared resources)
└── sample-data/
    └── weather.json
```

## Benefits

✅ **Single Source of Truth**: All static resources in one location
✅ **No Duplication**: Eliminated all duplicate files
✅ **Consistent Versions**: All projects use the same Bootstrap and other libraries
✅ **MAUI Ready**: Shared resources will be available for future MAUI project
✅ **Easier Maintenance**: Update files in one place only
✅ **Reduced Disk Usage**: Eliminated duplicate files
✅ **Better Organization**: Clear separation of concerns

## Future MAUI Integration

When adding MAUI project:
1. **Reference Inventory.Shared** - All resources will be automatically available
2. **No additional setup** needed for static resources
3. **Consistent UI** across all platforms (Web, Desktop, Mobile)
4. **Single maintenance point** for all UI resources

## Testing

To test the consolidation:

1. **Start Applications**:
   ```powershell
   .\start-apps.ps1
   ```

2. **Test Inventory.UI**: http://localhost:5001
3. **Test Inventory.Web.Client**: https://localhost:7001

4. **Check Browser Console**:
   - No 404 errors for CSS/JS files
   - All resources load from `_content/Inventory.Shared/`
   - SignalR functionality works
   - UI styling is consistent

## Expected Results

✅ All applications load resources from Inventory.Shared
✅ No 404 errors for static resources
✅ Consistent UI across all applications
✅ SignalR works in both applications
✅ Single place to maintain all static resources
✅ Ready for future MAUI project integration

---

**Status**: ✅ **COMPLETED** - wwwroot folders successfully consolidated
