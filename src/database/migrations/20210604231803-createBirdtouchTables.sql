-- users table should come with core identity

-- Creating table 'business_info'
CREATE TABLE IF NOT EXISTS business_info (
	id uuid PRIMARY KEY NOT NULL,
    fk_user_id uuid NOT NULL,
    companyname text DEFAULT NULL,
    email text DEFAULT NULL,
    phonenumber text DEFAULT NULL,
    website text DEFAULT NULL,
    adress text DEFAULT NULL,
    description text default NULL,
    profilepicturedata bytea DEFAULT NULL,
    CONSTRAINT fk_business_info_users_id FOREIGN KEY (fk_user_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
);

-- Creating table 'user_info'
CREATE TABLE IF NOT EXISTS user_info (
    id uuid PRIMARY KEY NOT NULL,
    fk_user_id uuid NOT NULL,
    firstName text DEFAULT NULL,
    lastName text DEFAULT NULL,
    email text DEFAULT NULL,
    phoneNumber text DEFAULT NULL,
    dateOfBirth text DEFAULT NULL,
    adress text DEFAULT NULL,
    fbLink text DEFAULT NULL,
    twLink text DEFAULT NULL,
    gPlusLink text DEFAULT  NULL,
    linkedInLink text DEFAULT NULL,
    description text default NULL,
    profilePictureData bytea  NULL,
    CONSTRAINT fk_user_info_users_id FOREIGN KEY (fk_user_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
);

-- Creating table 'active_users'
CREATE TABLE IF NOT EXISTS active_users (
    id uuid PRIMARY KEY NOT NULL,
    fk_user_id uuid NOT NULL,
    location_latitude decimal(11,8) DEFAULT NULL,
    location_longitude decimal(11,8) DEFAULT NULL,
    active_mode int DEFAULT NULL,
    datetime_last_update timestamp NOT NULL,
    CONSTRAINT fk_active_users_users_id FOREIGN KEY (fk_user_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE   
);

-- Creating table 'saved_private'
CREATE TABLE IF NOT EXISTS saved_private (
    id uuid PRIMARY KEY NOT NULL,
    fk_user_id uuid NOT NULL,
    fk_saved_contact_id uuid NOT NULL,
    description text DEFAULT NULL,
    CONSTRAINT fk_saved_private_users_id FOREIGN KEY (fk_user_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT fk_saved_private_saved_contact_users_id FOREIGN KEY (fk_saved_contact_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
);

-- Creating table 'saved_business'
CREATE TABLE IF NOT EXISTS saved_business (
    id uuid PRIMARY KEY NOT NULL,
    fk_user_id uuid NOT NULL,
    fk_saved_contact_id uuid NOT NULL,
    description text DEFAULT NULL,
    CONSTRAINT fk_ssaved_business_users_id FOREIGN KEY (fk_user_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT fk_saved_business_saved_contact_users_id FOREIGN KEY (fk_saved_contact_id)
        REFERENCES public.asp_net_users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
);

-- ADD Foreign key constraints and index if needed