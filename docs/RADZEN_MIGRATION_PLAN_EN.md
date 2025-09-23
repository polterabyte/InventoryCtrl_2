# Radzen Migration Plan

## Overview
- Status: In progress
- Priority: High
- Target duration: 10–15 days

## Goals
- Replace Bootstrap with Radzen components
- Remove custom CSS and inline styles
- Use Radzen themes and utilities only
- Standardize UI patterns across the app

## Success Metrics
- 0 Bootstrap classes in .razor files
- 0 custom CSS files (beyond Radzen basics if needed)
- 100% forms/grids/dialogs on Radzen components
- Single consistent Radzen theme

## Scope (high level)
- Layout/Nav: `MainLayout.razor`, `NavMenu`
- Forms: `Login.razor`, `Register.razor`, `*ManagementWidget.razor`
- Tables: Admin pages (Users, Units, Warehouses, ReferenceData)
- Dialogs: `ReferenceDataWidget.razor` and others
- Navigation elements: tabs/accordion
- Cards/metrics blocks

---

## Phases and Subtasks

### Phase 1: Setup and Foundation (1–2 days)
1.1 Radzen configuration
- Verify theme and script in `App.razor` (`RadzenTheme` + Radzen.Blazor.js)
- Ensure `<RadzenComponents />` in `MainLayout.razor`
- Confirm `@rendermode` is `InteractiveAuto`

1.2 Utilities
- Add `RadzenUi` (spacing gaps, mapping helpers)
- Add `RadzenNotificationBridge` to forward IUINotificationService to Radzen NotificationService

1.3 Documentation
- Record plan and checklists in docs

Exit criteria (Phase 1)
- Theme and script OK, `<RadzenComponents />` present
- Utilities added, bridge works
- Docs updated

### Phase 2: UI Migration (4–7 days)
2.1 Layout and navigation
- Replace custom layout containers with `RadzenPanel`/`RadzenSidebar`/`RadzenStack`
- Migrate `NavMenu` to `RadzenMenu`

2.2 Forms and validation
- Convert all forms to `RadzenTemplateForm` + inputs + validators
- Start with `Login.razor`, `Register.razor`, CRUD forms in `*ManagementWidget.razor`

2.3 Grids and lists
- Convert all tables to `RadzenDataGrid` (paging/sort/filter)
- Prioritize Admin pages

2.4 Dialogs
- Convert Bootstrap modals to `RadzenDialog` + `DialogService`
- Start with `ReferenceDataWidget.razor`

2.5 Navigation elements and cards
- Tabs to `RadzenTabs`, accordions to `RadzenAccordion`
- Cards to `RadzenCard`, layout with `RadzenStack`

### Phase 3: Cleanup and Optimization (2–3 days)
3.1 Remove Bootstrap
- Delete Bootstrap assets and references
- Remove Bootstrap classes from .razor files

3.2 CSS cleanup
- Remove custom CSS files and inline styles
- Keep only minimal essentials if any

3.3 Polish
- Visual regression pass, accessibility, performance (virtualization)

### Phase 4: Finalization (1 day)
- Align spacing/style consistency
- Update docs and close checklists

---

## Component Mapping
- Buttons: `.btn*` → `RadzenButton`
- Forms: `.form-control/select` → `RadzenTextBox/DropDown/Numeric/...` + validators
- Tables: `.table` → `RadzenDataGrid`
- Cards: `.card*` → `RadzenCard`
- Modals: `.modal*` → `RadzenDialog`
- Tabs: `.nav nav-tabs` → `RadzenTabs`
- Alerts/Badges: `.alert*`/`.badge*` → `RadzenAlert`/`RadzenBadge`
- Layout grid: `container/row/col-*` → `RadzenStack`/`RadzenPanel`
