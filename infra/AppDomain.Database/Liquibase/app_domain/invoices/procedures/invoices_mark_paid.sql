--liquibase formatted sql
--changeset dev_user:"create invoices_mark_paid function" runOnChange:true splitStatements:false
CREATE OR REPLACE FUNCTION app_domain.invoices_mark_paid(
        tenant_id uuid,
        invoice_id uuid,
        p_version int,
        amount_paid decimal(18, 2),
        payment_date timestamp with time zone DEFAULT timezone('utc', now())
    ) RETURNS SETOF app_domain.invoices LANGUAGE SQL AS $$
UPDATE app_domain.invoices
SET status = 'Paid',
    updated_date_utc = timezone('utc', now()),
    amount_paid = invoices_mark_paid.amount_paid,
    payment_date = invoices_mark_paid.payment_date
WHERE invoices.tenant_id = invoices_mark_paid.tenant_id
    AND invoices.invoice_id = invoices_mark_paid.invoice_id
    AND invoices.xmin = invoices_mark_paid.p_version
    AND invoices.status NOT IN ('Paid', 'Cancelled')
    AND invoices.amount <= invoices_mark_paid.amount_paid
RETURNING *;
$$;
