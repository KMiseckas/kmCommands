// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands
{
    /// <summary>
    /// Delegate invoked when a command is executed.
    /// Arguments are pre-converted to the types declared in the command's parameter signature.
    /// </summary>
    /// <param name="args">
    /// The converted argument values in the order declared by the command's parameter signature.
    /// Each element is already typed as declared; cast to the expected type before use.
    /// </param>
    public delegate void CommandCallback(object[] args);
}
