--liquibase formatted sql
--changeset dev_user:"create invoices table"
CREATE TABLE IF NOT EXISTS app_domain.invoices (
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

--changeset dev_user:"add amount and other fields to invoices table"
ALTER TABLE app_domain.invoices
ADD COLUMN IF NOT EXISTS amount DECIMAL(18, 2),
    ADD COLUMN IF NOT EXISTS currency VARCHAR(3),
    ADD COLUMN IF NOT EXISTS due_date TIMESTAMP WITH TIME ZONE,
    ADD COLUMN IF NOT EXISTS cashier_id UUID;

--changeset dev_user:"add payment fields to invoices table"
ALTER TABLE app_domain.invoices
ADD COLUMN IF NOT EXISTS amount_paid DECIMAL(18, 2),
    ADD COLUMN IF NOT EXISTS payment_date TIMESTAMP WITH TIME ZONE;

--changeset dev_user:"add performance indexes to invoices table"
-- Index for querying invoices by status and tenant
CREATE INDEX IF NOT EXISTS idx_invoices_tenant_status
ON app_domain.invoices(tenant_id, status);

-- Index for querying invoices by due date
CREATE INDEX IF NOT EXISTS idx_invoices_due_date
ON app_domain.invoices(due_date)
WHERE due_date IS NOT NULL;

-- Index for querying invoices by cashier
CREATE INDEX IF NOT EXISTS idx_invoices_cashier
ON app_domain.invoices(tenant_id, cashier_id)
WHERE cashier_id IS NOT NULL;

-- Index for querying invoices by created date
CREATE INDEX IF NOT EXISTS idx_invoices_created_date
ON app_domain.invoices(tenant_id, created_date_utc);
