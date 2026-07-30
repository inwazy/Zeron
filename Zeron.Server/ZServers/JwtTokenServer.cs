// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// JwtTokenServer
    /// </summary>
    public class JwtTokenServer
    {
        // ServerSettings
        private readonly ServerSettings m_Settings;

        /// <summary>
        /// JwtTokenServer
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>Returns void.</returns>
        public JwtTokenServer(ServerSettings settings)
        {
            m_Settings = settings;
        }

        /// <summary>
        /// CreateToken
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Returns JWT string.</returns>
        public string CreateToken(UserEntity user)
        {
            Claim[] claims =
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            ];

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(m_Settings.JwtSecret));
            SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);
            DateTime expires = DateTime.UtcNow.AddMinutes(m_Settings.JwtExpireMinutes);

            JwtSecurityToken token = new(
                issuer: m_Settings.JwtIssuer,
                audience: m_Settings.JwtIssuer,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// CreateClaimsPrincipal
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Returns ClaimsPrincipal.</returns>
        public static ClaimsPrincipal CreateClaimsPrincipal(UserEntity user)
        {
            Claim[] claims =
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            ];

            ClaimsIdentity identity = new(claims, "ZeronCookie");

            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// ToUserInfo
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Returns UserInfoType.</returns>
        public static UserInfoType ToUserInfo(UserEntity user)
        {
            return new UserInfoType
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                Role = user.Role
            };
        }
    }
}
