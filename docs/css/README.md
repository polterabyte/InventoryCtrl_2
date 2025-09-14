# CSS Architecture Documentation

## Overview

This directory contains the unified CSS architecture for the Inventory Control System. All styles are centralized in the Shared project and referenced by all client applications (Web, Mobile, Desktop).

## File Structure

```
css/
├── design-system.css          # Main design system file with tokens and imports
├── app.css                    # Application base styles and overrides
├── components/                # Logical component styles
│   ├── buttons.css           # Button components and variants
│   ├── cards.css             # Card components and layouts
│   ├── forms.css             # Form controls and validation
│   ├── layout.css            # Layout components (containers, grid)
│   ├── navigation.css        # Navigation components (navbar, breadcrumbs)
│   └── notifications.css     # Toast notifications and alerts
├── themes/                   # Theme variations
│   ├── dark.css             # Dark theme overrides
│   └── light.css            # Light theme overrides
└── README.md                # This documentation
```

## Design System Architecture

### Design Tokens
All design tokens (colors, typography, spacing, etc.) are defined in `design-system.css` using CSS custom properties:

```css
:root {
  /* Colors */
  --color-primary: #1b6ec2;
  --color-success: #10b981;
  
  /* Typography */
  --font-size-base: 1rem;
  --font-weight-medium: 500;
  
  /* Spacing */
  --spacing-4: 1rem;
  
  /* Layout */
  --sidebar-width: 250px;
}
```

### Component Organization
Components are logically separated into individual CSS files:

- **buttons.css**: All button variants, sizes, and states
- **cards.css**: Card layouts, variants, and content styling
- **forms.css**: Form controls, validation, and layout
- **layout.css**: Grid system, containers, and page layout
- **navigation.css**: Navigation components and menus
- **notifications.css**: Toast notifications and alert components

### Import Strategy
All component files are imported into the main `design-system.css`:

```css
/* Import all component styles */
@import url('./components/buttons.css');
@import url('./components/cards.css');
@import url('./components/forms.css');
@import url('./components/notifications.css');
@import url('./components/layout.css');
@import url('./components/navigation.css');
```

## Usage Guidelines

### 1. Always Use Design Tokens
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

### 2. Leverage Component Classes
```html
<!-- ✅ Good -->
<button class="btn btn-primary btn-lg">Primary Action</button>
<div class="card card-elevated">
  <div class="card-header">
    <h3 class="card-title">Title</h3>
  </div>
  <div class="card-body">Content</div>
</div>

<!-- ❌ Avoid -->
<button style="background: #1b6ec2; padding: 12px 24px;">Button</button>
```

### 3. Component-Specific Styling
When creating new components:
1. Use design tokens for colors, spacing, typography
2. Leverage existing utility classes for layout
3. Add component-specific styles only when necessary
4. Consider if the style should be in a component file

### 4. Responsive Design
Use mobile-first approach with design token breakpoints:

```css
.component {
  padding: var(--spacing-2);
}

@media (min-width: 641px) {
  .component {
    padding: var(--spacing-4);
  }
}
```

## Multi-Client Support

### Web Client (Current)
```html
<link rel="stylesheet" href="_content/Inventory.Shared/css/design-system.css" />
<link rel="stylesheet" href="_content/Inventory.Shared/css/app.css" />
```

### Mobile Client (Future MAUI)
```xml
@import url('_content/Inventory.Shared/css/design-system.css');
@import url('_content/Inventory.Shared/css/app.css');
```

### Desktop Client (Future Electron/WPF)
```html
<link rel="stylesheet" href="css/design-system.css" />
<link rel="stylesheet" href="css/app.css" />
```

## Maintenance

### Adding New Design Tokens
1. Add the token to `design-system.css` in the `:root` selector
2. Document the token in this README
3. Use the token consistently across components
4. Update theme files if needed

### Adding New Components
1. Create new file in `components/` directory
2. Use design tokens consistently
3. Add import to `design-system.css`
4. Document the component in this README

### Updating Themes
1. Modify CSS custom properties in theme files
2. Test both light and dark themes
3. Update component styles if needed
4. Verify accessibility compliance

## Performance Considerations

- CSS custom properties are efficient and cached by browsers
- Component separation enables better caching strategies
- Utility classes reduce CSS bundle size
- Design tokens enable runtime theming without rebuilds

## Best Practices

1. **Single Source of Truth**: All styles in Shared project
2. **Consistent Naming**: Use BEM-like naming conventions
3. **Documentation**: Document all new tokens and components
4. **Testing**: Test changes across all current and future clients
5. **Accessibility**: Ensure WCAG compliance for all styles
6. **Performance**: Optimize for bundle size and runtime performance
