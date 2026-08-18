CREATE DATABASE purchase;

CREATE ROLE purchase_user
    WITH LOGIN
         PASSWORD 'purchase_dev'
         NOSUPERUSER
         NOCREATEDB
         NOCREATEROLE;

REVOKE CONNECT ON DATABASE purchase FROM PUBLIC;

GRANT CONNECT ON DATABASE purchase TO purchase_user;

\connect purchase

GRANT USAGE, CREATE ON SCHEMA public TO purchase_user;
