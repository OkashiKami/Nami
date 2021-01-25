﻿using System.Threading.Tasks;
using DSharpPlus.Entities;
using Nami.Modules.Administration.Services;

namespace Nami.Modules.Administration.Extensions
{
    public static class AutoRoleServiceExtensions
    {
        public static async Task GrantRolesAsync(this AutoRoleService service, NamiBot shard, DiscordGuild guild, DiscordMember member)
        {
            foreach (ulong rid in service.GetIds(guild.Id)) {
                DiscordRole? role = guild.GetRole(rid);
                if (role is { }) {
                    await LoggingService.TryExecuteWithReportAsync(
                        shard, guild, member.GrantRoleAsync(role), "rep-role-403", "rep-role-404",
                        code404action: () => service.RemoveAsync(guild.Id, rid)
                    );
                } else {
                    await service.RemoveAsync(guild.Id, rid);
                }
            }
        }
    }
}
