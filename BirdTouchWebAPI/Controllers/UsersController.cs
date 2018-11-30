using BirdTouchWebAPI.Constants;
using BirdTouchWebAPI.Data.Application;
using BirdTouchWebAPI.Data.Identity;
using BirdTouchWebAPI.Models;
using BirdTouchWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
                    // TODO: Add types of incorrect password format and handle it in client
                    return BadRequest();
                }

                var justCreatedUser = await _userManager.FindByNameAsync(loginCredentials.Username);

                _applicationContext.UserInfo.Add(new UserInfo()
                {
                    Id = Guid.NewGuid(),
                    Firstname = loginCredentials.Firstname,
                    Lastname = loginCredentials.Lastname,
                    Description = loginCredentials.Description,
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
                        Firstname = loginCredentials.Firstname,
                        Lastname = loginCredentials.Lastname,
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
