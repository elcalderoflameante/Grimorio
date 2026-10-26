\set ON_ERROR_STOP on
\pset pager off
BEGIN TRANSACTION READ ONLY;
SET LOCAL statement_timeout = '60s';
SET LOCAL lock_timeout = '5s';

-- Candidates only: no merging, identity validation, or cross-branch matching.
WITH normalized AS (
    SELECT c.*, regexp_replace(c."TaxId", '[[:space:]-]', '', 'g') AS document
    FROM billing."Customers" c
    WHERE c."IsDeleted" = false
), candidates AS (
    SELECT n.*, count(*) OVER (PARTITION BY "BranchId", document) AS copies
    FROM normalized n
    WHERE document ~ '^([0-9]{10}|[0-9]{13})$'
      AND document <> '9999999999999'
)
SELECT c."BranchId" AS sucursal_id,
       c.document AS identificacion,
       c.copies AS registros_duplicados,
       c."Id" AS cliente_id,
       c."Name" AS nombre,
       c."IsActive" AS activo,
       c."Phone" AS telefono,
       c."Email" AS correo,
       count(DISTINCT p."OrderId") AS ordenes,
       count(p."Id") AS cobros,
       coalesce(sum(p."OrderAmount"), 0) AS total_cobrado
FROM candidates c
LEFT JOIN billing."OrderPayments" p
       ON p."CustomerId" = c."Id" AND p."BranchId" = c."BranchId"
      AND p."IsDeleted" = false
WHERE c.copies > 1
GROUP BY c."BranchId", c.document, c.copies, c."Id", c."Name",
         c."IsActive", c."Phone", c."Email"
ORDER BY c."BranchId", c.document, c."Name", c."Id";

ROLLBACK;
