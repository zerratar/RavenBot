﻿using TwitchLib.Client.Models;

namespace TwitchLib.Client.Events
{
    /// <summary>
    /// Args representing a user was banned event.
    /// Implements the <see cref="System.EventArgs" />
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    /// <inheritdoc />
    public class OnUserBannedArgs : EventArgs
    {
        /// <summary>
        /// The user ban
        /// </summary>
        public UserBan UserBan { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OnUserBannedArgs"/> class.
        /// </summary>
        public OnUserBannedArgs(UserBan userBan)
        {
            UserBan = userBan;
        }
    }
}
