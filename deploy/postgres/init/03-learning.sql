CREATE DATABASE learning;

CREATE ROLE learning_user
    WITH LOGIN
         PASSWORD 'learning_dev'
         NOSUPERUSER
         NOCREATEDB
         NOCREATEROLE;

REVOKE CONNECT ON DATABASE learning FROM PUBLIC;

GRANT CONNECT ON DATABASE learning TO learning_user;

\connect learning

GRANT USAGE, CREATE ON SCHEMA public TO learning_user;
