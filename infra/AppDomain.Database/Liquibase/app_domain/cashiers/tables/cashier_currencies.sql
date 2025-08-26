--liquibase formatted sql
--changeset dev_user:"create cashier_currencies table"
CREATE TABLE IF NOT EXISTS app_domain.cashier_currencies (
    cashier_id UUID NOT NULL,
    currency_id UUID NOT NULL,
    effective_date_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    custom_currency_code VARCHAR(10) NOT NULL,
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    PRIMARY KEY (cashier_id, currency_id, effective_date_utc)
);
