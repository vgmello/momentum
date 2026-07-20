--liquibase formatted sql
--changeset dev_user:"create main schema"
CREATE SCHEMA IF NOT EXISTS main;

--changeset dev_user:"create svcbus_queues schema"
CREATE SCHEMA IF NOT EXISTS svcbus_queues;
