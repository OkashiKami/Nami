﻿using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Humanizer;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamStore;
using Nami.Attributes;
using Nami.Exceptions;
using Nami.Extensions;
using Nami.Modules.Search.Services;

namespace Nami.Modules.Search
{
    [Group("steam"), Module(ModuleType.Searches), NotBlocked]
    [Aliases("s", "st")]
    [Cooldown(3, 5, CooldownBucketType.Channel)]
    public sealed class SteamModule : NamiServiceModule<SteamService>
    {
        #region steam
        [GroupCommand, Priority(1)]
        public Task ExecuteGroupAsync(CommandContext ctx,
                                     [Description("desc-id")] ulong id)
            => this.InfoAsync(ctx, id);

        [GroupCommand, Priority(0)]
        public Task ExecuteGroupAsync(CommandContext ctx,
                                     [RemainingText, Description("desc-username")] string username)
            => this.InfoAsync(ctx, username);
        #endregion

        #region steam profile
        [Command("profile"), Priority(1)]
        [Aliases("id", "user", "info")]
        public async Task InfoAsync(CommandContext ctx,
                                   [Description("desc-id")] ulong id) 
            => await this.PrintProfileAsync(ctx, await this.Service.GetInfoAsync(id));

        [Command("profile"), Priority(0)]
        public async Task InfoAsync(CommandContext ctx,
                                   [RemainingText, Description("desc-username")] string username) 
            => await this.PrintProfileAsync(ctx, await this.Service.GetInfoAsync(username));
        #endregion

        #region steam game
        [Command("game"), Priority(1)]
        [Aliases("g", "gm", "store")]
        public async Task GameAsync(CommandContext ctx,
                                   [Description("desc-id")] uint id) 
            => await this.PrintGameAsync(ctx, await this.Service.GetStoreInfoAsync(id));

        [Command("game"), Priority(0)]
        public async Task GameAsync(CommandContext ctx,
                                   [RemainingText, Description("desc-gamename")] string game)
        {
            uint? id = await this.Service.GetAppIdAsync(game);
            if (id is null)
                throw new CommandFailedException(ctx, "cmd-err-steam-game");

            await this.PrintGameAsync(ctx, await this.Service.GetStoreInfoAsync(id.Value));
        }
        #endregion


        #region internals
        private Task PrintProfileAsync(CommandContext ctx, (SteamCommunityProfileModel, PlayerSummaryModel)? res)
        {
            if (res is null)
                throw new CommandFailedException(ctx, "cmd-err-steam-user");

            (SteamCommunityProfileModel model, PlayerSummaryModel summary) = res.Value;
            return ctx.RespondWithLocalizedEmbedAsync(async emb => {
                emb.WithTitle(summary.Nickname);
                emb.WithDescription(model.Summary);
                emb.WithColor(this.ModuleColor);
                emb.WithThumbnail(model.AvatarMedium.ToString());
                emb.WithUrl(this.Service.GetCommunityProfileUrl(model.SteamID));

                if (summary.ProfileVisibility != ProfileVisibility.Public) {
                    emb.WithLocalizedDescription("str-profile-private");
                    return;
                }

                emb.AddLocalizedTimestampField("str-member-since", summary.AccountCreatedDate, inline: true);

                if (summary.UserStatus != UserStatus.Offline)
                    emb.AddLocalizedTitleField("str-status", summary.UserStatus.Humanize(LetterCasing.Sentence), inline: true);
                else if (summary.LastLoggedOffDate.Year > 1000)
                    emb.AddLocalizedTimestampField("str-last-seen", summary.LastLoggedOffDate, inline: true);

                emb.AddLocalizedTitleField("str-id", model.SteamID, inline: true);
                emb.AddLocalizedTitleField("str-playing", summary.PlayingGameName, inline: true, unknown: false);
                emb.AddLocalizedTitleField("str-location", model.Location, inline: true, unknown: false);
                emb.AddLocalizedTitleField("str-real-name", model.RealName, inline: true, unknown: false);
                emb.AddLocalizedTitleField("str-rating", model.SteamRating, inline: true, unknown: false);
                emb.AddLocalizedTitleField("str-headline", model.Headline, unknown: false);

                // TODO
                // emb.AddField("Game activity", $"{model.HoursPlayedLastTwoWeeks} hours past 2 weeks.", inline: true);

                if (model.IsVacBanned) {
                    int? bans = await this.Service.GetVacBanCountAsync(model.SteamID);
                    if (bans is { })
                        emb.AddLocalizedField("str-vac", "fmt-vac", contentArgs: new object[] { bans });
                    else
                        emb.AddLocalizedField("str-vac", "str-vac-ban", inline: true);
                } else {
                    emb.AddLocalizedField("str-vac", "str-vac-clean", inline: true);
                }

                if (model.MostPlayedGames.Any())
                    emb.AddLocalizedTitleField("str-most-played", model.MostPlayedGames.Take(5).Select(g => g.Name).JoinWith(", "));

                emb.AddLocalizedTitleField("str-trade-ban", model.TradeBanState, inline: true, unknown: false);
            });
        }

        private Task PrintGameAsync(CommandContext ctx, StoreAppDetailsDataModel? res)
        {
            if (res is null)
                throw new CommandFailedException(ctx, "cmd-err-steam-game");

            return ctx.RespondWithLocalizedEmbedAsync(emb => {
                emb.WithTitle(res.Name);
                emb.WithDescription(res.ShortDescription);
                emb.WithUrl(this.Service.GetGameStoreUrl(res.SteamAppId));
                emb.WithThumbnail(res.HeaderImage);
                emb.AddLocalizedTitleField("str-metacritic", res.Metacritic?.Score, inline: true, unknown: false);
                emb.AddLocalizedTitleField("str-price", res.PriceOverview?.FinalFormatted, inline: true, unknown: false);
                emb.AddLocalizedTitleField("str-release-date", res.ReleaseDate?.Date, inline: true, unknown: false);
                emb.AddLocalizedTitleField("str-devs", res.Developers.JoinWith(", "), inline: true);
                emb.AddLocalizedTitleField("str-genres", res.Genres.Select(g => g.Description).JoinWith(", "));
                emb.WithFooter(res.Website, null);
            });
        }
        #endregion
    }
}
