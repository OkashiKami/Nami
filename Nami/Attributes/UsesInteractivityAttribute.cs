﻿using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Nami.Services;

namespace Nami.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class UsesInteractivityAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            InteractivityService iService = ctx.Services.GetRequiredService<InteractivityService>();
            return Task.FromResult(!iService.IsResponsePending(ctx.Channel.Id, ctx.User.Id));
        }
    }
}
