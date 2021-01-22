﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using GScraper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nami
{
    public class CommandHub : BaseCommandModule
    {
        #region System Command (*)
        [RequirePrefixes("*")]
        [Command("restart")]
        [Description("This command will reset the discord bot")]
        public async Task Restart(CommandContext ctx)
        {
            var permissinGranted = await PermissionManager.CheckPermissions(ctx, PermissionManager.PermissionType.Admin);
            if (!permissinGranted) return;

            await Task.Run(async () =>
            {
                await ctx.RespondAsync("Nami is restarting, please wait a few seconds before trying to run any bot commands");
                Console.Clear();
                Program.instance.commandCode = Program.CommandCode.Reset;
            }).ConfigureAwait(false);
        }

        #endregion

        #region Moderator Commands (?)
        [RequirePrefixes("?")]
        [Command("announce")]
        [Description("This command is use to send announcments to the current server.")]
        public async Task AnnounceAsync(CommandContext ctx, [RemainingText][Description("The message that will be sent.")] string message)
        {
            var permissinGranted = await PermissionManager.CheckPermissions(ctx, PermissionManager.PermissionType.Admin);
            if (!permissinGranted) return;

            await Task.Run(async () =>
            {
                var embed = new DiscordEmbedBuilder();
                embed.Title = "Announcements";
                embed.Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    IconUrl = ctx.Member.AvatarUrl,
                    Name = ctx.User.Username
                };
                embed.Timestamp = new System.DateTimeOffset(DateTime.Now);
                embed.Description = $"@everyone {message}";
                embed.Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = $"Thank you for your time. {ctx.Guild.Name} Team",
                    IconUrl = ctx.Member.AvatarUrl,
                };
                embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                {
                    Url = ctx.Guild.IconUrl,
                };
                embed.Color = DiscordColor.Orange;
                foreach (var item in message.Split(' '))
                {
                    if (Uri.IsWellFormedUriString(item, UriKind.Absolute))
                    {
                        var gs = new GoogleScraper();
                        var image = await gs.GetImagesAsync(item, 1, false);
                        var field = embed.AddField("** **", "** **");
                        field.ImageUrl = image[0].ThumbnailLink;
                    }
                }
                var channel = ctx.Guild.Channels.ToList().Find(x => x.Value.Name.ToLower().Contains("announce")).Value ?? ctx.Guild.SystemChannel;
                await channel.SendMessageAsync(embed: embed.Build());
            }).ConfigureAwait(false);
            await ctx.Message.DeleteAsync();
            var msg = await ctx.RespondAsync("Your message has been sent to the server.");
            await Task.Delay(5000);
            await msg.DeleteAsync();
        }

        [RequirePrefixes("?")]
        [Command("announce-all")]
        [Description("This command is use to send announcments to all server that the bot is linked too")]
        public async Task AnnounceAllAsync(CommandContext ctx, [RemainingText][Description("The message that will be sent.")] string message)
        {
            var permissinGranted = await PermissionManager.CheckPermissions(ctx, PermissionManager.PermissionType.Admin);
            if (!permissinGranted) return;
            foreach (var server in ctx.Client.Guilds.ToList().Select(x => x.Value))
            {
                await Task.Run(async () =>
                {
                    var embed = new DiscordEmbedBuilder();
                    embed.Title = "Global Announcements";
                    embed.Author = new DiscordEmbedBuilder.EmbedAuthor()
                    {
                        IconUrl = ctx.Member.AvatarUrl,
                        Name = ctx.User.Username
                    };
                    embed.Timestamp = new System.DateTimeOffset(DateTime.Now);
                    embed.Description = $"@everyone {message}";
                    embed.Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = $"Thank you for your time. {ctx.Guild.Name} Team",
                        IconUrl = ctx.Member.AvatarUrl,
                    };
                    embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = server.IconUrl,
                    };
                    embed.Color = DiscordColor.IndianRed;
                    foreach (var item in message.Split(' '))
                    {
                        if (Uri.IsWellFormedUriString(item, UriKind.Absolute))
                        {
                            var gs = new GoogleScraper();
                            var image = await gs.GetImagesAsync(item, 1, false);
                            var field = embed.AddField("** **", "** **");
                            field.ImageUrl = image[0].ThumbnailLink;
                        }
                    }
                    var channel = server.Channels.ToList().Find(x => x.Value.Name.ToLower().Contains("announce")).Value ?? server.SystemChannel;
                    await channel.SendMessageAsync(embed: embed.Build());
                }).ConfigureAwait(false);
            }
            await ctx.Message.DeleteAsync();
            var msg = await ctx.RespondAsync("Your message has been sent to the server.");
            await Task.Delay(5000);
            await msg.DeleteAsync();
        }


        [RequirePrefixes("?")]
        [Command("purge")]
        [Description("This command will remove all messages that are in the text channel that the command was ran in.")]
        public async Task Purge(CommandContext ctx, int amount = 0)
        {
            var permissinGranted = await PermissionManager.CheckPermissions(ctx, PermissionManager.PermissionType.Admin);
            if (!permissinGranted) return;

            await Task.Run(async () =>
            {
                var messages = ctx.Channel.GetMessagesAsync(amount > 0 ? amount : 1000).Result.ToList();
                if(amount <= 100)
                {
                    await ctx.Channel.DeleteMessagesAsync(await ctx.Channel.GetMessagesAsync(amount), "The purge command on this channel has been called by a admin.");
                }
                else
                {
                    for (int i = 0; i < messages.Count; i++)
                    {
                        await messages[i].DeleteAsync("The purge command on this channel has been called by a admin.");
                    }
                }
            }).ConfigureAwait(false);
        }

        [RequirePrefixes("?")]
        [Command("respite")]
        [Description("This command will server mute a play for 1m for the first attempt, 3m for the second attempt, 5 for the sixed attempt and add two everytime the command is used for that member.")]
        public async Task Respite(CommandContext ctx, DiscordMember member, [RemainingText]string reason = default)
        {
            var permissinGranted = await PermissionManager.CheckPermissions(ctx, PermissionManager.PermissionType.Admin);
            if (!permissinGranted) return;

            await Task.Run(async () =>
            {
                await RespiteEngine.Add(ctx.Guild, member, reason);
            }).ConfigureAwait(false);
        }
        [RequirePrefixes("?")]
        [Command("rm-respite")]
        [Description("This command will remove server mute on a muted member.")]
        public async Task RemoveRespite(CommandContext ctx, DiscordMember member, [RemainingText]string reason = default)
        {
            var permissinGranted = await PermissionManager.CheckPermissions(ctx, PermissionManager.PermissionType.Admin);
            if (!permissinGranted) return;

            await Task.Run(async () =>
            {
                await RespiteEngine.Remove(ctx.Guild, member, reason);

            }).ConfigureAwait(false);
        }
        #endregion

        #region Music Commands (-)
        [RequirePrefixes("-")]
        [Command("join")]
        [Description("Has the bot join the voice channel that the user that ran the command is in. if user is not in voice channel it will join a channel that is name Music.")]
        public async Task Join(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Guild);
            if (conn == null)
            {
                var channel = default(DiscordChannel);
                if (ctx.Member.VoiceState != null)
                {
                    channel = ctx.Member.VoiceState.Channel;
                    if (channel.Type != ChannelType.Voice)
                    {
                        await ctx.RespondAsync("Not a valid voice channel.");
                        return;
                    }
                    conn = await node.ConnectAsync(channel);
                    await ctx.RespondAsync($"Connected!\nNow bound to {ctx.Channel.Mention} Text Channel and {channel.Mention} Voice Channel");
                }
                else return;
                // Create Music Instacne  for current guild
                var player = MusicPlayer.Connect(conn, ctx.Guild, ctx, channel, ctx.Channel);
                conn.PlaybackStarted += player.PlaybackStarted;
                conn.PlayerUpdated += player.PlaybackUpdated;
                conn.PlaybackFinished += player.PlaybackFinished;
            } 
        }

        [RequirePrefixes("-")]
        [Command("leave")]
        [Description("Has the bot leave the voice channel that it was currently in.")]
        public async Task Leave(CommandContext ctx)
        {
            if (ctx.Client.GetExtension<LavalinkExtension>() == null) return;

            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }
            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Disconnected!");
            MusicPlayer.Disconnect(ctx.Guild);
        }


        [RequirePrefixes("-")]
        [Command("play")]
        [Description("This is the music play command, this is also used to add songs to the queue.")]
        public async Task Play(CommandContext ctx, [Description("The url or search query to be played")][RemainingText] string query)
        {
            if(ctx.Client.GetExtension<LavalinkExtension>() == null) return;

            await Join(ctx);
            if (MusicPlayer.Find(ctx.Guild).TextCh != ctx.Channel) { await ctx.RespondAsync($"You are not int the right text channel you must be in {MusicPlayer.Find(ctx.Guild).TextCh.Mention} or unbind the bot.");  return; }

            var loadResult = await ctx.Client.GetLavalink().ConnectedNodes.Values.First().Rest.GetTracksAsync(query);
            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {query}.");
                return;
            }
            var track = loadResult.Tracks.First();

            await ctx.RespondAsync($"Now queued `{track.Author} | {track.Title}`");
            var result = Dispatcher.Load<NamiTrack>(track.Title);
            await MusicPlayer.Find(ctx.Guild).Play(ctx, result != null ? result : track);
        }

        [RequirePrefixes("-")]
        [Command("pause")]
        [Description("Pause the playback of the music player.")]
        public async Task Pause (CommandContext ctx)
        {

            if (MusicPlayer.Find(ctx.Guild).TextCh != ctx.Channel) { await ctx.RespondAsync($"You are not int the right text channel you must be in {MusicPlayer.Find(ctx.Guild).TextCh.Mention} or unbind the bot."); return; }

            if (ctx.Client.GetExtension<LavalinkExtension>() == null) return;
            await Join(ctx);
            var conn = ctx.Client.GetLavalink().ConnectedNodes.Values.First().GetGuildConnection(ctx.Guild);
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }
            await MusicPlayer.Find(ctx.Guild).Pause();
        }

        [RequirePrefixes("-")]
        [Command("Resume")]
        [Description("Resume the playback of the music player.")]
        public async Task Resume(CommandContext ctx)
        {
            if (MusicPlayer.Find(ctx.Guild).TextCh != ctx.Channel) { await ctx.RespondAsync($"You are not int the right text channel you must be in {MusicPlayer.Find(ctx.Guild).TextCh.Mention} or unbind the bot."); return; }
            if (ctx.Client.GetExtension<LavalinkExtension>() == null) return;
            await Join(ctx);
            var conn = ctx.Client.GetLavalink().ConnectedNodes.Values.First().GetGuildConnection(ctx.Guild);
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }
            await MusicPlayer.Find(ctx.Guild).Resume();
        }

        [RequirePrefixes("-")]
        [Command("stop")]
        [Description("Stop the playback of the music player")]
        public async Task Stop(CommandContext ctx)
        {
            if (MusicPlayer.Find(ctx.Guild).TextCh != ctx.Channel) { await ctx.RespondAsync($"You are not int the right text channel you must be in {MusicPlayer.Find(ctx.Guild).TextCh.Mention} or unbind the bot."); return; }
            if (ctx.Client.GetExtension<LavalinkExtension>() == null) return;
            await Join(ctx);
            var conn = ctx.Client.GetLavalink().ConnectedNodes.Values.First().GetGuildConnection(ctx.Guild);
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }
            await MusicPlayer.Find(ctx.Guild).Stop();
        }

        [RequirePrefixes("-")]
        [Command("next")]
        [Description("Stop the playback of the music player, and plays the next song in the queue")]
        public async Task Next(CommandContext ctx)
        {
            if (MusicPlayer.Find(ctx.Guild).TextCh != ctx.Channel) { await ctx.RespondAsync($"You are not int the right text channel you must be in {MusicPlayer.Find(ctx.Guild).TextCh.Mention} or unbind the bot."); return; }
            if (ctx.Client.GetExtension<LavalinkExtension>() == null) return;
            await Join(ctx);
            var conn = ctx.Client.GetLavalink().ConnectedNodes.Values.First().GetGuildConnection(ctx.Guild);
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }
            await MusicPlayer.Find(ctx.Guild).Next();
        }

        [RequirePrefixes("-")]
        [Command("previous")]
        [Description("Stop the playback of the music player, and plays the previous song in the queue")]
        public async Task Previous(CommandContext ctx)
        {
            if (MusicPlayer.Find(ctx.Guild).TextCh != ctx.Channel) { await ctx.RespondAsync($"You are not int the right text channel you must be in {MusicPlayer.Find(ctx.Guild).TextCh.Mention} or unbind the bot."); return; }
            if (ctx.Client.GetExtension<LavalinkExtension>() == null) return;
            await Join(ctx);
            var conn = ctx.Client.GetLavalink().ConnectedNodes.Values.First().GetGuildConnection(ctx.Guild);
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }
            await MusicPlayer.Find(ctx.Guild).Previous();
        }

        [RequirePrefixes("-")]
        [Command("clear")]
        [Description("Stop the playback of the music player, and clears the queue")]
        public async Task Clear(CommandContext ctx)
        {
            if (MusicPlayer.Find(ctx.Guild).TextCh != ctx.Channel) { await ctx.RespondAsync($"You are not int the right text channel you must be in {MusicPlayer.Find(ctx.Guild).TextCh.Mention} or unbind the bot."); return; }
            if (ctx.Client.GetExtension<LavalinkExtension>() == null) return;
            await Join(ctx);
            var conn = ctx.Client.GetLavalink().ConnectedNodes.Values.First().GetGuildConnection(ctx.Guild);
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }
            await MusicPlayer.Find(ctx.Guild).Clear();
        }

        [RequirePrefixes("-")]
        [Command("repeat")]
        [Description("Repeat the que, or one song in the queue.")]
        public async Task Repeat(CommandContext ctx, [Description("The repeat mode `one`, `all`, or `none`")]string mode)
        {
            if (MusicPlayer.Find(ctx.Guild).TextCh != ctx.Channel) { await ctx.RespondAsync($"You are not int the right text channel you must be in {MusicPlayer.Find(ctx.Guild).TextCh.Mention} or unbind the bot."); return; }
            if (ctx.Client.GetExtension<LavalinkExtension>() == null) return;
            await Join(ctx);
            var conn = ctx.Client.GetLavalink().ConnectedNodes.Values.First().GetGuildConnection(ctx.Guild);
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            if (Enum.TryParse<RepeatMode>(mode, true, out RepeatMode _mode))
                await MusicPlayer.Find(ctx.Guild).Repeat(_mode);
            else await MusicPlayer.Find(ctx.Guild).Repeat(RepeatMode.none);
        }

        [RequirePrefixes("-")]
        [Command("shuffle")]
        [Description("Suffles the que, and the order the songs are played.")]
        public async Task Shuffle(CommandContext ctx)
        {
            if (MusicPlayer.Find(ctx.Guild).TextCh != ctx.Channel) { await ctx.RespondAsync($"You are not int the right text channel you must be in {MusicPlayer.Find(ctx.Guild).TextCh.Mention} or unbind the bot."); return; }
            if (ctx.Client.GetExtension<LavalinkExtension>() == null) return;
            await Join(ctx);
            var conn = ctx.Client.GetLavalink().ConnectedNodes.Values.First().GetGuildConnection(ctx.Guild);
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }
            await MusicPlayer.Find(ctx.Guild).Shuffle();
        }

        [RequirePrefixes("-")]
        [Command("info")]
        [Description("Gets or updates the info of the embed that is most recently displayed")]
        public async Task Info(CommandContext ctx)
        {
            if (MusicPlayer.Find(ctx.Guild).TextCh != ctx.Channel) { await ctx.RespondAsync($"You are not int the right text channel you must be in {MusicPlayer.Find(ctx.Guild).TextCh.Mention} or unbind the bot."); return; }
            if (ctx.Client.GetExtension<LavalinkExtension>() == null) return;
            await Join(ctx);
            var conn = ctx.Client.GetLavalink().ConnectedNodes.Values.First().GetGuildConnection(ctx.Guild);
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }
            await MusicPlayer.Find(ctx.Guild).Info();
        }
        #endregion

        #region LewdCommands (?)
        [RequirePrefixes("?")]
        [Description("Request a lew embed from the bot")]
        [Command("lewd")]
        public async Task Lewd(CommandContext ctx)
        {
            var _18Plus = ctx.Member.Roles.ToList().Find(x => x.Name.Contains("18+"));
            if (_18Plus == null)
            {
                await ctx.Member.SendMessageAsync("Sorry, but you are not authorized to run this command. If you find this an error please contact one of the server admin, or moderators.");
                return;
            }
            var _NSFW = ctx.Guild.Channels.ToList().FindAll(x => x.Value.IsNSFW).Select(x => x.Value).ToList();
            var embed = new RedditManager(ctx).RequestNSFW();
            await _NSFW.First().SendMessageAsync(embed: embed);
        }

        #endregion
    }
}