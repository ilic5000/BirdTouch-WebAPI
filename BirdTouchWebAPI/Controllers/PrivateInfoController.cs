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
    public class PrivateInfoController : ControllerBase
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
        public PrivateInfoController(
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
                        u.Gpluslink,
                        Id = u.FkUserId,
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Patch([FromBody] UserInfo patchedUserInfo)
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
    }
}
