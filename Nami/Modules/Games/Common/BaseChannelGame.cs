﻿using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Nami.Common;
using Nami.Services;

namespace Nami.Modules.Games.Common
{
    public abstract class BaseChannelGame : IChannelEvent
    {
        public DiscordChannel Channel { get; protected set; }

        public InteractivityExtension Interactivity { get; protected set; }

        public DiscordUser? Winner { get; protected set; }

        public bool IsTimeoutReached { get; protected set; }


        protected BaseChannelGame(InteractivityExtension interactivity, DiscordChannel channel)
        {
            this.Interactivity = interactivity;
            this.Channel = channel;
        }


        public abstract Task RunAsync(LocalizationService lcs);
    }
}
