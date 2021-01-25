﻿using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Nami.Attributes;
using Nami.Exceptions;

namespace Nami.Modules.Music
{
    [Group("voice"), Module(ModuleType.Music), Hidden]
    [Aliases("v")]
    [RequireGuild, RequirePrivilegedUser]
    [Cooldown(3, 5, CooldownBucketType.Guild)]
    public sealed class VoiceModule : NamiModule
    {
        #region voice connect
        [Command("connect")]
        [Aliases("c", "con", "conn")]
        public Task ConnectAsync(CommandContext ctx,
                                [RemainingText, Description("desc-chn-voice")] DiscordChannel? channel = null)
        {
            channel ??= ctx.Member.VoiceState?.Channel;
            if (channel is null)
                throw new CommandFailedException(ctx, "cmd-err-music-vc");

            if (channel.Type != ChannelType.Voice)
                throw new CommandFailedException(ctx, "cmd-err-chn-type-voice");

            if (!channel.PermissionsFor(ctx.Guild.CurrentMember).HasPermission(Permissions.AccessChannels))
                throw new ChecksFailedException(ctx.Command, ctx, new[] { new RequireBotPermissionsAttribute(Permissions.AccessChannels) });

            return ctx.Client.GetVoiceNext().ConnectAsync(channel);
        }
        #endregion

        #region voice disconnect
        [Command("disconnect")]
        [Aliases("d", "disconn", "dc")]
        public Task DisonnectAsync(CommandContext ctx)
        {
            ctx.Client.GetVoiceNext().GetConnection(ctx.Guild)?.Disconnect();
            return Task.CompletedTask;
        }
        #endregion
    }
}
