CREATE DATABASE course_authoring;

CREATE ROLE course_authoring_user
    WITH LOGIN
         PASSWORD 'course_authoring_dev'
         NOSUPERUSER
         NOCREATEDB
         NOCREATEROLE;

-- PostgreSQL concede CONNECT a PUBLIC por defecto: sin este REVOKE cualquier rol
-- del cluster (incluido enrollment_user) podria conectarse a esta base.
REVOKE CONNECT ON DATABASE course_authoring FROM PUBLIC;

GRANT CONNECT ON DATABASE course_authoring TO course_authoring_user;

\connect course_authoring

GRANT USAGE, CREATE ON SCHEMA public TO course_authoring_user;