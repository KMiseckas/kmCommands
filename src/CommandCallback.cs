// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands
{
    /// <summary>
    /// Delegate invoked when a command is executed.
    /// Return <c>null</c> for void commands, or the command's return value for non-void commands.
    /// Arguments are pre-converted to the types declared in the command's parameter signature.
    /// </summary>
    /// <param name="args">
    /// The converted argument values in the order declared by the command's parameter signature.
    /// Each element is already typed as declared; cast to the expected type before use.
    /// </param>
    /// <returns>
    /// The command's return value, or <c>null</c> for void commands.
    /// </returns>
    public delegate object CommandCallback(object[] args);
}
