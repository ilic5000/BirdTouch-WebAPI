using BirdTouchWebAPI.Data.Application;
using BirdTouchWebAPI.Data.Identity;
using BirdTouchWebAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        // Creates an user based on login credentials
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LoginCredentials loginCredentials)
        {
            var result = await _userManager.CreateAsync(new ApplicationUser
            {
                UserName = loginCredentials.Username,
                Email = loginCredentials.Username
            }, loginCredentials.Password);

            if (result.Succeeded)
            {
                //TODO: Maybe return JWT token
                return Ok();
            }

            return BadRequest();
        }
    }
}
