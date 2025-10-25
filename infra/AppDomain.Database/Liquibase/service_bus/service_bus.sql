--liquibase formatted sql

--changeset dev_user:"create queues schema"
CREATE SCHEMA IF NOT EXISTS queues;