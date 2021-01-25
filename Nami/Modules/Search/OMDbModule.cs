﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nami.Attributes;
using Nami.Exceptions;
using Nami.Extensions;
using Nami.Modules.Search.Common;
using Nami.Modules.Search.Services;
using Nami.Services.Common;

namespace Nami.Modules.Search
{
    [Group("imdb"), Module(ModuleType.Searches), NotBlocked]
    [Aliases("movies", "series", "serie", "movie", "film", "cinema", "omdb")]
    [Cooldown(3, 5, CooldownBucketType.Channel)]
    public sealed class OMDbModule : NamiServiceModule<OMDbService>
    {
        #region imdb
        [GroupCommand, Priority(0)]
        public Task ExecuteGroupAsync(CommandContext ctx,
                                     [RemainingText, Description("desc-query")] string title)
            => this.SearchByTitleAsync(ctx, title);
        #endregion

        #region imdb search
        [Command("search")]
        [Aliases("s", "find")]
        public async Task SearchAsync(CommandContext ctx,
                                     [RemainingText, Description("desc-query")] string query)
        {
            IReadOnlyList<MovieInfo>? res = await this.Service.SearchAsync(query);
            if (res is null || !res.Any()) {
                await ctx.FailAsync("cmd-err-res-none");
                return;
            }

            await ctx.PaginateAsync(res, (emb, r) => this.AddToEmbed(emb, r));
        }
        #endregion

        #region imdb title
        [Command("title")]
        [Aliases("t", "name", "n")]
        public Task SearchByTitleAsync(CommandContext ctx,
                                      [RemainingText, Description("desc-query")] string title)
            => this.SearchAndSendResultAsync(ctx, OMDbQueryType.Title, title);
        #endregion

        #region imdb id
        [Command("id")]
        public Task SearchByIdAsync(CommandContext ctx,
                                   [Description("desc-id")] string id)
            => this.SearchAndSendResultAsync(ctx, OMDbQueryType.Id, id);
        #endregion


        #region internals
        private async Task SearchAndSendResultAsync(CommandContext ctx, OMDbQueryType type, string query)
        {
            MovieInfo? info = await this.Service.SearchSingleAsync(type, query);
            if (info is null) {
                await ctx.FailAsync("cmd-err-res-none");
                return;
            }

            await ctx.RespondWithLocalizedEmbedAsync(emb => this.AddToEmbed(emb, info));
        }

        public LocalizedEmbedBuilder AddToEmbed(LocalizedEmbedBuilder emb, MovieInfo info)
        {
            emb.WithTitle(info.Title);
            emb.WithDescription(info.Plot);
            emb.WithColor(DiscordColor.Yellow);
            emb.WithUrl(this.Service.GetUrl(info.IMDbId));

            emb.AddLocalizedTitleField("str-type", info.Type, inline: true, unknown: false);
            emb.AddLocalizedTitleField("str-year", info.Year, inline: true, unknown: false);
            emb.AddLocalizedTitleField("str-id", info.IMDbId, inline: true, unknown: false);
            emb.AddLocalizedTitleField("str-genre", info.Genre, inline: true, unknown: false);
            emb.AddLocalizedTitleField("str-rel-date", info.ReleaseDate, inline: true, unknown: false);
            emb.AddLocalizedField("str-score", "fmt-rating-imdb", inline: true, contentArgs: new[] { info.IMDbRating, info.IMDbVotes });
            emb.AddLocalizedTitleField("str-rating", info.Rated, inline: true, unknown: false);
            emb.AddLocalizedTitleField("str-duration", info.Duration, inline: true, unknown: false);
            emb.AddLocalizedTitleField("str-writer", info.Writer, inline: true, unknown: false);
            emb.AddLocalizedTitleField("str-director", info.Director, inline: true, unknown: false);
            emb.AddLocalizedTitleField("str-actors", info.Actors, inline: true, unknown: false);
            if (!string.IsNullOrWhiteSpace(info.Poster) && info.Poster != "N/A")
                emb.WithThumbnail(info.Poster);

            emb.WithLocalizedFooter("fmt-powered-by", null, "OMDb");
            return emb;
        }
        #endregion
    }
}
