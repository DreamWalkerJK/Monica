# MudBlazor v9 Component Reference

This document provides a quick reference for MudBlazor v9 components.

## Component Categories

### 1. Layout Components

- **MudContainer** - Responsive container
- **MudGrid/MudItem** - 12-column grid system
- **MudPaper** - Material Design paper effect
- **MudCard** - Card container
- **MudDrawer** - Side drawer
- **MudLayout** - Page layout framework
- **MudMainContent** - Main content area
- **MudSpacer** - Flexible space
- **MudDivider** - Divider line
- **MudBreakpointProvider** - Responsive breakpoints

### 2. Navigation Components

- **MudAppBar** - Application bar
- **MudBreadcrumbs** - Breadcrumb navigation
- **MudLink** - Link
- **MudMenu** - Dropdown menu
- **MudNavMenu/MudNavLink** - Navigation menu
- **MudPagination** - Pagination
- **MudTabs/MudTabPanel** - Tab panels
- **MudStepper/MudStep** - Stepper
- **MudSpeedDial** - Speed dial button

### 3. Input Components

- **MudTextField** - Text input
- **MudNumericField** - Numeric input
- **MudSelect** - Dropdown select
- **MudAutocomplete** - Autocomplete
- **MudCheckBox** - Checkbox
- **MudRadio/MudRadioGroup** - Radio buttons
- **MudSwitch** - Switch toggle
- **MudSlider** - Slider
- **MudRating** - Rating
- **MudToggleGroup/MudToggleItem** - Toggle group
- **MudColorPicker** - Color picker
- **MudDatePicker** - Date picker
- **MudTimePicker** - Time picker
- **MudDateRangePicker** - Date range picker
- **MudMask** - Input mask
- **MudFileUpload** - File upload

### 4. Data Display Components

- **MudTable** - Basic table
- **MudDataGrid** - Advanced data grid (supports drag-drop sorting)
- **MudTreeView/MudTreeViewItem** - Tree view
- **MudList/MudListItem** - List
- **MudChip/MudChipSet** - Chips
- **MudBadge** - Badge
- **MudAvatar** - Avatar
- **MudTooltip** - Tooltip
- **MudCarousel** - Carousel
- **MudTimeline** - Timeline
- **MudChart<T>** - Typed charts; see [measurement time series](#measurement-time-series)
- **MudChat/MudChatBubble** - Chat component (removed in v9)

### 5. Feedback Components

- **MudAlert** - Alert
- **MudSnackbar** - Snackbar notification
- **MudDialog** - Dialog
- **MudProgressCircular** - Circular progress
- **MudProgressLinear** - Linear progress
- **MudSkeleton** - Skeleton loading
- **MudOverlay** - Overlay
- **MudBackdrop** - Backdrop

### 6. Button Components

- **MudButton** - Standard button
- **MudIconButton** - Icon button
- **MudFab** - Floating action button
- **MudButtonGroup** - Button group
- **MudToggleIconButton** - Toggle icon button

### 7. Utility Components

- **MudIcon** - Icon
- **MudText** - Text typography
- **MudHidden** - Responsive hiding
- **MudFocusTrap** - Focus trap
- **MudVirtualize** - Virtual scrolling
- **MudSwipeArea** - Swipe area
- **MudScrollToTop** - Scroll to top
- **MudMessageBox** - Message box
- **MudContextualActionBar** - Contextual action bar

## Common Code Examples

### Form Example

```razor
<MudForm @ref="form" @bind-IsValid="@isValid">
    <MudTextField T="string"
                  Label="Username"
                  @bind-Value="username"
                  Required="true"
                  RequiredError="Username is required" />

    <MudTextField T="string"
                  Label="Password"
                  @bind-Value="password"
                  InputType="InputType.Password"
                  Required="true" />

    <MudButton ButtonType="ButtonType.Submit"
               Variant="Variant.Filled"
               Color="Color.Primary"
               Disabled="!isValid">
        Submit
    </MudButton>
</MudForm>
```

### Data Grid Example

```razor
<MudDataGrid T="Person"
             Items="@people"
             Filterable="true"
             SortMode="@SortMode.Multiple"
             DragDropColumnReordering="true">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.Age" Title="Age" />
        <PropertyColumn Property="x => x.Email" Title="Email" />
        <TemplateColumn Title="Actions">
            <CellTemplate>
                <MudIconButton Icon="@Icons.Material.Filled.Edit"
                               Size="Size.Small"
                               OnClick="@(() => EditPerson(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>
```

### Dialog Example

```csharp
@code {
    private async Task ShowDialogAsync()
    {
        var parameters = new DialogParameters<MyDialog>
        {
            { x => x.ContentText, "Are you sure you want to delete?" },
            { x => x.ButtonText, "Delete" },
            { x => x.Color, Color.Error }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small
        };

        var dialog = await DialogService.ShowAsync<MyDialog>("Confirm Delete", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            // Execute delete operation
        }
    }
}
```

### Toggle Group Example

```razor
<MudToggleGroup T="string" @bind-Value="selectedValue">
    <MudToggleItem Value="@("option1")">Option 1</MudToggleItem>
    <MudToggleItem Value="@("option2")">Option 2</MudToggleItem>
    <MudToggleItem Value="@("option3")">Option 3</MudToggleItem>
</MudToggleGroup>
```

### Stepper Example

```razor
<MudStepper @ref="stepper">
    <MudStep Title="Step 1">
        <ChildContent>Step 1 content</ChildContent>
    </MudStep>
    <MudStep Title="Step 2">
        <ChildContent>Step 2 content</ChildContent>
    </MudStep>
    <MudStep Title="Complete">
        <ChildContent>Complete content</ChildContent>
    </MudStep>
</MudStepper>
```

### DataGrid Drag-Drop Example

```razor
<MudDataGrid T="MyModel"
             Items="@items"
             DragDropColumnReordering="true"
             ColumnsPanelReordering="true"
             DragIndicatorIcon="@Icons.Material.Filled.DragIndicator"
             DropAllowedClass="drop-allowed"
             DropNotAllowedClass="drop-not-allowed">
    <!-- Column definitions -->
</MudDataGrid>
```

## Measurement time series

For MudBlazor v9.11.0, use `MudChart<double>` with `ChartType.Timeseries`, `ChartSeries<double>.Data` containing `(DateTime, double)[]`, and explicit `TimeSeriesChartOptions`. The native time-series chart places points using elapsed timestamp milliseconds; timestamp strings on a categorical Line chart instead represent equally spaced sample order. Choose the representation deliberately and preserve every retained observation, or disclose any reduction and retain access to the complete data.

Construct typed options directly. The legacy `ChartOptions` conversion copies legend, tooltip and palette settings but does not transfer axis controls. `TimeSeriesChartOptions` defaults to five-minute label spacing and `HH:mm` axis/tooltips; choose a positive spacing for the actual span and include seconds or milliseconds when needed. Keep timestamps on one explicit basis, preferably UTC for elapsed-time geometry: the native component does not normalize `DateTime` kinds or time zones. Put the date and time basis with the window, and preserve the exact instant and an explicit offset in inspection. Empty data and a single distinct timestamp need their own states rather than invented points or a fabricated time span.

Keep different units on distinct plots or a clearly declared shared-unit axis. Supply theme palette roles through `ChartPalette`; do not rely on native default colors for theme fidelity. Use `MatchBoundsToSize` when the chart must keep labels readable at its actual container width, and verify the settled SVG, tooltips and local overflow in the browser. Its native resize observation belongs to the chart; add no parallel page observer solely for this sizing behavior. The v9 time-series implementation does not interpolate curves, so do not promise smoothing through an interpolation option.

`SelectedIndexChanged` identifies the **series**, including marker clicks; it is not an exact sample-selection callback. Provide a native button or link in an accessible data ledger when an inspector needs a specific observation. Carry the chosen record and its owning successful read time together so refresh, sorting, pagination or retention eviction cannot silently replace open evidence. The native dialog still needs a verified accessible name, focus containment, Escape/close behavior and focus restoration. Live polling can remove a timestamp-keyed trigger while its dialog remains open: after the native dialog result, preserve native restoration when that trigger is mounted, otherwise focus a stable native page action. Observe and drain dialog opening and closure separately so disposal can close the admitted dialog before waiting for its result. A provider can retire before its page: in v9, `Close` raises the close event and `Dismiss` completes the reference's result. Ensure the admitted result completes during owner retirement even when the provider has already unsubscribed; verify client navigation and reload with an inspector open. Keep scaled display values distinct from raw encoded measurement fields and machine timestamps. Backend units and counter limits belong to the owning infrastructure guidance, such as [Runtime Metrics](../../monica-infra-observability/references/metrics.md), rather than this component reference.

Verify version-sensitive behavior against `src/MudBlazor/Components/Chart/MudChart.razor`, `Models/ChartData.TimeSeries.cs`, `Models/Options/TimeSeriesChartOptions.cs`, `Charts/TimeSeries.razor`/`.razor.cs`, and `Base/MudAxisChartBase.cs` in the installed dependency source. Source contracts establish API and selection semantics; browser checks establish containment, accessible interaction, theme contrast and visual readiness.

## Common Component Properties

### All MudComponents Support

- **Class** - CSS class name
- **Style** - Inline styles
- **UserAttributes** - Custom HTML attributes
- **@ref** - Component reference

### Form Component Common Properties

- **@bind-Value** - Two-way binding
- **Label** - Label
- **Placeholder** - Placeholder
- **Required** - Required
- **RequiredError** - Required error message
- **Disabled** - Disabled
- **ReadOnly** - Read only
- **Error** - Has error
- **ErrorText** - Error text
- **HelperText** - Helper text
- **Variant** - Variant (Text/Filled/Outlined)
- **Margin** - Margin (None/Dense/Normal)

### Color Property Values

- Primary
- Secondary
- Tertiary
- Info
- Success
- Warning
- Error
- Dark
- Light
- Transparent
- Inherit
- Surface

### Size Property Values

- Small
- Medium
- Large

### Variant Property Values

- Text
- Filled
- Outlined

## Search Tips

### Find Component Implementation

```bash
# Find component definition
Glob: "**/Mud{ComponentName}.razor"
Glob: "**/Mud{ComponentName}.razor.cs"

# Find component tests
Glob: "**/{ComponentName}Tests.cs"

# Find component styles
Glob: "**/_mud{componentname}.scss"
```

### Find Component Usage

```bash
# Find examples in documentation
Glob: "**/Examples/{ComponentName}*.razor"

# Find usage in tests
Grep: "<Mud{ComponentName}"
```

## Important Notes

1. **Component naming rule**: All components start with "Mud" prefix
2. **Parameter binding**: Use @bind-Value for two-way binding
3. **Event callbacks**: Use EventCallback<T> type
4. **Async operations**: Prefer methods with Async suffix
5. **Style customization**: Through Class and Style properties, or use theme system

## Quick Component Location

If unsure about component location, use these path patterns:

- Component definition: `src/MudBlazor/Components/{Category}/Mud{ComponentName}.razor`
- Component logic: `src/MudBlazor/Components/{Category}/Mud{ComponentName}.razor.cs`
- Component tests: `src/MudBlazor.UnitTests/Components/{ComponentName}Tests.cs`
- Component styles: `src/MudBlazor/Styles/components/_{componentname}.scss`

Categories include:
AppBar, Avatar, Badge, Breadcrumbs, Button, Card, Carousel, Chart, Checkbox, Chip, ColorPicker, DataGrid, DatePicker, Dialog, Divider, Drawer, ExpansionPanel, Field, FileUpload, Form, Grid, Hidden, Highlighter, Icon, Input, Layout, Link, List, Menu, MessageBox, NavMenu, Overlay, Pagination, Paper, Popover, Progress, Radio, Rating, ScrollToTop, Select, Skeleton, Slider, Snackbar, SpeedDial, Stepper, SwipeArea, Switch, Table, Tabs, TextField, Timeline, TimePicker, ToggleButton, Tooltip, TreeView, Typography, Virtualize
