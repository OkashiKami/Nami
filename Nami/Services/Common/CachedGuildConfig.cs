﻿using System.Globalization;
using Nami.Modules.Administration.Common;

namespace Nami.Services.Common
{
    public sealed class CachedGuildConfig
    {
        public const string DefaultCurrency = "NUD";
        public const decimal DefaultAllowance = 1500.00m;
        public const string DefaultStarboardEmoji = ":star:";
        public const int DefaultStarboardSensitivity = 5;
        public const string DefaultTimezoneId = "Central Standard Time";

        public string Currency { get; set; } = DefaultCurrency;
        public decimal Allowance { get; set; } = DefaultAllowance;
        public string Prefix { get; set; } = BotConfig.DefaultPrefix;
        public string Locale {
            get => this.locale;
            set {
                this.locale = value;
                this.culture = new CultureInfo(value, false);
            }
        }
        public string TimezoneId { get; set; } = DefaultTimezoneId;
        public string? StarboardEmoji { get; set; }

        public ulong? StreamerBadge { get; set; } = 0;

        public ulong StarboardChannelId { get; set; } = 0;
        public int StarboardSensitivity { get; set; } = DefaultStarboardSensitivity;
        public ulong LogChannelId { get; set; } = 0;
        public bool SuggestionsEnabled { get; set; } = false;
        public bool ReactionResponse { get; set; } = false;
        public LinkfilterSettings LinkfilterSettings { get; set; } = new LinkfilterSettings();
        public AntispamSettings AntispamSettings { get; set; } = new AntispamSettings();
        public AntiMentionSettings AntiMentionSettings { get; set; } = new AntiMentionSettings();
        public RatelimitSettings RatelimitSettings { get; set; } = new RatelimitSettings();

        public bool LoggingEnabled => this.LogChannelId != default;
        public bool StarboardEnabled => this.StarboardChannelId != default;
        public CultureInfo Culture {
            get {
                if (this.culture is null)
                    this.culture = new CultureInfo(this.Locale, false);
                return this.culture;
            }
        }


        private CultureInfo? culture;
        private string locale = BotConfig.DefaultLocale;
    }
}
