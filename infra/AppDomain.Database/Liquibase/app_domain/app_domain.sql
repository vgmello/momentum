--liquibase formatted sql
--changeset dev_user:"create database" runInTransaction:false context:@setup
CREATE DATABASE app_domain;
--changeset dev_user:"create app_domain schema"
CREATE SCHEMA IF NOT EXISTS app_domain;
