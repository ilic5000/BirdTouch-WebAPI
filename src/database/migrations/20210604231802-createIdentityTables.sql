-- Table: public.asp_net_users

-- DROP TABLE public.asp_net_users;

CREATE TABLE IF NOT EXISTS public.asp_net_users
(
    id uuid NOT NULL,
    user_name character varying(256) COLLATE pg_catalog."default",
    normalized_user_name character varying(256) COLLATE pg_catalog."default",
    email character varying(256) COLLATE pg_catalog."default",
    normalized_email character varying(256) COLLATE pg_catalog."default",
    email_confirmed boolean NOT NULL,
    password_hash text COLLATE pg_catalog."default",
    security_stamp text COLLATE pg_catalog."default",
    concurrency_stamp text COLLATE pg_catalog."default",
    phone_number text COLLATE pg_catalog."default",
    phone_number_confirmed boolean NOT NULL,
    two_factor_enabled boolean NOT NULL,
    lockout_end timestamp with time zone,
    lockout_enabled boolean NOT NULL,
    access_failed_count integer NOT NULL,
    CONSTRAINT pk_asp_net_users PRIMARY KEY (id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE public.asp_net_users
    OWNER to postgres;

-- Index: email_index

-- DROP INDEX public.email_index;

CREATE INDEX IF NOT EXISTS email_index
    ON public.asp_net_users USING btree
    (normalized_email COLLATE pg_catalog."default")
    TABLESPACE pg_default;

-- Index: user_name_index

-- DROP INDEX public.user_name_index;

CREATE UNIQUE INDEX IF NOT EXISTS user_name_index
    ON public.asp_net_users USING btree
    (normalized_user_name COLLATE pg_catalog."default")
    TABLESPACE pg_default;


-- Table: public.asp_net_roles

-- DROP TABLE public.asp_net_roles;

CREATE TABLE IF NOT EXISTS public.asp_net_roles
(
    id uuid NOT NULL,
    name character varying(256) COLLATE pg_catalog."default",
    normalized_name character varying(256) COLLATE pg_catalog."default",
    concurrency_stamp text COLLATE pg_catalog."default",
    CONSTRAINT pk_asp_net_roles PRIMARY KEY (id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE public.asp_net_roles
    OWNER to postgres;

-- Index: role_name_index

-- DROP INDEX public.role_name_index;

CREATE UNIQUE INDEX IF NOT EXISTS role_name_index
    ON public.asp_net_roles USING btree
    (normalized_name COLLATE pg_catalog."default")
    TABLESPACE pg_default;



-- Table: public.asp_net_user_roles

-- DROP TABLE public.asp_net_user_roles;

CREATE TABLE IF NOT EXISTS public.asp_net_user_roles
(
    user_id uuid NOT NULL,
    role_id uuid NOT NULL,
    CONSTRAINT pk_asp_net_user_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_asp_net_user_roles_asp_net_roles_role_id FOREIGN KEY (role_id)
        REFERENCES public.asp_net_roles (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT fk_asp_net_user_roles_asp_net_users_user_id FOREIGN KEY (user_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE public.asp_net_user_roles
    OWNER to postgres;

-- Index: ix_asp_net_user_roles_role_id

-- DROP INDEX public.ix_asp_net_user_roles_role_id;

CREATE INDEX IF NOT EXISTS ix_asp_net_user_roles_role_id
    ON public.asp_net_user_roles USING btree
    (role_id)
    TABLESPACE pg_default;



-- Table: public.asp_net_user_tokens

-- DROP TABLE public.asp_net_user_tokens;

CREATE TABLE IF NOT EXISTS public.asp_net_user_tokens
(
    user_id uuid NOT NULL,
    login_provider text COLLATE pg_catalog."default" NOT NULL,
    name text COLLATE pg_catalog."default" NOT NULL,
    value text COLLATE pg_catalog."default",
    CONSTRAINT pk_asp_net_user_tokens PRIMARY KEY (user_id, login_provider, name),
    CONSTRAINT fk_asp_net_user_tokens_asp_net_users_user_id FOREIGN KEY (user_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE public.asp_net_user_tokens
    OWNER to postgres;


-- Table: public.asp_net_user_logins

-- DROP TABLE public.asp_net_user_logins;

CREATE TABLE IF NOT EXISTS public.asp_net_user_logins
(
    login_provider text COLLATE pg_catalog."default" NOT NULL,
    provider_key text COLLATE pg_catalog."default" NOT NULL,
    provider_display_name text COLLATE pg_catalog."default",
    user_id uuid NOT NULL,
    CONSTRAINT pk_asp_net_user_logins PRIMARY KEY (login_provider, provider_key),
    CONSTRAINT fk_asp_net_user_logins_asp_net_users_user_id FOREIGN KEY (user_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE public.asp_net_user_logins
    OWNER to postgres;

-- Index: ix_asp_net_user_logins_user_id

-- DROP INDEX public.ix_asp_net_user_logins_user_id;

CREATE INDEX IF NOT EXISTS ix_asp_net_user_logins_user_id
    ON public.asp_net_user_logins USING btree
    (user_id)
    TABLESPACE pg_default;


-- SEQUENCE: public.asp_net_user_claims_id_seq

-- DROP SEQUENCE public.asp_net_user_claims_id_seq;

CREATE SEQUENCE IF NOT EXISTS public.asp_net_user_claims_id_seq;

ALTER SEQUENCE public.asp_net_user_claims_id_seq
    OWNER TO postgres;


-- Table: public.asp_net_user_claims

-- DROP TABLE public.asp_net_user_claims;

CREATE TABLE IF NOT EXISTS public.asp_net_user_claims
(
    id integer NOT NULL DEFAULT nextval('asp_net_user_claims_id_seq'::regclass),
    user_id uuid NOT NULL,
    claim_type text COLLATE pg_catalog."default",
    claim_value text COLLATE pg_catalog."default",
    CONSTRAINT pk_asp_net_user_claims PRIMARY KEY (id),
    CONSTRAINT fk_asp_net_user_claims_asp_net_users_user_id FOREIGN KEY (user_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE public.asp_net_user_claims
    OWNER to postgres;

-- Index: ix_asp_net_user_claims_user_id

-- DROP INDEX public.ix_asp_net_user_claims_user_id;

CREATE INDEX IF NOT EXISTS ix_asp_net_user_claims_user_id
    ON public.asp_net_user_claims USING btree
    (user_id)
    TABLESPACE pg_default;

-- SEQUENCE: public.asp_net_role_claims_id_seq

-- DROP SEQUENCE public.asp_net_role_claims_id_seq;

CREATE SEQUENCE IF NOT EXISTS public.asp_net_role_claims_id_seq;

ALTER SEQUENCE public.asp_net_role_claims_id_seq
    OWNER TO postgres;

-- Table: public.asp_net_role_claims

-- DROP TABLE public.asp_net_role_claims;

CREATE TABLE IF NOT EXISTS public.asp_net_role_claims
(
    id integer NOT NULL DEFAULT nextval('asp_net_role_claims_id_seq'::regclass),
    role_id uuid NOT NULL,
    claim_type text COLLATE pg_catalog."default",
    claim_value text COLLATE pg_catalog."default",
    CONSTRAINT pk_asp_net_role_claims PRIMARY KEY (id),
    CONSTRAINT fk_asp_net_role_claims_asp_net_roles_role_id FOREIGN KEY (role_id)
        REFERENCES public.asp_net_roles (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE public.asp_net_role_claims
    OWNER to postgres;

-- Index: ix_asp_net_role_claims_role_id

-- DROP INDEX public.ix_asp_net_role_claims_role_id;

CREATE INDEX IF NOT EXISTS ix_asp_net_role_claims_role_id
    ON public.asp_net_role_claims USING btree
    (role_id)
    TABLESPACE pg_default;