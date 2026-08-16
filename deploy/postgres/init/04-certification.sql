CREATE DATABASE certification;

CREATE ROLE certification_user
    WITH LOGIN
         PASSWORD 'certification_dev'
         NOSUPERUSER
         NOCREATEDB
         NOCREATEROLE;

REVOKE CONNECT ON DATABASE certification FROM PUBLIC;

GRANT CONNECT ON DATABASE certification TO certification_user;

\connect certification

GRANT USAGE, CREATE ON SCHEMA public TO certification_user;
