--liquibase formatted sql
--changeset dev_user:"create cashier_currencies table"
CREATE TABLE IF NOT EXISTS main.cashier_currencies (
    tenant_id UUID NOT NULL,
    cashier_id UUID NOT NULL,
    currency_id UUID NOT NULL,
    effective_date_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    custom_currency_code VARCHAR(10) NOT NULL,
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    PRIMARY KEY (tenant_id, cashier_id, currency_id, effective_date_utc),
    CONSTRAINT fk_cashier_currencies_cashier
        FOREIGN KEY (tenant_id, cashier_id)
        REFERENCES main.cashiers(tenant_id, cashier_id)
);
