﻿using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Humanizer;
using Nami.Attributes;
using Nami.Extensions;
using Nami.Modules.Search.Common;
using Nami.Modules.Search.Services;

namespace Nami.Modules.Search
{
    [Group("urbandict"), Module(ModuleType.Searches), NotBlocked]
    [Aliases("ud", "urban", "urbandictionary")]
    [Cooldown(3, 5, CooldownBucketType.Channel)]
    public class UrbanDictModule : NamiModule
    {
        #region urbandict
        [GroupCommand]
        public async Task ExecuteGroupAsync(CommandContext ctx,
                                           [RemainingText, Description("desc-query")] string query)
        {
            UrbanDictData? data = await UrbanDictService.GetDefinitionForTermAsync(query);
            if (data is null) {
                await ctx.FailAsync("cmd-err-res-none");
                return;
            }

            await ctx.PaginateAsync(
                "fmt-ud",
                data.List,
                res => {
                    var sb = new StringBuilder(this.Localization.GetString(ctx.Guild?.Id, "str-def-by"));
                    sb.Append(Formatter.Bold(res.Author)).AppendLine().AppendLine();
                    sb.Append(Formatter.Bold(res.Word)).Append(" :");
                    sb.AppendLine(Formatter.BlockCode(res.Definition.Trim().Truncate(1000)));
                    if (!string.IsNullOrWhiteSpace(res.Example))
                        sb.Append(this.Localization.GetString(ctx.Guild?.Id, "str-examples")).AppendLine(Formatter.BlockCode(res.Example.Trim().Truncate(250)));
                    sb.Append(res.Permalink);
                    return sb.ToString();
                },
                this.ModuleColor,
                1,
                query
            );
        }
        #endregion
    }
}