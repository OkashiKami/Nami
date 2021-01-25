﻿using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Nami.Common;
using Nami.Extensions;
using Nami.Modules.Administration.Services;
using Nami.Modules.Currency.Common;
using Nami.Services;

namespace Nami.Modules.Currency.Extensions
{
    public static class SlotMachineRollExtensions
    {
        private static ImmutableArray<DiscordEmoji> _emoji = new DiscordEmoji[] {
            Emojis.LargeBlueDiamond,
            Emojis.Seven,
            Emojis.MoneyBag,
            Emojis.Trophy,
            Emojis.Gift,
            Emojis.Cherries
        }.ToImmutableArray();


        public static Task SendAsync(this SlotMachineRoll roll, CommandContext ctx, DiscordColor color)
        {
            return ctx.RespondWithLocalizedEmbedAsync(emb => {
                emb.WithLocalizedTitle("fmt-casino-slot", Emojis.LargeOrangeDiamond, Emojis.LargeOrangeDiamond);
                emb.WithColor(color);
                emb.WithDescription(roll.ToEmojiString());
                emb.WithThumbnail(ctx.User.AvatarUrl);

                var sb = new StringBuilder();
                foreach ((DiscordEmoji e, int m) in _emoji.Zip(SlotMachineRoll.Multipliers))
                    sb.Append(e).Append(Formatter.InlineCode($" x{m} "));

                emb.AddLocalizedTitleField("str-multipliers", sb);

                string currency = ctx.Services.GetRequiredService<GuildConfigService>().GetCachedConfig(ctx.Guild.Id).Currency;
                CultureInfo culture = ctx.Services.GetRequiredService<LocalizationService>().GetGuildCulture(ctx.Guild.Id);
                emb.AddLocalizedField("str-result", "fmt-casino-win", 
                    contentArgs: new object[] { ctx.User.Mention, roll.WonAmount.ToWords(culture), roll.WonAmount, currency }
                );
            });
        }

        public static string ToEmojiString(this SlotMachineRoll roll)
        {
            var sb = new StringBuilder();

            sb.Append(Emojis.BlackSquare);
            for (int i = 0; i < 5; i++)
                sb.Append(Emojis.SmallOrangeDiamond);
            sb.AppendLine();

            for (int i = 0; i < 3; i++) {
                if (i % 2 != 0)
                    sb.Append(Emojis.Joystick);
                else
                    sb.Append(Emojis.BlackSquare);
                sb.Append(Emojis.SmallOrangeDiamond);
                for (int j = 0; j < 3; j++)
                    sb.Append(_emoji[roll.Result[i, j]]);
                sb.AppendLine(Emojis.SmallOrangeDiamond);
            }

            sb.Append(Emojis.BlackSquare);
            for (int i = 0; i < 5; i++)
                sb.Append(Emojis.SmallOrangeDiamond);

            return sb.ToString();
        }
    }
}
