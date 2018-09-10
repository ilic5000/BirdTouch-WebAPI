do $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='birdtouch_user')
  THEN
   CREATE USER birdtouch_user WITH PASSWORD 'birtouch22!2$#@!mit' SUPERUSER CREATEDB;
   RAISE NOTICE 'User birdtouch_user with the password birtouch22!2$#@!mit was created';
  END IF;
END
$$;