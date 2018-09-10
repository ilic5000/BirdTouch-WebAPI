using BirdTouchWebAPI.Constants;
using BirdTouchWebAPI.Data.Application;
using BirdTouchWebAPI.Data.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BirdTouchWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessInfoController : ControllerBase
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
        public BusinessInfoController(
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

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Get()
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Patch([FromBody] BusinessInfo patchedUserInfo)
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
    }
}
