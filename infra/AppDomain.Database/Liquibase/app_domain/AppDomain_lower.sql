--liquibase formatted sql
--changeset dev_user:"create database" runInTransaction:false context:@setup
CREATE DATABASE AppDomain_lower;
--changeset dev_user:"create AppDomain_lower schema"
CREATE SCHEMA IF NOT EXISTS AppDomain_lower;
