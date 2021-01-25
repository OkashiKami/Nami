﻿using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nami.Common;
using Nami.Exceptions;
using Nami.Extensions;
using Nami.Modules.Games.Common;
using Nami.Modules.Games.Extensions;

namespace Nami.Modules.Games
{
    public partial class GamesModule
    {
        [Group("minesweeper")]
        [Aliases("mines", "ms")]
        public sealed class MinesweeperModule : NamiModule
        {
            #region game minesweeper
            [GroupCommand]
            public Task ExecuteGroupAsync(CommandContext ctx,
                                         [Description("desc-game-ms-rows")] int rows = 9,
                                         [Description("desc-game-ms-cols")] int cols = 9,
                                         [Description("desc-game-ms-bombs")] int bombs = 10)
            {
                if (rows < 4 || rows > 9 || cols < 4 || cols > 12)
                    throw new InvalidCommandUsageException(ctx, "cmd-err-game-ms-dim", 4, 9, 4, 12);

                try {
                    var field = new MinesweeperField(rows, cols, bombs);
                    return ctx.RespondAsync(field.ToEmojiString());
                } catch (ArgumentException) {
                    throw new CommandFailedException(ctx, "cmd-err-game-ms-bombs");
                }
            }
            #endregion

            #region game minesweeper rules
            [Command("rules")]
            [Aliases("help", "h", "ruling", "rule")]
            public Task RulesAsync(CommandContext ctx)
                => ctx.ImpInfoAsync(this.ModuleColor, Emojis.Information, "str-game-ms");
            #endregion
        }
    }
}
