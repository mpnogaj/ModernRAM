﻿using System;
using ProjectRAM.Core.Commands;

namespace ProjectRAM.Core.Commands.JumpCommands;

[CommandName("jgtz")]
public class JGTZCommand : JumpCommandBase
{
    public JGTZCommand(long line, string? label, string argument) : base(line, label, argument)
    {
    }

    public override ulong Execute(string accumulator, Action<string> makeJump)
    {
        if (accumulator.IsPositive())
        {
            makeJump(FormattedArgument);
        }

        return accumulator.LCost();
    }
}