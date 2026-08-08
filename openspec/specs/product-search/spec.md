# Product Search — Spec

## Purpose

Defines the product discovery surface (`GET /api/Productos/ByName`) that every product-picking flow in the app (add product to a weight, "Cambiar producto") sources its candidate products from. Distinguishes it from ID-based product resolution, which serves a different purpose — re-hydrating already-referenced products for historical records — and is intentionally exempt from the same filtering.

## Requirements

### Requirement: Disabled products excluded from search results
The system SHALL exclude products with `CSTATUSPRODUCTO = 0` (disabled/soft-deleted) from `GET /api/Productos/ByName` results, regardless of whether the name or code matches the search term.

#### Scenario: Disabled product does not appear in a matching search
- **WHEN** a client sends `GET /api/Productos/ByName?name=<term>` and a disabled product (`CSTATUSPRODUCTO = 0`) matches `<term>` by name or code
- **THEN** that product is not included in the response

#### Scenario: Enabled product still appears in a matching search
- **WHEN** a client sends `GET /api/Productos/ByName?name=<term>` and an enabled product (`CSTATUSPRODUCTO = 1`) matches `<term>` by name or code
- **THEN** that product is included in the response, subject to the existing pagination (`page`, `sizePage`) and ordering (`CNOMBREPRODUCTO`) behavior

### Requirement: ID-based product resolution remains unfiltered
The system SHALL NOT apply the `CSTATUSPRODUCTO` filter to `GetByIdAsync` or `GetByMultipleIdsAsync` — both continue to resolve a product by its ID regardless of its enabled/disabled status, so that already-referenced products (e.g. from historical `WeightDetail` records) remain resolvable.

#### Scenario: A disabled product is still resolvable by ID
- **WHEN** a client sends `GET /api/Productos/ById?id=<a disabled product's id>`
- **THEN** the server returns that product normally, unaffected by its `CSTATUSPRODUCTO` value
