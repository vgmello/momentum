--liquibase formatted sql
--changeset dev_user:"create cashiers table"
CREATE TABLE IF NOT EXISTS app_domain.cashiers (
    tenant_id UUID,
    cashier_id UUID,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100),
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    updated_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    PRIMARY KEY (tenant_id, cashier_id)
);

--changeset dev_user:"add email to cashiers table"
ALTER TABLE app_domain.cashiers
ADD COLUMN IF NOT EXISTS email VARCHAR(100);
