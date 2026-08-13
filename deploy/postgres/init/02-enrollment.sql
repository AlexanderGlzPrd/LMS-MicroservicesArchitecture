CREATE DATABASE enrollment;

CREATE ROLE enrollment_user
    WITH LOGIN
         PASSWORD 'enrollment_dev'
         NOSUPERUSER
         NOCREATEDB
         NOCREATEROLE;

-- PostgreSQL concede CONNECT a PUBLIC por defecto: sin este REVOKE cualquier rol
-- del cluster (incluido course_authoring_user) podria conectarse a esta base
REVOKE CONNECT ON DATABASE enrollment FROM PUBLIC;

GRANT CONNECT ON DATABASE enrollment TO enrollment_user;

\connect enrollment

GRANT USAGE, CREATE ON SCHEMA public TO enrollment_user;
