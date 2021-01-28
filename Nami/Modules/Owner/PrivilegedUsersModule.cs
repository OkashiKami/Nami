﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Nami.Attributes;
using Nami.Database.Models;
using Nami.Exceptions;
using Nami.Extensions;
using Nami.Modules.Owner.Services;

namespace Nami.Modules.Owner
{
    [Group("privilegedusers"), Module(ModuleType.Owner), Hidden]
    [Aliases("pu", "privu", "privuser", "pusers", "puser", "pusr")]
    [RequireOwner]
    public sealed class PrivilegedUsersModule : NamiServiceModule<PrivilegedUserService>
    {
        #region privilegedusers
        [GroupCommand, Priority(1)]
        public Task ExecuteGroupAsync(CommandContext ctx)
            => this.ListAsync(ctx);

        [GroupCommand, Priority(0)]
        public Task ExecuteGroupAsync(CommandContext ctx,
                                     [Description("desc-users")] params DiscordUser[] users)
            => this.AddAsync(ctx, users);
        #endregion

        #region privilegedusers add
        [Command("add")]
        [Aliases("register", "reg", "new", "a", "+", "+=", "<<", "<", "<-", "<=")]
        public async Task AddAsync(CommandContext ctx,
                                  [Description("desc-users")] params DiscordUser[] users)
        {
            if (users is null || !users.Any())
                throw new InvalidCommandUsageException(ctx, "cmd-err-missing-users");

            await this.Service.AddAsync(users.Select(u => new PrivilegedUser { UserId = u.Id }));
            await ctx.InfoAsync(this.ModuleColor);
        }
        #endregion

        #region privilegedusers delete
        [Command("delete")]
        [Aliases("unregister", "remove", "rm", "del", "d", "-", "-=", ">", ">>", "->", "=>")]
        public async Task DeleteAsync(CommandContext ctx,
                                     [Description("desc-users")] params DiscordUser[] users)
        {
            if (users is null || !users.Any())
                throw new InvalidCommandUsageException(ctx, "cmd-err-missing-users");

            await this.Service.RemoveAsync(users.Select(u => new PrivilegedUser { UserId = u.Id }));
            await ctx.InfoAsync(this.ModuleColor);
        }
        #endregion

        #region privilegedusers list
        [Command("list")]
        [Aliases("print", "show", "view", "ls", "l", "p")]
        public async Task ListAsync(CommandContext ctx)
        {
            IReadOnlyList<PrivilegedUser> privileged = await this.Service.GetAsync();

            var notFound = new List<PrivilegedUser>();
            var valid = new List<DiscordUser>();
            foreach (PrivilegedUser pu in privileged) {
                try {
                    DiscordUser user = await ctx.Client.GetUserAsync(pu.UserId);
                    valid.Add(user);
                } catch (NotFoundException) {
                    LogExt.Debug(ctx, "Found 404 privileged user: {UserId}", pu.UserId);
                    notFound.Add(pu);
                }
            }

            if (!valid.Any())
                throw new CommandFailedException(ctx, "cmd-err-choice-none");

            await ctx.PaginateAsync(
                "str-priv",
                valid,
                user => user.ToString(),
                this.ModuleColor,
                10
            );

            LogExt.Information(ctx, "Removing {Count} not found privileged users", notFound.Count);
            await this.Service.RemoveAsync(notFound);
        }
        #endregion
    }
}
