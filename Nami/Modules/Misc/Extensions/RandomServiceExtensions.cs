﻿using System;
using System.Linq;
using DSharpPlus.Entities;
using Nami.Common;
using Nami.Modules.Misc.Services;

namespace Nami.Modules.Misc.Extensions
{
    public static class RandomServiceExtensions
    {
        public static bool EightBall(this RandomService service, DiscordChannel channel, string question, out string answer)
        {
            bool localized = true;

            if (question.StartsWith("when", StringComparison.InvariantCultureIgnoreCase) ||
                question.StartsWith("how long", StringComparison.InvariantCultureIgnoreCase)) {
                answer = service.GetRandomTimeAnswer();
            } else if (question.StartsWith("who", StringComparison.InvariantCultureIgnoreCase) && channel.Guild is { }) {
                var rng = new SecureRandom();
                DiscordMember member = rng.ChooseRandomElement(rng.NextBool(3)
                    ? channel.Users.Where(m => IsOnline(m))
                    : channel.Users.Where(m => !IsOnline(m))
                );
                answer = member.Mention;
                localized = false;
            } else if (question.StartsWith("how much", StringComparison.InvariantCultureIgnoreCase) ||
                question.StartsWith("how many", StringComparison.InvariantCultureIgnoreCase)) {
                answer = service.GetRandomQuantityAnswer();
            } else {
                answer = service.GetRandomYesNoAnswer();
            }

            return localized;

            static bool IsOnline(DiscordMember m)
                => (m?.Presence?.Status ?? UserStatus.Offline) >= UserStatus.Online;
        }
    }
}
