using BirdTouchWebAPI.Data.Application;
using BirdTouchWebAPI.Data.Identity;
using BirdTouchWebAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
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
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="context">The injected context</param>
        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _applicationContext = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }
        #endregion

        // POST api/users
        // Creates user based on login credentials
        [HttpPost]
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

                //TODO: Maybe return JWT token
                return Ok(new
                {
                    Id = justCreatedUser.Id,
                    Username = justCreatedUser.UserName
                });
            }
            catch (System.Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Route("doesusernameexist")]
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
            catch (System.Exception)
            {
                return BadRequest();
            }
        }
    }
}
