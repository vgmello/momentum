--liquibase formatted sql
--changeset dev_user:"create database" runInTransaction:false context:@setup
CREATE DATABASE AppDomain;
--changeset dev_user:"create AppDomain schema"
CREATE SCHEMA IF NOT EXISTS AppDomain;
