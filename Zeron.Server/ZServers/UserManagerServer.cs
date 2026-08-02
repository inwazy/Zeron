// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Zeron.Server.Data;
using Zeron.Server.Data.Entities;
using Zeron.Server.ZCore;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.Server.ZServers
{
    /// <summary>
    /// UserManagerServer
    /// </summary>
    public class UserManagerServer
    {
        // Database context
        private readonly ZeronServerDbContext m_DbContext;

        /// <summary>
        /// UserManagerServer
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>Returns void.</returns>
        public UserManagerServer(
            ZeronServerDbContext dbContext)
        {
            m_DbContext = dbContext;
        }

        /// <summary>
        /// GetUsersAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns user list.</returns>
        public async Task<List<UserInfoType>> GetUsersAsync(
            CancellationToken cancellationToken = default)
        {
            List<UserEntity> users = await m_DbContext.Users
                .OrderBy(user => user.Username)
                .ToListAsync(cancellationToken);

            return users.Select(JwtTokenServer.ToUserInfo).ToList();
        }

        /// <summary>
        /// GetUserAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns user or null.</returns>
        public async Task<UserInfoType?> GetUserAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            UserEntity? user = await m_DbContext.Users
                .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

            return user == null ? null : JwtTokenServer.ToUserInfo(user);
        }

        /// <summary>
        /// CreateUserAsync
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns created user or error message.</returns>
        public async Task<(UserInfoType? User, string? Error)> CreateUserAsync(
            UserCreateRequestType request,
            CancellationToken cancellationToken = default)
        {
            string? username = request.Username?.Trim();
            string? password = request.Password;
            string? role = NormalizeRole(request.Role);

            if (string.IsNullOrWhiteSpace(username))
            {
                return (null, "Username is required.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return (null, "Password must be at least 6 characters.");
            }

            if (role == null)
            {
                return (null, "Role must be Admin, Operator, or Viewer.");
            }

            bool exists = await m_DbContext.Users
                .AnyAsync(user => user.Username == username, cancellationToken);

            if (exists)
            {
                return (null, "Username already exists.");
            }

            UserEntity user = new()
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = DateTime.UtcNow
            };

            m_DbContext.Users.Add(user);
            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "UserManagerServer created user '{0}' with role '{1}'.", user.Username, user.Role));

            return (JwtTokenServer.ToUserInfo(user), null);
        }

        /// <summary>
        /// UpdateUserAsync
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="request"></param>
        /// <param name="actorUserId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns updated user or error message.</returns>
        public async Task<(UserInfoType? User, string? Error)> UpdateUserAsync(
            Guid userId,
            UserUpdateRequestType request,
            Guid? actorUserId = null,
            CancellationToken cancellationToken = default)
        {
            UserEntity? user = await m_DbContext.Users
                .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

            if (user == null)
            {
                return (null, "User not found.");
            }

            if (request.Role != null)
            {
                string? role = NormalizeRole(request.Role);

                if (role == null)
                {
                    return (null, "Role must be Admin, Operator, or Viewer.");
                }

                if (user.Role == ServerRoles.Admin
                    && role != ServerRoles.Admin
                    && !await HasOtherActiveAdminAsync(user.Id, cancellationToken))
                {
                    return (null, "Cannot demote the last active Admin.");
                }

                user.Role = role;
            }

            if (request.IsActive.HasValue)
            {
                if (!request.IsActive.Value
                    && actorUserId.HasValue
                    && actorUserId.Value == user.Id)
                {
                    return (null, "Cannot deactivate your own account.");
                }

                if (!request.IsActive.Value
                    && user.Role == ServerRoles.Admin
                    && user.IsActive
                    && !await HasOtherActiveAdminAsync(user.Id, cancellationToken))
                {
                    return (null, "Cannot deactivate the last active Admin.");
                }

                user.IsActive = request.IsActive.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                if (request.Password.Length < 6)
                {
                    return (null, "Password must be at least 6 characters.");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                user.MustChangePassword = true;
            }

            await m_DbContext.SaveChangesAsync(cancellationToken);

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "UserManagerServer updated user '{0}'.", user.Username));

            return (JwtTokenServer.ToUserInfo(user), null);
        }

        /// <summary>
        /// HasOtherActiveAdminAsync
        /// </summary>
        /// <param name="excludeUserId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns bool.</returns>
        private async Task<bool> HasOtherActiveAdminAsync(
            Guid excludeUserId, 
            CancellationToken cancellationToken)
        {
            return await m_DbContext.Users.AnyAsync(
                user => user.Id != excludeUserId
                    && user.Role == ServerRoles.Admin
                    && user.IsActive,
                cancellationToken);
        }

        /// <summary>
        /// NormalizeRole
        /// </summary>
        /// <param name="role"></param>
        /// <returns>Returns normalized role or null.</returns>
        private static string? NormalizeRole(
            string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            if (string.Equals(role, ServerRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return ServerRoles.Admin;
            }

            if (string.Equals(role, ServerRoles.Operator, StringComparison.OrdinalIgnoreCase))
            {
                return ServerRoles.Operator;
            }

            if (string.Equals(role, ServerRoles.Viewer, StringComparison.OrdinalIgnoreCase))
            {
                return ServerRoles.Viewer;
            }

            return null;
        }
    }
}
