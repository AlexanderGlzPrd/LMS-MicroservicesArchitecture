CREATE DATABASE course_authoring;

CREATE ROLE course_authoring_user
    WITH LOGIN
         PASSWORD 'course_authoring_dev'
         NOSUPERUSER
         NOCREATEDB
         NOCREATEROLE;

GRANT CONNECT ON DATABASE course_authoring TO course_authoring_user;

\connect course_authoring

GRANT USAGE, CREATE ON SCHEMA public TO course_authoring_user;