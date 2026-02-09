--liquibase formatted sql
--changeset dev_user:"create invoices table"
CREATE TABLE IF NOT EXISTS main.invoices (
    tenant_id UUID,
    invoice_id UUID,
    name VARCHAR(100) NOT NULL,
    status TEXT NOT NULL,
    amount DECIMAL(18, 2),
    currency VARCHAR(3),
    due_date TIMESTAMP WITH TIME ZONE,
    cashier_id UUID,
    amount_paid DECIMAL(18, 2),
    payment_date TIMESTAMP WITH TIME ZONE,
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    updated_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    PRIMARY KEY (tenant_id, invoice_id)
);

-- Example: ALTER TABLE changeset demonstrating how to add columns to an existing table.
-- These columns already exist in the CREATE TABLE above, but this shows the Liquibase
-- pattern for evolving a schema over time with separate changesets.
--changeset dev_user:"add amount and other fields to invoices table"
ALTER TABLE main.invoices
ADD COLUMN IF NOT EXISTS amount DECIMAL(18, 2),
    ADD COLUMN IF NOT EXISTS currency VARCHAR(3),
    ADD COLUMN IF NOT EXISTS due_date TIMESTAMP WITH TIME ZONE,
    ADD COLUMN IF NOT EXISTS cashier_id UUID;

--changeset dev_user:"add payment fields to invoices table"
ALTER TABLE main.invoices
ADD COLUMN IF NOT EXISTS amount_paid DECIMAL(18, 2),
    ADD COLUMN IF NOT EXISTS payment_date TIMESTAMP WITH TIME ZONE;

--changeset dev_user:"add performance indexes to invoices table"
-- Index for querying invoices by status and tenant
CREATE INDEX IF NOT EXISTS idx_invoices_tenant_status
ON main.invoices(tenant_id, status);

-- Index for querying invoices by due date
CREATE INDEX IF NOT EXISTS idx_invoices_due_date
ON main.invoices(due_date)
WHERE due_date IS NOT NULL;

-- Index for querying invoices by cashier
CREATE INDEX IF NOT EXISTS idx_invoices_cashier
ON main.invoices(tenant_id, cashier_id)
WHERE cashier_id IS NOT NULL;

-- Index for querying invoices by created date
CREATE INDEX IF NOT EXISTS idx_invoices_created_date
ON main.invoices(tenant_id, created_date_utc);

--changeset dev_user:"add foreign key constraint for cashier"
ALTER TABLE main.invoices
ADD CONSTRAINT fk_invoices_cashier
    FOREIGN KEY (tenant_id, cashier_id)
    REFERENCES main.cashiers(tenant_id, cashier_id);
