# Pedido Header & Lines — Spec

## Purpose

Defines the multi-product `Pedido` (header) / `PedidoLine` model that replaced the single-product-per-row `ProviderPurchase` entity, letting one purchase order group several distinct products under one header.

## Requirements

### Requirement: Pedido header groups multiple product lines
The system SHALL expose a `Pedido` entity representing a provider purchase order, with `ProviderId`, `ExpectedArrival`, and `Notes`, and one or more `PedidoLine`s (`ProductId`, `RequiredAmount`, `Price`, `Notes`) beneath it. This replaces the single-product-per-row `ProviderPurchase` model entirely.

#### Scenario: Creating a pedido with multiple products
- **WHEN** a purchase manager submits `POST /api/Pedido` with one `ProviderId` and three `PedidoLine`s for three different products
- **THEN** one `Pedido` header is created with all three lines attached, each independently trackable

#### Scenario: Adding a line to an existing pedido
- **WHEN** a purchase manager submits `POST /api/Pedido/Line` for an existing, non-deleted `Pedido`
- **THEN** a new `PedidoLine` is appended to that header without affecting its existing lines

### Requirement: ProviderPurchase model is removed
The system SHALL NOT expose the `ProviderPurchase` entity, its repository, service, controller, or DTO after this change. No production data migration is performed for this table.

#### Scenario: Legacy endpoints no longer exist
- **WHEN** a client sends a request to any former `api/ProviderPurchase/*` route
- **THEN** the server returns `404 Not Found` (route no longer registered)

### Requirement: Pedido and line CRUD follow existing soft-delete and pagination conventions
`Pedido` and `PedidoLine` SHALL use the same `BaseEntity` soft-delete (`IsDeleted`) and `top`/`page` pagination conventions already used by other list endpoints in this codebase. (Deletion itself is a self-authorize-gated action — see `pedido-delete`/`pedido-line-delete` for the current guarded endpoints and mechanics.)

#### Scenario: Listing pedidos is paginated
- **WHEN** `GET /api/Pedido/All?top=10&page=2` is called
- **THEN** the response returns at most 10 non-deleted pedidos, skipping the first page's worth
