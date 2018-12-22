using BirdTouchWebAPI.Constants;
using BirdTouchWebAPI.Data.Application;
using BirdTouchWebAPI.Data.Identity;
using BirdTouchWebAPI.Extensions;
using BirdTouchWebAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BirdTouchWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActiveUsersController : ControllerBase
    {
        #region Protected Members
        /// <summary>
        /// The scoped Application context
        /// </summary>
        protected ApplicationDbContext _applicationContext;
        /// <summary>
        /// The manager for handling user creation, deletion, searching, roles etc...
        /// </summary>
        protected UserManager<ApplicationUser> _userManager;
        /// <summary>
        /// The manager for handling signing in and out for our users
        /// </summary>
        protected SignInManager<ApplicationUser> _signInManager;
        protected IConfiguration _configuration;
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="context">The injected context</param>
        public ActiveUsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _applicationContext = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }
        #endregion

        // Makes user visible
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Post([FromBody] ActiveUsers updatedActiveUser)
        {
            try
            {
                var userId = User
                        .Claims
                        .FirstOrDefault(c => c.Type == ClaimsConstants.USERID).Value;

                if (string.IsNullOrEmpty(userId))
                {
                    throw new NullReferenceException("UserId is missing");
                }

                var activeUser = await _applicationContext
                                        .ActiveUsers
                                        .FirstOrDefaultAsync(u =>
                                            u.FkUserId == Guid.Parse(userId)
                                            && u.ActiveMode == updatedActiveUser.ActiveMode);

                if (activeUser != null)
                {
                    activeUser.DatetimeLastUpdate = DateTime.UtcNow;
                    activeUser.LocationLatitude = updatedActiveUser.LocationLatitude;
                    activeUser.LocationLongitude = updatedActiveUser.LocationLongitude;
                    await _applicationContext.SaveChangesAsync();
                    return Ok();
                }

                var newActiveUser = new ActiveUsers()
                {
                    Id = Guid.NewGuid(),
                    ActiveMode = updatedActiveUser.ActiveMode,
                    DatetimeLastUpdate = DateTime.UtcNow,
                    FkUserId = Guid.Parse(userId),
                    LocationLatitude = updatedActiveUser.LocationLatitude,
                    LocationLongitude = updatedActiveUser.LocationLongitude,
                };

                _applicationContext.Add(newActiveUser);
                await _applicationContext.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException);
            }
        }

        // Makes user invisible
        [HttpDelete]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Delete([FromBody] ActiveUsers updatedActiveUser)
        {
            try
            {
                var userId = User
                        .Claims
                        .FirstOrDefault(c => c.Type == ClaimsConstants.USERID).Value;

                if (string.IsNullOrEmpty(userId))
                {
                    throw new NullReferenceException("UserId is missing");
                }

                var activeUser = await _applicationContext
                                        .ActiveUsers
                                        .FirstOrDefaultAsync(u =>
                                            u.FkUserId == Guid.Parse(userId)
                                            && u.ActiveMode == updatedActiveUser.ActiveMode);

                if (activeUser != null)
                {
                    _applicationContext.Remove(activeUser);
                    await _applicationContext.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException);
            }
        }

        /// <summary>
        /// Gets users that are near logged in user
        /// </summary>
        /// <param name="activeMode">Get private or business users</param>
        /// <param name="radiusOfSearch">Search radius in kilometers</param>
        /// <returns></returns>
        [HttpGet]
        [Route("getUsersNearMe")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetUsersNearMe(int? activeMode, double? radiusOfSearch)
        {
            try
            {
                var userId = User
                        .Claims
                        .FirstOrDefault(c => c.Type == ClaimsConstants.USERID).Value;

                if (string.IsNullOrEmpty(userId))
                {
                    throw new NullReferenceException("UserId is missing");
                }

                if (radiusOfSearch == null
                    || radiusOfSearch == 0)
                {
                    return BadRequest();
                }

                var activeUser = await _applicationContext
                                        .ActiveUsers
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(u =>
                                            u.FkUserId == Guid.Parse(userId)
                                            && u.ActiveMode == activeMode);

                if (activeUser == null
                    || activeUser.LocationLatitude == null
                    || activeUser.LocationLongitude == null)
                {
                    return BadRequest();
                }

                var activeUserCoordinates = new Coordinate()
                {
                    Latitude = (double)activeUser.LocationLatitude,
                    Longitude = (double)activeUser.LocationLongitude
                };

                // TODO: Remove when live
                Console.WriteLine();
                Console.WriteLine($"User {activeUser.FkUserId} at location:");
                Console.WriteLine($"Latitude: {activeUser.LocationLatitude}");
                Console.WriteLine($"Longitude: {activeUser.LocationLongitude}");
                Console.WriteLine($"is searching users at radius of {radiusOfSearch} km in mode: {activeMode}");

                var listOfUsersIdNearMe = await _applicationContext
                                            .ActiveUsers
                                            .AsNoTracking()
                                            .Where(u => u.ActiveMode == activeMode
                                                     && u.FkUserId != activeUser.FkUserId
                                                     && activeUserCoordinates
                                                          .DistanceTo(
                                                            (double)u.LocationLatitude,
                                                            (double)u.LocationLongitude)
                                                        < radiusOfSearch)
                                            .Select(u => u.FkUserId)
                                            .ToListAsync();

                if (listOfUsersIdNearMe.Count == 0)
                {
                    // TODO: Remove when live
                    Console.WriteLine("Found 0 users");
                    Console.WriteLine();

                    if (activeMode.ToString() == ActiveModesConstants.PRIVATE)
                    {
                        return Ok(JsonConvert.SerializeObject(new List<UserInfo>()));
                    }

                    if (activeMode.ToString() == ActiveModesConstants.BUSINESS)
                    {
                        return Ok(JsonConvert.SerializeObject(new List<BusinessInfo>()));
                    }
                }

                // TODO: Remove when live
                Console.WriteLine("Found: ");
                foreach (var userIdFromList in listOfUsersIdNearMe)
                {
                    var userFromDb = await _applicationContext.ActiveUsers.AsNoTracking().
                                                                           Where(a => a.FkUserId == userIdFromList
                                                                                      && a.ActiveMode == activeMode)
                                                                           .FirstOrDefaultAsync();

                    Console.WriteLine($" {userFromDb.FkUserId} with distance {activeUserCoordinates.DistanceTo((double)userFromDb.LocationLatitude, (double)userFromDb.LocationLongitude)}");
                }

                Console.WriteLine("------------------- end of list of near users");

                if (activeMode.ToString() == ActiveModesConstants.PRIVATE)
                {
                    var listOfUsersPrivateInfo = await _applicationContext
                        .UserInfo
                        .AsNoTracking()
                        .Where(u => listOfUsersIdNearMe.Contains(u.FkUserId)
                                    && (!string.IsNullOrEmpty(u.Firstname)
                                        || !string.IsNullOrEmpty(u.Lastname))
                                    && (!string.IsNullOrEmpty(u.Phonenumber)
                                        || !string.IsNullOrEmpty(u.Description) //TODO: Maybe remove description in future
                                        || !string.IsNullOrEmpty(u.Fblink)
                                        || !string.IsNullOrEmpty(u.Twlink)
                                        || !string.IsNullOrEmpty(u.Gpluslink)
                                        || !string.IsNullOrEmpty(u.Linkedinlink)
                                        || !string.IsNullOrEmpty(u.Email)))
                        .Select(
                        u => new
                        {
                            u.FkUserId,
                            u.Adress,
                            u.Dateofbirth,
                            u.Description,
                            u.Email,
                            u.Fblink,
                            u.Firstname,
                            u.Lastname,
                            u.Gpluslink,
                            u.Id,
                            u.Linkedinlink,
                            u.Phonenumber,
                            u.Profilepicturedata,
                            u.Twlink
                        })
                        .ToListAsync();

                    return (Ok(JsonConvert.SerializeObject(listOfUsersPrivateInfo)));
                }

                if (activeMode.ToString() == ActiveModesConstants.BUSINESS)
                {
                    var listOfUsersBusinessInfo = await _applicationContext
                        .BusinessInfo
                        .AsNoTracking()
                        .Where(u => listOfUsersIdNearMe.Contains(u.FkUserId))
                        .Select(
                        u => new
                        {
                            u.Adress,
                            u.Companyname,
                            u.Description,
                            u.Email,
                            u.FkUserId,
                            u.Id,
                            u.Phonenumber,
                            u.Profilepicturedata,
                            u.Website
                        })
                        .ToListAsync();

                    return (Ok(JsonConvert.SerializeObject(listOfUsersBusinessInfo)));
                }

                return BadRequest();
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException);
            }
        }
    }
}