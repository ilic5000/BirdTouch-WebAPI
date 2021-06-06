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