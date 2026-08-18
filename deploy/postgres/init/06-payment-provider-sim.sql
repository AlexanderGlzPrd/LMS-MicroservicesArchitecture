CREATE DATABASE payments;

CREATE ROLE payments_user
    WITH LOGIN
         PASSWORD 'payments_dev'
         NOSUPERUSER
         NOCREATEDB
         NOCREATEROLE;

REVOKE CONNECT ON DATABASE payments FROM PUBLIC;

GRANT CONNECT ON DATABASE payments TO payments_user;

\connect payments

GRANT USAGE, CREATE ON SCHEMA public TO payments_user;
