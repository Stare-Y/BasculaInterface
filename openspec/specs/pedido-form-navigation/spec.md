# Pedido Form Navigation — Spec

## Purpose

Defines `PedidoFormView`'s chrome and navigation behavior: a single conventional back action consistent with the rest of the app's responsive WinUI side-panel / Android bottom-bar pattern, preservation of existing header/line actions through the layout change, automation identifiers for the VM bot harness, and readability/interaction rules for the Productos del Pedido section.

## Requirements

### Requirement: PedidoFormView exposes a single conventional back action
`PedidoFormView` SHALL expose exactly one back action (`Regresar`), positioned and styled consistent with the app's established WinUI side-panel / Android bottom-bar chrome (as used by `PedidoListView`), rather than a bottom-row "Cancelar" button. No standalone "Cancelar" control SHALL exist alongside it.

#### Scenario: Back button is present and positioned per convention
- **WHEN** an operator opens `PedidoFormView` (create or edit)
- **THEN** a `Regresar` button is visible in the WinUI side panel (or the Android bottom bar, depending on platform), alongside `Guardar` and, when applicable, `Eliminar`

#### Scenario: No separate Cancel action exists
- **WHEN** an operator inspects the action controls on `PedidoFormView`
- **THEN** there is no button labeled "Cancelar" or equivalent distinct from the back action

### Requirement: Back navigates away without a confirmation prompt
Tapping the back action SHALL navigate away from `PedidoFormView` immediately, with no unsaved-changes confirmation or save prompt — preserving the screen's existing exit behavior.

#### Scenario: Back pops the navigation stack
- **WHEN** an operator taps `Regresar` on `PedidoFormView`, with or without unsaved header/line edits
- **THEN** the app navigates back to the previous screen (`PedidoListView`) immediately, with no dialog shown first

### Requirement: Existing header and line actions remain functional after the layout change
`Guardar` and `Eliminar` (when visible) SHALL remain present and fully functional within the restructured layout, and the `Concluded` read-only state handling (disabling provider/date/notes/save/add-line controls) SHALL continue to work against the same named controls.

#### Scenario: Save still creates/updates the pedido header
- **WHEN** an operator fills in the provider and expected arrival and taps `Guardar` in the new layout
- **THEN** the pedido header is created or updated exactly as before the layout change, and the Lines section reveal-on-first-save behavior is unaffected

#### Scenario: Concluded pedidos still render read-only
- **WHEN** an operator opens an existing pedido whose `Concluded` flag is `true`
- **THEN** the provider, date, notes, save, and add-line controls are disabled and `Eliminar` is hidden, exactly as before the layout change

### Requirement: Touched controls carry stable automation identifiers
The buttons introduced or relabeled by this change (`BtnSave`, `BtnDelete`, `BtnBack`, and their Android counterparts) SHALL carry an `AutomationProperties.AutomationId`, so the VM bot harness can locate and exercise them.

#### Scenario: Bot harness can find the back button
- **WHEN** the FlaUI bot harness opens `PedidoFormView` and queries for the back button by its `AutomationId`
- **THEN** the control is found and clicking it navigates back to `PedidoListView`

### Requirement: Productos section text is readable in both themes
Every label in the Productos del Pedido section (the line cards, the empty-state message, and the "Agregar Producto" panel) SHALL use an explicit theme-aware text color, rather than relying on the shared `Label` style's non-theme-aware default.

#### Scenario: Line card text is legible in light theme
- **WHEN** an operator views `PedidoFormView` in light theme with at least one pedido line
- **THEN** the product name, pending amount, and status text on that line's card are clearly legible against the card background

### Requirement: Pedido lines default to a collapsed summary row
Each pedido line in the Productos del Pedido `CollectionView` SHALL render as a collapsed one-row summary (product name, pending amount, status) by default. Tapping a line SHALL reveal its full detail (received/required amounts, the dis-tare note when applicable, and the Pesar/Cerrar actions); tapping it again SHALL collapse it back.

#### Scenario: Line starts collapsed and expands on tap
- **WHEN** an operator opens a pedido with existing lines
- **THEN** each line shows only its summary row, and tapping one reveals its detail and action buttons without affecting the other lines

#### Scenario: Pesar/Cerrar remain reachable and functional only when expanded
- **WHEN** an operator taps a line to expand it and then taps `Pesar` or `Cerrar`
- **THEN** the action behaves exactly as before this change (opens the convert-to-weight popup, or closes the line)
