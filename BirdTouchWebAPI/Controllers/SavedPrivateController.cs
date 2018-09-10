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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BirdTouchWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SavedPrivateController : ControllerBase
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
        public SavedPrivateController(
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

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Post([FromBody] List<Guid> listOfPrivateUsersToBeSaved)
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

                var alreadySavedUsers = await _applicationContext
                                        .SavedPrivate
                                        .Where(u =>
                                                 u.FkUserId == Guid.Parse(userId))
                                        .Select(t => t.FkSavedContactId)
                                        .ToListAsync();

                var usersToBeDeleted =
                    alreadySavedUsers.Except(listOfPrivateUsersToBeSaved).ToList();

                listOfPrivateUsersToBeSaved = listOfPrivateUsersToBeSaved
                                                .Except(alreadySavedUsers).ToList();

                foreach (var userIdToBeSaved in listOfPrivateUsersToBeSaved)
                {
                    await _applicationContext.SavedPrivate.AddAsync(
                        new SavedPrivate()
                        {
                            Id = Guid.NewGuid(),
                            FkUserId = Guid.Parse(userId),
                            FkSavedContactId = userIdToBeSaved
                        });
                }

                var listForDeletion = await _applicationContext
                    .SavedPrivate
                    .Where(u => u.FkUserId == Guid.Parse(userId)
                            && usersToBeDeleted.Contains(u.FkSavedContactId))
                    .ToListAsync();

                _applicationContext.SavedPrivate.RemoveRange(listForDeletion);

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
