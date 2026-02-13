--liquibase formatted sql
--changeset dev_user:"create app_domain database" runInTransaction:false context:!aspire
CREATE DATABASE app_domain;

--changeset dev_user:"create service_bus database" runInTransaction:false context:!aspire
CREATE DATABASE service_bus;
