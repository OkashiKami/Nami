﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Nami.Common;
using Nami.Database.Models;
using Nami.Exceptions;
using Nami.Extensions;
using Nami.Modules.Games.Common;
using Nami.Modules.Games.Extensions;
using Nami.Modules.Games.Services;
using Nami.Services;

namespace Nami.Modules.Games
{
    public partial class GamesModule
    {
        [Group("tictactoe")]
        [Aliases("ttt")]
        [RequireGuild]
        public sealed class TicTacToeModule : NamiServiceModule<ChannelEventService>
        {
            #region game tictactoe
            [GroupCommand]
            public async Task ExecuteGroupAsync(CommandContext ctx,
                                               [Description("desc-game-movetime")] TimeSpan? moveTime = null)
            {
                if (moveTime?.TotalSeconds is < 2 or > 120)
                    throw new InvalidCommandUsageException(ctx, "cmd-err-game-movetime", 2, 120);

                if (this.Service.IsEventRunningInChannel(ctx.Channel.Id))
                    throw new CommandFailedException(ctx, "cmd-err-evt-dup");

                DiscordUser? opponent = await ctx.WaitForGameOpponentAsync();
                if (opponent is null)
                    throw new CommandFailedException(ctx, "cmd-err-game-op-none", ctx.User.Mention);

                var game = new TicTacToeGame(ctx.Client.GetInteractivity(), ctx.Channel, ctx.User, opponent, moveTime);
                this.Service.RegisterEventInChannel(game, ctx.Channel.Id);
                try {
                    await game.RunAsync(this.Localization);

                    if (game.Winner is { }) {
                        if (game.IsTimeoutReached)
                            await ctx.ImpInfoAsync(this.ModuleColor, Emojis.Trophy, "str-game-timeout", game.Winner.Mention);
                        else
                            await ctx.ImpInfoAsync(this.ModuleColor, Emojis.Trophy, "fmt-winners", game.Winner.Mention);

                        GameStatsService gss = ctx.Services.GetRequiredService<GameStatsService>();
                        await gss.UpdateStatsAsync(game.Winner.Id, s => s.TicTacToeWon++);
                        await gss.UpdateStatsAsync(game.Winner == ctx.User ? opponent.Id : ctx.User.Id, s => s.TicTacToeLost++);
                    } else {
                        await ctx.ImpInfoAsync(this.ModuleColor, Emojis.Joystick, "str-game-draw");
                    }

                } finally {
                    this.Service.UnregisterEventInChannel(ctx.Channel.Id);
                }
            }
            #endregion

            #region game tictactoe rules
            [Command("rules")]
            [Aliases("help", "h", "ruling", "rule")]
            public Task RulesAsync(CommandContext ctx)
                => ctx.ImpInfoAsync(this.ModuleColor, Emojis.Information, "str-game-ttt");
            #endregion

            #region game tictactoe stats
            [Command("stats"), Priority(1)]
            [Aliases("s")]
            public Task StatsAsync(CommandContext ctx,
                                  [Description("desc-member")] DiscordMember? member = null)
                => this.StatsAsync(ctx, member as DiscordUser);

            [Command("stats"), Priority(0)]
            public async Task StatsAsync(CommandContext ctx,
                                        [Description("desc-user")] DiscordUser? user = null)
            {
                user ??= ctx.User;
                GameStatsService gss = ctx.Services.GetRequiredService<GameStatsService>();

                GameStats? stats = await gss.GetAsync(user.Id);
                await ctx.RespondWithLocalizedEmbedAsync(emb => {
                    emb.WithLocalizedTitle("fmt-game-stats", user.ToDiscriminatorString());
                    emb.WithColor(this.ModuleColor);
                    emb.WithThumbnail(user.AvatarUrl);
                    if (stats is null)
                        emb.WithLocalizedDescription("str-game-stats-none");
                    else
                        emb.WithDescription(stats.BuildTicTacToeStatsString());
                });
            }
            #endregion

            #region game tictactoe top
            [Command("top")]
            [Aliases("t", "leaderboard")]
            public async Task TopAsync(CommandContext ctx)
            {
                GameStatsService gss = ctx.Services.GetRequiredService<GameStatsService>();
                IReadOnlyList<GameStats> topStats = await gss.GetTopTicTacToeStatsAsync();
                string top = await GameStatsExtensions.BuildStatsStringAsync(ctx.Client, topStats, s => s.BuildTicTacToeStatsString());
                await ctx.ImpInfoAsync(this.ModuleColor, Emojis.Trophy, "fmt-game-ttt-top", top);
            }
            #endregion
        }
    }
}
