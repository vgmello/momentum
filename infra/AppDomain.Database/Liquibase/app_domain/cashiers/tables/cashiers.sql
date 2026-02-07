--liquibase formatted sql
--changeset dev_user:"create cashiers table"
CREATE TABLE IF NOT EXISTS main.cashiers (
    tenant_id UUID,
    cashier_id UUID,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100),
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    updated_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    PRIMARY KEY (tenant_id, cashier_id)
);

--changeset dev_user:"add email to cashiers table"
ALTER TABLE main.cashiers
ADD COLUMN IF NOT EXISTS email VARCHAR(100);

--changeset dev_user:"add unique constraints to cashiers table"
-- Ensure email is unique per tenant for cashiers
CREATE UNIQUE INDEX IF NOT EXISTS idx_cashiers_unique_email
ON main.cashiers(tenant_id, email)
WHERE email IS NOT NULL;

-- Index for querying cashiers by name
CREATE INDEX IF NOT EXISTS idx_cashiers_name
ON main.cashiers(tenant_id, name);
