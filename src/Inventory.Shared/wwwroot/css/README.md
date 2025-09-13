# CSS Architecture for Inventory Control System

## Overview
This directory contains the unified CSS architecture for the Inventory Control System. All clients (Web, Mobile, Desktop) use the same styling system for consistency.

## File Structure
```
css/
├── design-system.css          # Main design system with tokens and utilities
├── app.css                    # Application-specific base styles
├── components/                # Modular component styles
│   ├── buttons.css           # Button components
│   ├── cards.css             # Card components
│   ├── forms.css             # Form components
│   └── notifications.css     # Notification components
├── themes/                   # Theme variations
│   ├── light.css            # Light theme overrides
│   └── dark.css             # Dark theme overrides
└── README.md                # This file
```

## Usage for Different Clients

### Web Client (Blazor WebAssembly)
```html
<!-- In index.html -->
<link rel="stylesheet" href="_content/Inventory.Shared/bootstrap/bootstrap.min.css" />
<link rel="stylesheet" href="_content/Inventory.Shared/css/design-system.css" />
<link rel="stylesheet" href="_content/Inventory.Shared/css/app.css" />
```

### Future Mobile Client (MAUI/Blazor Hybrid)
```xml
<!-- In MauiProgram.cs or equivalent -->
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://your-api-url/") });

<!-- In CSS imports -->
@import url('_content/Inventory.Shared/css/design-system.css');
@import url('_content/Inventory.Shared/css/app.css');
```

### Future Desktop Client (WPF/Electron)
```html
<!-- For Electron-based desktop app -->
<link rel="stylesheet" href="css/design-system.css" />
<link rel="stylesheet" href="css/app.css" />
```

## Design Tokens

### Colors
All colors are defined as CSS custom properties in `:root`:
- Primary colors: `--color-primary`, `--color-primary-dark`, etc.
- Semantic colors: `--color-success`, `--color-error`, `--color-warning`, `--color-info`
- Neutral colors: `--color-gray-50` to `--color-gray-900`
- Text colors: `--color-text-primary`, `--color-text-secondary`, `--color-text-muted`
- Background colors: `--color-bg-primary`, `--color-bg-secondary`, `--color-bg-tertiary`

### Typography
- Font family: `--font-family-primary`
- Font sizes: `--font-size-xs` to `--font-size-4xl`
- Font weights: `--font-weight-normal` to `--font-weight-bold`
- Line heights: `--line-height-tight`, `--line-height-normal`, `--line-height-relaxed`

### Spacing
- Spacing scale: `--spacing-1` to `--spacing-20`
- Layout variables: `--sidebar-width`, `--topbar-height`, `--content-padding`

### Layout
- Border radius: `--radius-sm` to `--radius-full`
- Shadows: `--shadow-sm` to `--shadow-xl`
- Transitions: `--transition-fast`, `--transition-normal`, `--transition-slow`
- Z-index scale: `--z-dropdown` to `--z-notification`

## Component System

### Buttons
```css
.btn, .btn-primary, .btn-secondary, .btn-outline
.btn-sm, .btn-lg
.btn-success, .btn-error, .btn-warning, .btn-info
```

### Cards
```css
.card, .card-header, .card-body, .card-footer
.card-elevated, .card-outlined, .card-flat
.card-sm, .card-lg
.card-title, .card-subtitle, .card-text, .card-actions
```

### Forms
```css
.form-control, .form-label, .form-group, .form-row, .form-col
.form-control-sm, .form-control-lg
.form-check, .form-select, .form-floating
.validation-message, .form-text
```

### Notifications
```css
.toast-notification, .toast-header, .toast-content, .toast-actions
.toast-success, .toast-error, .toast-warning, .toast-info, .toast-debug
.alert, .alert-success, .alert-error, .alert-warning, .alert-info
```

## Utility Classes

### Layout
```css
.flex, .flex-col, .flex-row
.items-center, .items-start, .items-end
.justify-center, .justify-between, .justify-end
```

### Spacing
```css
.p-1 to .p-8, .px-2, .px-4, .px-6, .py-2, .py-4, .py-6
.m-1 to .m-8, .mb-2, .mb-4, .mb-6, .mt-2, .mt-4, .mt-6
```

### Typography
```css
.text-xs to .text-xl
.text-center, .text-left, .text-right
.font-normal to .font-bold
.text-primary, .text-secondary, .text-muted
```

### Display & Position
```css
.block, .inline-block, .inline, .hidden
.relative, .absolute, .fixed, .sticky
.w-full, .h-full, .w-auto, .h-auto
```

## Theme System

### Automatic Theme Detection
The system automatically detects user's preferred color scheme:
```css
@media (prefers-color-scheme: dark) {
  :root {
    /* Dark theme variables */
  }
}
```

### Manual Theme Control
For explicit theme control, use data attributes:
```html
<html data-theme="dark">
<!-- or -->
<html data-theme="light">
```

### Custom Themes
Create new theme files in the `themes/` directory:
```css
/* themes/custom.css */
:root[data-theme="custom"] {
  --color-primary: #your-color;
  /* other overrides */
}
```

## Responsive Design

### Breakpoints
```css
--breakpoint-sm: 640px
--breakpoint-md: 768px
--breakpoint-lg: 1024px
--breakpoint-xl: 1280px
--breakpoint-2xl: 1536px
```

### Mobile-First Approach
All styles are written mobile-first with progressive enhancement:
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

## Animation System

### Built-in Animations
```css
.fade-in, .fade-out
.slide-in-right, .slide-out-right
.spin
```

### Custom Animations
Define custom animations using design tokens:
```css
@keyframes customAnimation {
  from {
    transform: translateY(var(--spacing-4));
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}
```

## Best Practices

### 1. Always Use Design Tokens
```css
/* ✅ Good */
.component {
  color: var(--color-primary);
  padding: var(--spacing-4);
  border-radius: var(--radius-md);
}

/* ❌ Avoid */
.component {
  color: #1b6ec2;
  padding: 16px;
  border-radius: 6px;
}
```

### 2. Leverage Utility Classes
```html
<!-- ✅ Good -->
<div class="flex items-center justify-between p-4">
  <h2 class="text-xl font-semibold">Title</h2>
  <button class="btn btn-primary">Action</button>
</div>
```

### 3. Component-Specific Styling
- Use design tokens for colors, spacing, typography
- Leverage utility classes for layout
- Add component-specific styles only when necessary
- Use CSS custom properties for component theming

### 4. Responsive Design
- Write mobile-first CSS
- Use design tokens in media queries
- Test on multiple screen sizes

## Adding New Components

1. Create a new file in `components/` directory
2. Use design tokens consistently
3. Add utility classes where appropriate
4. Include responsive behavior
5. Add to main `design-system.css` imports
6. Document in this README

## Performance Considerations

- CSS custom properties are efficient and cached by browsers
- Utility classes reduce CSS bundle size
- Component-scoped styles prevent style conflicts
- Design tokens enable runtime theming without rebuilds
- Modular imports allow tree-shaking unused styles

## Browser Support

- Modern browsers with CSS custom properties support
- IE11+ with polyfills for CSS custom properties
- Progressive enhancement for older browsers

## Maintenance

### Adding New Design Tokens
1. Add to `:root` in `design-system.css`
2. Document in this README
3. Use consistently across components
4. Consider impact on existing themes

### Updating Themes
1. Modify variables in theme files
2. Test both light and dark themes
3. Verify accessibility compliance
4. Update documentation

### Component Updates
1. Update component CSS files
2. Test across all clients
3. Verify responsive behavior
4. Update utility classes if needed
