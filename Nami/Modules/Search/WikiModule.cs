﻿using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nami.Attributes;
using Nami.Exceptions;
using Nami.Extensions;
using Nami.Modules.Search.Common;
using Nami.Modules.Search.Services;

namespace Nami.Modules.Search
{
    [Group("wikipedia"), Module(ModuleType.Searches), NotBlocked]
    [Aliases("wiki")]
    [Cooldown(3, 5, CooldownBucketType.Channel)]
    public sealed class WikiModule : NamiModule
    {
        #region wikipedia
        [GroupCommand]
        public Task ExecuteGroupAsync(CommandContext ctx,
                                     [RemainingText, Description("desc-query")] string query)
            => this.SearchAsync(ctx, query);
        #endregion

        #region wiki search
        [Command("search")]
        [Aliases("s", "find")]
        public async Task SearchAsync(CommandContext ctx,
                                     [RemainingText, Description("desc-query")] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new InvalidCommandUsageException(ctx, "cmd-err-query");

            WikiSearchResponse? res = await WikiService.SearchAsync(query);
            if (res is null || !res.Any()) {
                await ctx.FailAsync("cmd-err-res-none");
                return;
            }

            await ctx.PaginateAsync(res, (emb, r) => {
                emb.WithTitle(r.Title);
                emb.WithDescription(r.Snippet);
                emb.WithUrl(r.Url);
                emb.WithLocalizedFooter("fmt-powered-by", WikiService.WikipediaIconUrl, "Wikipedia API");
                return emb;
            }, this.ModuleColor);
        }
        #endregion
    }
}
