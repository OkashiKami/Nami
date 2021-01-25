﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Nami.Attributes;
using Nami.Common;
using Nami.Database.Models;
using Nami.Exceptions;
using Nami.Extensions;
using Nami.Modules.Administration.Services;
using Nami.Modules.Chickens.Common;
using Nami.Modules.Chickens.Services;
using Nami.Modules.Currency.Services;
using Nami.Services;
using Nami.Services.Common;

namespace Nami.Modules.Chickens
{
    public partial class ChickenModule
    {
        [Group("upgrade"), UsesInteractivity]
        [Aliases("perks", "upgrades", "upg", "u")]
        public sealed class UpgradeModule : NamiServiceModule<ChickenUpgradeService>
        {
            #region chicken upgrade
            [GroupCommand, Priority(1)]
            public Task ExecuteGroupAsync(CommandContext ctx)
                => this.ListAsync(ctx);

            [GroupCommand, Priority(0)]
            public async Task ExecuteGroupAsync(CommandContext ctx,
                                               [Description("desc-chicken-upgrade-ids")] params int[] ids)
            {
                if (ids is null || !ids.Any())
                    throw new CommandFailedException(ctx, "cmd-err-chicken-upg-ids-none");

                if (ctx.Services.GetRequiredService<ChannelEventService>().IsEventRunningInChannel(ctx.Channel.Id, out ChickenWar _))
                    throw new CommandFailedException(ctx, "cmd-err-chicken-war");

                Chicken? chicken = await ctx.Services.GetRequiredService<ChickenService>().GetCompleteAsync(ctx.Guild.Id, ctx.User.Id);
                if (chicken is null)
                    throw new CommandFailedException(ctx, "cmd-err-chicken-none");
                chicken.Owner = ctx.User;

                if (chicken.Stats.Upgrades?.Any(u => ids.Contains(u.Id)) ?? false)
                    throw new CommandFailedException(ctx, "cmd-err-chicken-upg-dup");

                IReadOnlyList<ChickenUpgrade> upgrades = await this.Service.GetAsync();
                var toBuy = upgrades.Where(u => ids.Contains(u.Id)).ToList();

                CachedGuildConfig gcfg = ctx.Services.GetRequiredService<GuildConfigService>().GetCachedConfig(ctx.Guild.Id);
                long totalCost = toBuy.Sum(u => u.Cost);
                string upgradeNames = toBuy.Select(u => u.Name).JoinWith(", ");
                if (!await ctx.WaitForBoolReplyAsync("q-chicken-upg", args: new object[] { ctx.User.Mention, totalCost, gcfg.Currency, upgradeNames }))
                    return;

                if (!await ctx.Services.GetRequiredService<BankAccountService>().TryDecreaseBankAccountAsync(ctx.Guild.Id, ctx.User.Id, totalCost))
                    throw new CommandFailedException(ctx, "cmd-err-funds", gcfg.Currency, totalCost);

                await ctx.Services.GetRequiredService<ChickenBoughtUpgradeService>().AddAsync(
                    toBuy.Select(u => new ChickenBoughtUpgrade {
                        Id = u.Id,
                        GuildId = chicken.GuildId,
                        UserId = chicken.UserId
                    })
                );

                int addedStr = toBuy.Where(u => u.UpgradesStat == ChickenStatUpgrade.Str).Sum(u => u.Modifier);
                int addedVit = toBuy.Where(u => u.UpgradesStat == ChickenStatUpgrade.MaxVit).Sum(u => u.Modifier);
                await ctx.ImpInfoAsync(this.ModuleColor, Emojis.Chicken, "fmt-chicken-upg", ctx.User.Mention, chicken.Name, toBuy.Count, addedStr, addedVit);
            }
            #endregion

            #region chicken upgrade list
            [Command("list")]
            [Aliases("print", "show", "view", "ls", "l", "p")]
            public async Task ListAsync(CommandContext ctx)
            {
                IReadOnlyList<ChickenUpgrade> upgrades = await this.Service.GetAsync();
                if (!upgrades.Any())
                    throw new CommandFailedException(ctx, "cmd-err-res-none");

                await ctx.PaginateAsync(upgrades.OrderBy(u => u.Cost), (emb, u) => {
                    emb.WithTitle(u.Name);
                    emb.AddLocalizedTitleField("str-id", u.Id, inline: true);
                    emb.AddLocalizedTitleField("str-cost", $"{u.Cost:n0}", inline: true);
                    emb.AddLocalizedTitleField("str-cost", $"+{u.Modifier}{u.UpgradesStat.Humanize(LetterCasing.AllCaps)}", inline: true);
                    return emb;
                }, this.ModuleColor);
            }
            #endregion
        }
    }
}
