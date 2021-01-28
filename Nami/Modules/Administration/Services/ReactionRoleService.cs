﻿using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nami.Database;
using Nami.Database.Models;
using Nami.Services;

namespace Nami.Modules.Administration.Services
{
    public sealed class ReactionRoleService : DbAbstractionServiceBase<ReactionRole, ulong, string>
    {
        public override bool IsDisabled => false;


        public ReactionRoleService(DbContextBuilder dbb)
            : base(dbb) { }


        public override DbSet<ReactionRole> DbSetSelector(NamiDbContext db)
            => db.ReactionRoles;

        public override IQueryable<ReactionRole> GroupSelector(IQueryable<ReactionRole> rrs, ulong gid)
            => rrs.Where(ar => ar.GuildIdDb == (long)gid);

        public override ReactionRole EntityFactory(ulong gid, string emoji)
            => new ReactionRole { GuildId = gid, Emoji = emoji };

        public override string EntityIdSelector(ReactionRole rr)
            => rr.Emoji;

        public override ulong EntityGroupSelector(ReactionRole rr)
            => rr.GuildId;

        public override object[] EntityPrimaryKeySelector(ulong gid, string emoji)
            => new object[] { (long)gid, emoji };
    }
}
