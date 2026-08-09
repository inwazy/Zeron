// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server.Data.Entities
{
    /// <summary>
    /// UserAgentBindingEntity - binds a dashboard user to a Demand agent.
    /// </summary>
    public class UserAgentBindingEntity
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// UserId
        /// </summary>
        public Guid UserId
        {
            get;
            set;
        }

        /// <summary>
        /// AgentKey - maps to AgentEntity.AgentKey / Demand AgentId.
        /// </summary>
        public string AgentKey
        {
            get;
            set;
        } = "";

        /// <summary>
        /// BoundAt
        /// </summary>
        public DateTime BoundAt
        {
            get;
            set;
        }

        /// <summary>
        /// User
        /// </summary>
        public UserEntity? User
        {
            get;
            set;
        }
    }
}
