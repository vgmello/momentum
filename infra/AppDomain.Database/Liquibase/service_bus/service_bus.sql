--liquibase formatted sql

--changeset dev_user:"create database" runInTransaction:false context:@setup
CREATE DATABASE service_bus;

--changeset dev_user:"create queues schema"
CREATE SCHEMA IF NOT EXISTS queues;
