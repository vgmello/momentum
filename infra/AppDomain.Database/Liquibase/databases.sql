--liquibase formatted sql

--changeset dev_user:"create app_domain database" runInTransaction:false
CREATE DATABASE app_domain;

--changeset dev_user:"create service_bus database" runInTransaction:false
CREATE DATABASE service_bus;
