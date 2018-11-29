using BirdTouchWebAPI.Constants;
using BirdTouchWebAPI.Data.Application;
using BirdTouchWebAPI.Data.Identity;
using BirdTouchWebAPI.Models;
using BirdTouchWebAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BirdTouchWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        #region Protected Members
        protected ApplicationDbContext _context;
        protected UserManager<ApplicationUser> _userManager;
        protected SignInManager<ApplicationUser> _signInManager;
        protected IConfiguration _configuration;
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="context">The injected context</param>
        public LoginController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }
        #endregion

        /// <summary>
        /// Creates JWT token when credentials are valid
        /// </summary>
        /// <param name="loginCredentials">Credentials used for login</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post(LoginCredentials loginCredentials)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(loginCredentials.Username);

                if (user == null)
                {
                    return Forbid();
                }

                // Sign user in with the valid credentials
                var result = await _signInManager.CheckPasswordSignInAsync(
                    user,
                    loginCredentials.Password,
                    true);

                // If successful...
                if (!result.Succeeded)
                {
                    return Forbid();
                }

                var claims = new[]
                   {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimsConstants.USERID, user.Id.ToString())
                    };

                var userInfo = await _context
                    .UserInfo
                    .AsNoTracking()
                    .Where(u => u.FkUserId == user.Id).FirstOrDefaultAsync();

                return Ok(new
                {
                    User = new
                    {
                        Id = user.Id,
                        Username = user.UserName,
                        Firstname = userInfo?.Firstname,
                        Lastname = userInfo?.Lastname,
                        ProfilePictureData = userInfo?.Profilepicturedata
                    },
                    JwtToken = JWTGenerator.GenerateJWTToken(_configuration, claims)
                });
            }
            catch (Exception e)
            {
                return BadRequest();
            }
        }
    }
}
