﻿using System;
using DSharpPlus.CommandsNext;
using Nami.Services;

namespace Nami.Exceptions
{
    public class LocalizationException : LocalizedException
    {
        public LocalizationException(string rawMessage)
            : base(rawMessage)
        {

        }

        public LocalizationException(CommandContext ctx, params object?[]? args)
            : base(ctx, args)
        {

        }

        public LocalizationException(CommandContext ctx, string key, params object?[]? args)
            : base(ctx, key, args)
        {

        }

        public LocalizationException(CommandContext ctx, Exception inner, string key, params object?[]? args)
            : base(ctx, key, inner, args)
        {

        }

        public LocalizationException(LocalizationService lcs, ulong gid, params object?[]? args)
            : base(lcs, gid, args)
        {

        }

        public LocalizationException(LocalizationService lcs, ulong gid, string key, params object?[]? args)
            : base(lcs, gid, key, args)
        {

        }

        public LocalizationException(LocalizationService lcs, ulong gid, Exception inner, string key, params object?[]? args)
            : base(lcs, gid, key, inner, args)
        {

        }
    }
}
