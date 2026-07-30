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
        /// OnModelCreating
        /// </summary>
        /// <param name="modelBuilder"></param>
        /// <returns>Returns void.</returns>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
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
        }
    }
}
