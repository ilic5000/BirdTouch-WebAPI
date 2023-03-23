using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BirdTouchWebAPI.Services
{
    public static class JWTGenerator
    {
        /// <summary>
        /// Temporary solution for PoC
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        public static string GenerateJWTToken(IConfiguration configuration, Claim[] claims)
        {
            var key = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(configuration["JWTSecurityKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["JWTValidIssuer"],
                audience: configuration["JWTValidAudience"],
                claims: claims,
                expires: DateTime.Now.AddDays(int.Parse(configuration["JWTLifetimeDays"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
