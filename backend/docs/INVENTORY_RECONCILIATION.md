# Inventory reconciliation

Read-only diagnostics under `Inventory.Movements.View`, scoped to the authenticated branch.
`GET /api/inventory/conciliacion` accepts `articleId`, `warehouseId`, `search`, `severity`,
`page` (default 1) and `pageSize` (default 20, maximum 100).
Quantities represent a single repeatable-read database snapshot, not a physical count.
Counters apply to the article/warehouse scope; `totalFindings` and the page apply to search/severity too.

## Checks

- Confirmed: ledger versus stock projection, invalid active reservations, production snapshot versus linked movements.
- Review: negative stock, reservations exceeding stock, multiple deductions for the same payment item/reservation,
  purchase net quantity versus original inventory-entry quantity (including corrections and cancellations).
- Incomplete: sales, purchases or production without structured origin links; purchase lines with no linked entry.

Production quantities come from the production order and its ingredient snapshots, never current recipes.
Purchase quantities are compared in the original entry unit, never with today's conversion factors.
Different partial payments are not grouped together as suspected duplicates.
Historical reference strings are displayed but never parsed to assign a source or infer a reversal.
`Confirmed` identifies a violated consistency rule, not permission to modify or reverse data.

## Traceability

New sale deductions store `OrderPaymentItemId`, `StockReservationId` and `OrderItemId`.
New purchase entries and correction/cancellation movements store `PurchaseItemId`.
Production keeps its existing `ProductionOrderMovement` relation.
Origin fields are internal command data, not accepted from the public manual-movement DTO.
All links are saved in the existing operation transaction. Nullable foreign keys keep historical data unchanged.
`GET /api/inventory/movimientos/{id}/origen` reads even soft-deleted source documents,
always explicitly restricted to the authenticated branch. It exposes no customer/payment-method personal data.

## Limits

No automatic correction, reversal, cost adjustment or physical inventory is performed.
Legacy purchases may have an unlinked entry but a newly linked reversal; these require review, not automatic repair.
An unlinked sale cannot be assigned reliably to one of several partial payments.
This phase does not prove every paid ingredient was consumed: no immutable full ingredient-consumption plan
was stored per payment. It cannot reconstruct historical recipes or identify all incorrect consumption quantities.
Sales without ingredient requirements may legitimately have no stock movement.
Purchase base-unit conversion correctness and cost reconciliation are outside this first diagnostic scope.
Reports scan the branch's ledger and operation history; production deployments should measure execution time
before enabling frequent polling. The UI refreshes on demand, not periodically.

## Verification

- `dotnet run --project backend/tests/InventoryReconciliation.Checks`
- Build backend/frontend; verify EF has no pending model changes.
- With an isolated test database: change only the stock projection and confirm `BalanceMismatch`.
- Pay two parts of an order: each deduction must point to its own payment item, not appear as a duplicate.
- Cancel an unpaid item: no active reservation should remain.
- Compare production inputs/output with the stored production order.
- Create, edit, cancel a purchase; review movement origins including the soft-deleted old items.
- Confirm another branch cannot fetch a movement origin by ID.
- Repeat the report while paying/producing: no transient mismatch should be reported.

Builds and pure checks do not replace these transactional integration tests.
