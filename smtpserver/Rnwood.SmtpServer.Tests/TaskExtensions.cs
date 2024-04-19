// <copyright file="TaskExtensions.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="TaskExtensions" />
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="task">The task<see cref="Task" /></param>
    /// <param name="seconds">The seconds<see cref="int" /></param>
    /// <param name="descriptionOfTask">The descriptionOfTask<see cref="string" /></param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    public static async Task WithTimeout(this Task task, int seconds, string descriptionOfTask)
    {
        if (Debugger.IsAttached)
        {
            await task;
            return;
        }

        Task completedTask = await Task.WhenAny(task, Task.Delay(seconds * 1000));

        if (completedTask != task)
        {
            throw new TimeoutException("Timeout waiting for " + descriptionOfTask);
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="task">The task<see cref="Task" /></param>
    /// <param name="descriptionOfTask">The descriptionOfTask<see cref="string" /></param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    public static async Task WithTimeout(this Task task, string descriptionOfTask) =>
        await WithTimeout(task, 10, descriptionOfTask);
}
