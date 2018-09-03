using BirdTouchWebAPI.Data.Application;
using BirdTouchWebAPI.Data.Identity;
using BirdTouchWebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Linq;
using BirdTouchWebAPI.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using BirdTouchWebAPI.Services;
using Microsoft.Extensions.Configuration;
using BirdTouchWebAPI.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BirdTouchWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
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
        public UsersController(
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

        // POST api/users
        // Creates user based on login credentials
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Post([FromBody] LoginCredentials loginCredentials)
        {
            try
            {
                var result = await _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = loginCredentials.Username,
                    Email = loginCredentials.Username
                }, loginCredentials.Password);

                if (!result.Succeeded)
                {
                    return BadRequest();

                }

                var justCreatedUser = await _userManager.FindByNameAsync(loginCredentials.Username);

                _applicationContext.UserInfo.Add(new UserInfo()
                {
                    Id = Guid.NewGuid(),
                    FkUserId = justCreatedUser.Id
                });

                _applicationContext.BusinessInfo.Add(new BusinessInfo()
                {
                    Id = Guid.NewGuid(),
                    FkUserId = justCreatedUser.Id
                });

                await _applicationContext.SaveChangesAsync();

                var claims = new[]
                {
                        new Claim(ClaimTypes.Name, justCreatedUser.UserName),
                        new Claim(ClaimsConstants.USERID, justCreatedUser.Id.ToString())
                };

                return Ok(new
                {
                    User = new
                    {
                        Id = justCreatedUser.Id,
                        Username = justCreatedUser.UserName
                    },
                    JwtToken = JWTGenerator.GenerateJWTToken(_configuration, claims)
                });
            }
            catch (Exception)
            {
                var justCreatedUser = await _userManager.FindByNameAsync(loginCredentials.Username);
                if (justCreatedUser != null)
                {
                    var userInfo = _applicationContext
                                .UserInfo
                                .FirstOrDefault(u => u.Id == justCreatedUser.Id);

                    if (userInfo != null)
                    {
                        _applicationContext
                            .UserInfo
                            .Remove(userInfo);
                    }

                    var businessInfo = _applicationContext
                                .BusinessInfo
                                .FirstOrDefault(u => u.Id == justCreatedUser.Id);

                    if (businessInfo != null)
                    {
                        _applicationContext
                            .BusinessInfo
                            .Remove(businessInfo);
                    }

                    _applicationContext.SaveChanges();
                    await _userManager.DeleteAsync(justCreatedUser);
                }

                return BadRequest();
            }
        }

        [HttpGet]
        [Route("getPrivateInfo")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetPrivateInfo()
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

                var extendedUserInfo = await _applicationContext
                    .UserInfo
                    .AsNoTracking()
                    .Include(u => u.FkUser)
                    .Where(u => u.FkUserId == Guid.Parse(userId))
                    .Select(u => new
                    {
                        u.FkUser.UserName,
                        u.Adress,
                        u.Dateofbirth,
                        u.Description,
                        u.Email,
                        u.Fblink,
                        u.Firstname,
                        u.FkUserId,
                        u.Gpluslink,
                        u.Id,
                        u.Lastname,
                        u.Linkedinlink,
                        u.Phonenumber,
                        u.Profilepicturedata,
                        u.Twlink
                    }).FirstOrDefaultAsync();

                return Ok(extendedUserInfo);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPatch]
        [Route("patchPrivateInfo")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> PatchPrivateInfo([FromBody] UserInfo patchedUserInfo)
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

                var userInfo = await _applicationContext
                                        .UserInfo
                                        .FirstOrDefaultAsync(u => u.FkUserId == Guid.Parse(userId));
                if (userInfo == null)
                {
                    throw new NullReferenceException("UserInfo is missing");
                }

                userInfo.Firstname = patchedUserInfo.Firstname;
                userInfo.Lastname = patchedUserInfo.Lastname;
                userInfo.Adress = patchedUserInfo.Adress;
                userInfo.Dateofbirth = patchedUserInfo.Dateofbirth;
                userInfo.Email = patchedUserInfo.Email;
                userInfo.Phonenumber = patchedUserInfo.Phonenumber;
                userInfo.Description = patchedUserInfo.Description;
                userInfo.Fblink = patchedUserInfo.Fblink;
                userInfo.Gpluslink = patchedUserInfo.Gpluslink;
                userInfo.Linkedinlink = patchedUserInfo.Linkedinlink;
                userInfo.Twlink = patchedUserInfo.Twlink;

                if (patchedUserInfo.Profilepicturedata != null)
                {
                    userInfo.Profilepicturedata = patchedUserInfo.Profilepicturedata;
                }

                _applicationContext.Update(userInfo);
                await _applicationContext.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException);
            }
        }

        [HttpGet]
        [Route("getBusinessInfo")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetBusinessInfo()
        {
            try
            {
                var userId = User
                    .Claims
                    .FirstOrDefault(c => c.Type == ClaimsConstants.USERID).Value;

                if (string.IsNullOrEmpty(userId))
                {
                    throw new NullReferenceException();
                }

                BusinessInfo businessInfo = await _applicationContext
                    .BusinessInfo
                    .AsNoTracking()
                    .Where(u => u.FkUserId == Guid.Parse(userId))
                    .FirstOrDefaultAsync();

                return Ok(businessInfo);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPatch]
        [Route("patchBusinessInfo")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> PatchBusinessInfo([FromBody] BusinessInfo patchedUserInfo)
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

                var businessInfo = await _applicationContext
                                        .BusinessInfo
                                        .FirstOrDefaultAsync(u => u.FkUserId == Guid.Parse(userId));
                if (businessInfo == null)
                {
                    throw new NullReferenceException("UserInfo is missing");
                }

                businessInfo.Companyname = patchedUserInfo.Companyname;
                businessInfo.Adress = patchedUserInfo.Adress;
                businessInfo.Description = patchedUserInfo.Description;
                businessInfo.Email = patchedUserInfo.Email;
                businessInfo.Phonenumber = patchedUserInfo.Phonenumber;
                businessInfo.Website = patchedUserInfo.Website;

                if (patchedUserInfo.Profilepicturedata != null)
                {
                    businessInfo.Profilepicturedata = patchedUserInfo.Profilepicturedata;
                }

                _applicationContext.Update(businessInfo);
                await _applicationContext.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException);
            }
        }

        [HttpPost]
        [Route("makeUserVisible")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> MakeUserVisible([FromBody] ActiveUsers updatedActiveUser)
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

        [HttpDelete]
        [Route("makeUserInvisible")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> MakeUserInvisible([FromBody] ActiveUsers updatedActiveUser)
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
                    if (activeMode.ToString() == ActiveModesConstants.PRIVATE)
                    {
                        return Ok(JsonConvert.SerializeObject(new List<UserInfo>()));
                    }

                    if (activeMode.ToString() == ActiveModesConstants.BUSINESS)
                    {
                        return Ok(JsonConvert.SerializeObject(new List<BusinessInfo>()));
                    }
                }

                if (activeMode.ToString() == ActiveModesConstants.PRIVATE)
                {
                    var listOfUsersPrivateInfo = await _applicationContext
                        .UserInfo
                        .AsNoTracking()
                        .Where(u => listOfUsersIdNearMe.Contains(u.FkUserId))
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


        [HttpGet]
        [Route("doesusernameexist")]
        [AllowAnonymous]
        public async Task<IActionResult> DoesUsernameExist(string username)
        {
            try
            {
                bool userExists = false;
                var user = await _userManager.FindByNameAsync(username);

                if (user != null)
                {
                    userExists = true;
                }

                return Ok(new
                {
                    UserExists = userExists
                });
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}
