--liquibase formatted sql
--changeset dev_user:"create invoices table"
CREATE TABLE IF NOT EXISTS AppDomain.invoices (
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
ALTER TABLE AppDomain.invoices
ADD COLUMN IF NOT EXISTS amount DECIMAL(18, 2),
    ADD COLUMN IF NOT EXISTS currency VARCHAR(3),
    ADD COLUMN IF NOT EXISTS due_date TIMESTAMP WITH TIME ZONE,
    ADD COLUMN IF NOT EXISTS cashier_id UUID;
--changeset dev_user:"add payment fields to invoices table"
ALTER TABLE AppDomain.invoices
ADD COLUMN IF NOT EXISTS amount_paid DECIMAL(18, 2),
    ADD COLUMN IF NOT EXISTS payment_date TIMESTAMP WITH TIME ZONE;