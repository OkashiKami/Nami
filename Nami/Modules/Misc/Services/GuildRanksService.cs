﻿using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nami.Database;
using Nami.Database.Models;
using Nami.Services;

namespace Nami.Misc.Services
{
    public sealed class GuildRanksService : DbAbstractionServiceBase<XpRank, ulong, short>
    {
        public override bool IsDisabled => false;


        public GuildRanksService(DbContextBuilder dbb)
            : base(dbb) { }


        public override DbSet<XpRank> DbSetSelector(NamiDbContext db)
            => db.XpRanks;

        public override IQueryable<XpRank> GroupSelector(IQueryable<XpRank> entities, ulong grid) =>
            entities.Where(r => r.GuildIdDb == (long)grid);

        public override XpRank EntityFactory(ulong grid, short id)
            => new XpRank { GuildId = grid, Rank = id };

        public override short EntityIdSelector(XpRank entity)
            => entity.Rank;

        public override ulong EntityGroupSelector(XpRank entity)
            => entity.GuildId;

        public override object[] EntityPrimaryKeySelector(ulong grid, short id)
            => new object[] { (long)grid, id };
    }
}
