// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// AuthServer
    /// </summary>
    public class AuthServer
    {
        // DbContext
        private readonly ZeronServerDbContext m_DbContext;

        // ServerSettings
        private readonly ServerSettings m_Settings;

        // JwtTokenServer
        private readonly JwtTokenServer m_JwtTokenServer;

        /// <summary>
        /// AuthServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="settings"></param>
        /// <param name="jwtTokenServer"></param>
        /// <returns>Returns void.</returns>
        public AuthServer(
            ZeronServerDbContext dbContext, 
            ServerSettings settings, 
            JwtTokenServer jwtTokenServer)
        {
            m_DbContext = dbContext;
            m_Settings = settings;
            m_JwtTokenServer = jwtTokenServer;
        }

        /// <summary>
        /// SeedDefaultUserAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns void.</returns>
        public async Task SeedDefaultUserAsync(
            CancellationToken cancellationToken = default)
        {
            bool hasUsers = await m_DbContext.Users.AnyAsync(cancellationToken);

            if (hasUsers)
            {
                return;
            }

            UserEntity admin = new()
            {
                Id = Guid.NewGuid(),
                Username = m_Settings.DefaultAdminUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(m_Settings.DefaultAdminPassword),
                Role = ServerRoles.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            m_DbContext.Users.Add(admin);
            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "AuthServer seeded default admin user '{0}'.", admin.Username));
        }

        /// <summary>
        /// LoginAsync
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns LoginResponseType.</returns>
        public async Task<LoginResponseType> LoginAsync(
            string? username,
            string? password,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new LoginResponseType
                {
                    Success = false,
                    Message = "Username and password are required."
                };
            }

            UserEntity? user = await m_DbContext.Users
                .FirstOrDefaultAsync(item => item.Username == username, cancellationToken);

            if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return new LoginResponseType
                {
                    Success = false,
                    Message = "Invalid username or password."
                };
            }

            UserInfoType userInfo = JwtTokenServer.ToUserInfo(user);

            return new LoginResponseType
            {
                Success = true,
                Token = m_JwtTokenServer.CreateToken(user),
                User = userInfo
            };
        }

        /// <summary>
        /// GetUserByIdAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns UserInfoType or null.</returns>
        public async Task<UserInfoType?> GetUserByIdAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            UserEntity? user = await m_DbContext.Users
                .FirstOrDefaultAsync(item => item.Id == userId && item.IsActive, cancellationToken);

            return user == null ? null : JwtTokenServer.ToUserInfo(user);
        }

        /// <summary>
        /// GetUserFromPrincipal
        /// </summary>
        /// <param name="principal"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns UserInfoType or null.</returns>
        public async Task<UserInfoType?> GetUserFromPrincipalAsync(
            ClaimsPrincipal? principal,
            CancellationToken cancellationToken = default)
        {
            string? userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                return null;
            }

            return await GetUserByIdAsync(parsedUserId, cancellationToken);
        }

        /// <summary>
        /// ValidateAgentApiKey
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns>Returns bool.</returns>
        public bool ValidateAgentApiKey(
            string? apiKey)
        {
            return !string.IsNullOrWhiteSpace(apiKey)
                && string.Equals(apiKey, m_Settings.AgentApiKey, StringComparison.Ordinal);
        }
    }
}
