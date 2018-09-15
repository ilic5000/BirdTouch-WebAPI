# BirdTouch-WebAPI
BirdTouch-WebAPI is a server that is consumed by [Birdtouch Android app](https://github.com/ilic5000/BirdTouch-Client).

Written in .NET Core 2.1 with PostgreSQL database.
# Database installation
Follow the installation [procedure](https://github.com/ilic5000/BirdTouch-WebAPI/tree/master/BirdTouchWebAPI/DatabaseInstallScripts).
# Configuration
All of the configuration is done by editing [appsettings.json](https://github.com/ilic5000/BirdTouch-WebAPI/blob/master/BirdTouchWebAPI/appsettings.json)  file. Recommended changes:
* Update [connection string](https://github.com/ilic5000/BirdTouch-WebAPI/blob/4e485f6b6ef212a93d1acce73727e34249f25280/BirdTouchWebAPI/appsettings.json#L4) used to access the database 
* Change [JWTSecurityKey](https://github.com/ilic5000/BirdTouch-WebAPI/blob/4e485f6b6ef212a93d1acce73727e34249f25280/BirdTouchWebAPI/appsettings.json#L6) that is used for hashing of the authorization tokens.
