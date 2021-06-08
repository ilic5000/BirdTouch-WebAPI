# BirdTouch-WebAPI
BirdTouch-WebAPI is a server that is consumed by [Birdtouch Android app](https://github.com/ilic5000/BirdTouch-Client).

Server is written in .NET 5.0 using PostgreSQL database.

# Requirements
- Docker
- Docker compose
- Port `4050` to be free and not used by any app on the machine where docker daemon is running

# Installation

- Configure `.env` file (see Configuration section)
- Run `docker-compose up -d` from the root of this repo
- Run `docker-compose ps` to check if all services are up and running
    - Note: `database-migration` service should be in State `Exit 0`
- Congratulations! WebAPI is now available on port `4050`

# Configuration
All of the configuration is done by editing [.env](https://github.com/ilic5000/BirdTouch-WebAPI/blob/master/BirdTouchWebAPI/.env) file. 

* Recommended changes:
    * Update `POSTGRES_PASSWORD` and connection strings used to access the database. 
    * Update `PGADMIN_DEFAULT_PASSWORD` password used for accessing PgAdmin app.
    * Change `JWTSecurityKey` that is used for hashing of the authorization tokens.

* Optional:
    * `RemoveInactiveUsersRunEvery` scheduled task that will check if there are inactive users
    * `RemoveInactiveUsersRemoveUsersOlderThan` set how many hours of inactivity are considered for user to be in inactive state

# Good to know specifics

## Obtaining IP address of the WSL2 Linux container

If you are using WSL2 on Windows10 and have Linux container where you installed docker, in order to access Birdtouch WebAPI (e.g. from the Birdtouch Client running on Android emulator) you need to found out the WSL2 session's IP address. 

Do the following procedure:

1. Login to Linux machine with WSL2
2. Execute `ifconfig`
3. Find the `net` value of the `eth0:`, for example it would be `172.22.200.173`
4. Congratulations, now you can access your Birdtouch WebAPI running on WSL2 Linux container via `172.22.200.173:4050` 