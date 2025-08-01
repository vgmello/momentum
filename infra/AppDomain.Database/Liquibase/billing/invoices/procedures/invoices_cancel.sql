--liquibase formatted sql
--changeset dev_user:"create invoices_cancel function" runOnChange:true splitStatements:false
CREATE OR REPLACE FUNCTION AppDomain.invoices_cancel(
        tenant_id uuid,
        invoice_id uuid,
        p_version int
    ) RETURNS SETOF AppDomain.invoices LANGUAGE SQL AS $$
UPDATE AppDomain.invoices
SET status = 'Cancelled',
    updated_date_utc = timezone('utc', now())
WHERE invoices.tenant_id = invoices_cancel.tenant_id
    AND invoices.invoice_id = invoices_cancel.invoice_id
    AND invoices.xmin = invoices_cancel.p_version
    AND invoices.status NOT IN ('Paid', 'Cancelled')
RETURNING *;
$$;