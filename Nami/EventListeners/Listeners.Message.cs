﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Nami.Common;
using Nami.Database;
using Nami.Database.Models;
using Nami.EventListeners.Attributes;
using Nami.EventListeners.Common;
using Nami.Extensions;
using Nami.Misc.Services;
using Nami.Modules.Administration.Extensions;
using Nami.Modules.Administration.Services;
using Nami.Modules.Owner.Services;
using Nami.Modules.Reactions.Services;
using Nami.Services;
using Nami.Services.Common;

namespace Nami.EventListeners
{
    internal static partial class Listeners
    {
        [AsyncEventListener(DiscordEventType.MessagesBulkDeleted)]
        public static async Task BulkDeleteEventHandlerAsync(NamiBot bot, MessageBulkDeleteEventArgs e)
        {
            if (e.Guild is null)
                return;

            if (!LoggingService.IsLogEnabledForGuild(bot, e.Guild.Id, out LoggingService logService, out LocalizedEmbedBuilder emb))
                return;

            if (LoggingService.IsChannelExempted(bot, e.Guild, e.Channel, out GuildConfigService gcs))
                return;

            emb.WithLocalizedTitle(DiscordEventType.MessagesBulkDeleted, "evt-msg-del-bulk", e.Channel);
            emb.AddLocalizedTitleField("str-count", e.Messages.Count, inline: true);
            using var ms = new MemoryStream();
            using var sw = new StreamWriter(ms);
            foreach (DiscordMessage msg in e.Messages) {
                sw.WriteLine($"[{msg.Timestamp}] {msg.Author}");
                sw.WriteLine(string.IsNullOrWhiteSpace(msg.Content) ? "?" : msg.Content);
                sw.WriteLine(msg.Attachments.Select(a => $"{a.FileName} ({a.FileSize})").JoinWith(", "));
                sw.Flush();
            }
            ms.Seek(0, SeekOrigin.Begin);
            DiscordChannel? chn = gcs.GetLogChannelForGuild(e.Guild);

            var builder = new DiscordMessageBuilder();
            builder.WithFile($"{e.Channel.Name}-deleted-messages.txt", ms);
            builder.WithEmbed(emb.Build());
            await (chn?.SendMessageAsync(builder) ?? Task.CompletedTask);
        }

        [AsyncEventListener(DiscordEventType.MessageCreated)]
        public static async Task MessageCreateEventHandlerAsync(NamiBot bot, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot)
                return;

            if (e.Guild is null) {
                LogExt.Debug(bot.GetId(null), new[] { "DM message received from {User}:", "{Message}" }, e.Author, e.Message);
                return;
            }

            if (bot.Services.GetRequiredService<BlockingService>().IsBlocked(e.Guild.Id, e.Channel.Id, e.Author.Id))
                return;

            if (string.IsNullOrWhiteSpace(e.Message?.Content))
                return;

            if (!e.Message.Content.StartsWith(bot.Services.GetRequiredService<GuildConfigService>().GetGuildPrefix(e.Guild.Id))) {
                short rank = bot.Services.GetRequiredService<UserRanksService>().ChangeXp(e.Guild.Id, e.Author.Id);
                if (rank != 0) {
                    LocalizationService ls = bot.Services.GetRequiredService<LocalizationService>();
                    LevelRole? lr = await bot.Services.GetRequiredService<LevelRoleService>().GetAsync(e.Guild.Id, rank);
                    DiscordRole? levelRole = lr is { } ? e.Guild.GetRole(lr.RoleId) : null;
                    XpRank? rankInfo = await bot.Services.GetRequiredService<GuildRanksService>().GetAsync(e.Guild.Id, rank);
                    string rankupStr;
                    if (levelRole is { }) {
                        rankupStr = ls.GetString(e.Guild.Id, "fmt-rankup-lr",
                            e.Author.Mention, Formatter.Bold(rank.ToString()), rankInfo?.Name ?? "/", levelRole.Mention
                        );
                        DiscordMember member = await e.Guild.GetMemberAsync(e.Author.Id);
                        await member.GrantRoleAsync(levelRole);
                    } else {
                        rankupStr = ls.GetString(e.Guild.Id, "fmt-rankup", e.Author.Mention, Formatter.Bold(rank.ToString()), rankInfo?.Name ?? "/");
                    }
                    await e.Channel.EmbedAsync(rankupStr, Emojis.Medal);
                }
            }
        }

        [AsyncEventListener(DiscordEventType.MessageCreated)]
        public static async Task MessageCreateProtectionHandlerAsync(NamiBot bot, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Guild is null || string.IsNullOrWhiteSpace(e.Message?.Content))
                return;

            if (bot.Services.GetRequiredService<BlockingService>().IsChannelBlocked(e.Channel.Id))
                return;

            CachedGuildConfig? gcfg = bot.Services.GetRequiredService<GuildConfigService>().GetCachedConfig(e.Guild.Id);
            if (gcfg is { }) {
                if (gcfg.RatelimitSettings.Enabled)
                    await bot.Services.GetRequiredService<RatelimitService>().HandleNewMessageAsync(e, gcfg.RatelimitSettings);
                if (gcfg.AntispamSettings.Enabled)
                    await bot.Services.GetRequiredService<AntispamService>().HandleNewMessageAsync(e, gcfg.AntispamSettings);
                if (gcfg.AntiMentionSettings.Enabled)
                    await bot.Services.GetRequiredService<AntiMentionService>().HandleNewMessageAsync(e, gcfg.AntiMentionSettings);
            }
        }

        [AsyncEventListener(DiscordEventType.MessageCreated)]
        public static Task MessageCreateBackupHandlerAsync(NamiBot bot, MessageCreateEventArgs e)
        {
            return e.Guild is null
                ? Task.CompletedTask
                : bot.Services.GetRequiredService<BackupService>().BackupAsync(e.Message);
        }

        [AsyncEventListener(DiscordEventType.MessageCreated)]
        public static async Task MessageFilterEventHandlerAsync(NamiBot bot, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Guild is null || string.IsNullOrWhiteSpace(e.Message?.Content))
                return;

            if (bot.Services.GetRequiredService<BlockingService>().IsChannelBlocked(e.Channel.Id))
                return;

            CachedGuildConfig? gcfg = bot.Services.GetRequiredService<GuildConfigService>().GetCachedConfig(e.Guild.Id);
            if (gcfg?.LinkfilterSettings.Enabled ?? false) {
                if (await bot.Services.GetRequiredService<LinkfilterService>().HandleNewMessageAsync(e, gcfg.LinkfilterSettings))
                    return;
            }

            if (!bot.Services.GetRequiredService<FilteringService>().TextContainsFilter(e.Guild.Id, e.Message.Content, out _))
                return;

            if (!e.Channel.PermissionsFor(e.Guild.CurrentMember).HasFlag(Permissions.ManageMessages))
                return;


            // TODO automatize, same below in message update handler
            LocalizationService ls = bot.Services.GetRequiredService<LocalizationService>();
            try {
                await e.Message.DeleteAsync(ls.GetString(e.Guild.Id, "rsn-filter-match"));
                await e.Channel.LocalizedEmbedAsync(ls, "fmt-filter", e.Author.Mention, Formatter.Strip(e.Message.Content));
            } catch {
                // TODO
            }
        }

        [AsyncEventListener(DiscordEventType.MessageCreated)]
        public static async Task MessageReactionEventHandlerAsync(NamiBot bot, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Guild is null || string.IsNullOrWhiteSpace(e.Message?.Content))
                return;

            if (bot.Services.GetRequiredService<BlockingService>().IsBlocked(e.Guild.Id, e.Channel.Id, e.Author.Id))
                return;

            ReactionsService rs = bot.Services.GetRequiredService<ReactionsService>();

            if (e.Channel.PermissionsFor(e.Guild.CurrentMember).HasFlag(Permissions.AddReactions)) {
                EmojiReaction? er = rs.FindMatchingEmojiReactions(e.Guild.Id, e.Message.Content)
                    .Shuffle()
                    .FirstOrDefault();

                // TODO move to service
                if (er is { }) {
                    try {
                        DiscordClient client = bot.Client.GetShard(e.Guild.Id);
                        var emoji = DiscordEmoji.FromName(client, er.Response);
                        await e.Message.CreateReactionAsync(emoji);
                    } catch (ArgumentException) {
                        using NamiDbContext db = bot.Database.CreateContext();
                        db.EmojiReactions.RemoveRange(
                            db.EmojiReactions
                                .Where(r => r.GuildIdDb == (long)e.Guild.Id)
                                .AsEnumerable()
                                .Where(r => r.HasSameResponseAs(er))
                        );
                        await db.SaveChangesAsync();
                    } catch (NotFoundException) {
                        LogExt.Debug(bot.GetId(e.Guild.Id), "Trying to react to a deleted message.");
                    }
                }
            }

            TextReaction? tr = rs.FindMatchingTextReaction(e.Guild.Id, e.Message.Content);
            // TODO move to service
            if (tr is { } && tr.CanSend())
                await e.Channel.SendMessageAsync(tr.Response.Replace("%user%", e.Author.Mention));
        }

        [AsyncEventListener(DiscordEventType.MessageDeleted)]
        public static async Task MessageDeleteEventHandlerAsync(NamiBot bot, MessageDeleteEventArgs e)
        {
            if (e.Guild is null || e.Message is null)
                return;

            if (!LoggingService.IsLogEnabledForGuild(bot, e.Guild.Id, out LoggingService logService, out LocalizedEmbedBuilder emb))
                return;

            if (LoggingService.IsChannelExempted(bot, e.Guild, e.Channel, out GuildConfigService gcs))
                return;

            if (e.Message.Author == bot.Client.CurrentUser && bot.Services.GetRequiredService<ChannelEventService>().IsEventRunningInChannel(e.Channel.Id))
                return;

            emb.WithLocalizedTitle(DiscordEventType.MessageDeleted, "evt-msg-del");
            emb.AddLocalizedTitleField("str-chn", e.Channel.Mention, inline: true);
            emb.AddLocalizedTitleField("str-author", e.Message.Author?.Mention, inline: true);

            DiscordAuditLogMessageEntry? entry = await e.Guild.GetLatestAuditLogEntryAsync<DiscordAuditLogMessageEntry>(AuditLogActionType.MessageDelete);
            if (entry is { }) {
                DiscordMember? member = await e.Guild.GetMemberAsync(entry.UserResponsible.Id);
                if (member is { } && gcs.IsMemberExempted(e.Guild.Id, member.Id, member.Roles.Select(r => r.Id)))
                    return;
                emb.AddFieldsFromAuditLogEntry(entry);
            }

            if (!string.IsNullOrWhiteSpace(e.Message.Content)) {
                string sanitizedContent = Formatter.BlockCode(Formatter.Strip(e.Message.Content.Truncate(1000)));
                emb.AddLocalizedTitleField("str-content", sanitizedContent);
                if (bot.Services.GetRequiredService<FilteringService>().TextContainsFilter(e.Guild.Id, e.Message.Content, out _)) {
                    LocalizationService ls = bot.Services.GetRequiredService<LocalizationService>();
                    emb.WithDescription(Formatter.Italic(ls.GetString(e.Guild.Id, "rsn-filter-match")));
                }
            }
            if (e.Message.Embeds.Any())
                emb.AddLocalizedTitleField("str-embeds", e.Message.Embeds.Count, inline: true);
            if (e.Message.Reactions.Any())
                emb.AddLocalizedTitleField("str-reactions", e.Message.Reactions.Select(r => r.Emoji.GetDiscordName()).JoinWith(" "), inline: true);
            if (e.Message.Attachments.Any()) {
                emb.AddLocalizedTitleField("str-attachments", e.Message.Attachments.Select(a => ToMaskedUrl(a)).JoinWith(), inline: true);

                static string ToMaskedUrl(DiscordAttachment a)
                    => Formatter.MaskedUrl($"{a.FileName} ({a.FileSize.ToMetric(decimals: 0)}B)", new Uri(a.Url));
            }
            if (e.Message.CreationTimestamp is { })
                emb.AddLocalizedTimestampField("str-created-at", e.Message.CreationTimestamp, inline: true);

            await logService.LogAsync(e.Channel.Guild, emb);
        }

        [AsyncEventListener(DiscordEventType.MessageUpdated)]
        public static async Task MessageUpdateEventHandlerAsync(NamiBot bot, MessageUpdateEventArgs e)
        {
            if (e.Guild is null || (e.Author?.IsBot ?? false) || e.Channel is null || e.Message is null || e.Author is null)
                return;

            if (bot.Services.GetRequiredService<BlockingService>().IsChannelBlocked(e.Channel.Id))
                return;

            if (e.Message.Author == bot.Client.CurrentUser && bot.Services.GetRequiredService<ChannelEventService>().IsEventRunningInChannel(e.Channel.Id))
                return;

            if (e.MessageBefore?.Embeds?.Count < e.Message.Embeds?.Count)
                return;     // Discord added embed(s)

            LocalizationService ls = bot.Services.GetRequiredService<LocalizationService>();
            FilteringService fs = bot.Services.GetRequiredService<FilteringService>();
            if (!string.IsNullOrWhiteSpace(e.Message.Content) && fs.TextContainsFilter(e.Guild.Id, e.Message.Content, out _)) {
                try {
                    await e.Message.DeleteAsync(ls.GetString(e.Guild.Id, "rsn-filter-match"));
                    await e.Channel.LocalizedEmbedAsync(ls, "fmt-filter", e.Author.Mention, Formatter.Strip(e.Message.Content));
                } catch {
                    // TODO
                }
            }

            if (!LoggingService.IsLogEnabledForGuild(bot, e.Guild.Id, out LoggingService logService, out LocalizedEmbedBuilder emb))
                return;

            if (LoggingService.IsChannelExempted(bot, e.Guild, e.Channel, out GuildConfigService gcs))
                return;

            DiscordMember member = await e.Guild.GetMemberAsync(e.Author.Id);
            if (member is { } && gcs.IsMemberExempted(e.Guild.Id, member.Id, member.Roles.Select(r => r.Id)))
                return;

            string jumplink = Formatter.MaskedUrl(ls.GetString(e.Guild.Id, "str-jumplink"), e.Message.JumpLink);
            emb.WithLocalizedTitle(DiscordEventType.MessageUpdated, "evt-msg-upd", desc: jumplink);
            emb.AddLocalizedTitleField("str-location", e.Channel.Mention, inline: true);
            emb.AddLocalizedTitleField("str-author", e.Message.Author?.Mention, inline: true);

            emb.AddLocalizedContentField(
                "str-upd-bef",
                "fmt-msg-cre",
                inline: false,
                ls.GetLocalizedTimeString(e.Guild.Id, e.Message.CreationTimestamp, unknown: true),
                e.MessageBefore?.Embeds?.Count ?? 0,
                e.MessageBefore?.Reactions?.Count ?? 0,
                e.MessageBefore?.Attachments?.Count ?? 0,
                FormatContent(e.MessageBefore)
            );
            emb.AddLocalizedContentField(
                "str-upd-aft",
                "fmt-msg-upd",
                inline: true,
                ls.GetLocalizedTimeString(e.Guild.Id, e.Message.EditedTimestamp, unknown: true),
                e.Message.Embeds?.Count ?? 0,
                e.Message.Reactions?.Count ?? 0,
                e.Message.Attachments?.Count ?? 0,
                FormatContent(e.Message)
            );

            await logService.LogAsync(e.Channel.Guild, emb);


            static string? FormatContent(DiscordMessage? msg)
                => string.IsNullOrWhiteSpace(msg?.Content) ? null : Formatter.BlockCode(msg.Content.Truncate(700));
        }
    }
}
