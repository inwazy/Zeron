// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Zeron.Server.Data.Entities;

namespace Zeron.Server.Data
{
    /// <summary>
    /// ZeronServerDbContext
    /// </summary>
    public class ZeronServerDbContext : DbContext
    {
        /// <summary>
        /// ZeronServerDbContext
        /// </summary>
        /// <param name="options"></param>
        /// <returns>Returns void.</returns>
        public ZeronServerDbContext(DbContextOptions<ZeronServerDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Agents
        /// </summary>
        public DbSet<AgentEntity> Agents => Set<AgentEntity>();

        /// <summary>
        /// AgentHeartbeats
        /// </summary>
        public DbSet<AgentHeartbeatEntity> AgentHeartbeats => Set<AgentHeartbeatEntity>();

        /// <summary>
        /// Tasks
        /// </summary>
        public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

        /// <summary>
        /// TaskAssignments
        /// </summary>
        public DbSet<TaskAssignmentEntity> TaskAssignments => Set<TaskAssignmentEntity>();

        /// <summary>
        /// TaskResults
        /// </summary>
        public DbSet<TaskResultEntity> TaskResults => Set<TaskResultEntity>();

        /// <summary>
        /// Events
        /// </summary>
        public DbSet<EventEntity> Events => Set<EventEntity>();

        /// <summary>
        /// Users
        /// </summary>
        public DbSet<UserEntity> Users => Set<UserEntity>();

        /// <summary>
        /// Alerts
        /// </summary>
        public DbSet<AlertEntity> Alerts => Set<AlertEntity>();

        /// <summary>
        /// TaskSchedules
        /// </summary>
        public DbSet<TaskScheduleEntity> TaskSchedules => Set<TaskScheduleEntity>();

        /// <summary>
        /// ManagedPackages
        /// </summary>
        public DbSet<ManagedPackageEntity> ManagedPackages => Set<ManagedPackageEntity>();

        /// <summary>
        /// UserAgentBindings
        /// </summary>
        public DbSet<UserAgentBindingEntity> UserAgentBindings => Set<UserAgentBindingEntity>();

        /// <summary>
        /// AuditLogs
        /// </summary>
        public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

        /// <summary>
        /// OnModelCreating
        /// </summary>
        /// <param name="modelBuilder"></param>
        /// <returns>Returns void.</returns>
        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AgentEntity>(entity =>
            {
                entity.HasIndex(agent => agent.AgentKey).IsUnique();
                entity.HasIndex(agent => agent.Status);
            });

            modelBuilder.Entity<TaskAssignmentEntity>(entity =>
            {
                entity.HasIndex(assignment => assignment.Status);
                entity.HasOne(assignment => assignment.Result)
                    .WithOne(result => result!.Assignment)
                    .HasForeignKey<TaskResultEntity>(result => result.AssignmentId);
            });

            modelBuilder.Entity<EventEntity>(entity =>
            {
                entity.HasIndex(evt => evt.Topic);
                entity.HasIndex(evt => evt.ReceivedAt);
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.HasIndex(user => user.Username).IsUnique();
            });

            modelBuilder.Entity<AlertEntity>(entity =>
            {
                entity.HasIndex(alert => alert.Status);
                entity.HasIndex(alert => alert.RuleType);
                entity.HasIndex(alert => alert.AgentKey);
                entity.HasIndex(alert => alert.CreatedAt);
            });

            modelBuilder.Entity<TaskScheduleEntity>(entity =>
            {
                entity.HasIndex(schedule => schedule.Name).IsUnique();
                entity.HasIndex(schedule => schedule.Enabled);
                entity.HasIndex(schedule => schedule.NextRunAt);
            });

            modelBuilder.Entity<ManagedPackageEntity>(entity =>
            {
                entity.HasIndex(package => package.Name).IsUnique();
                entity.HasIndex(package => package.IsEnabled);
            });

            modelBuilder.Entity<UserAgentBindingEntity>(entity =>
            {
                entity.HasIndex(binding => new { binding.UserId, binding.AgentKey }).IsUnique();
                entity.HasIndex(binding => binding.AgentKey);
                entity.HasOne(binding => binding.User)
                    .WithMany()
                    .HasForeignKey(binding => binding.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLogEntity>(entity =>
            {
                entity.HasIndex(log => log.OccurredAt);
                entity.HasIndex(log => log.Action);
                entity.HasIndex(log => log.ActorUsername);
                entity.HasIndex(log => log.TargetKey);
                entity.HasIndex(log => log.Source);
            });
        }
    }
}
