using System;
using System.Threading;
using System.Threading.Tasks;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Dispatches a synchronous callback onto a host-selected execution context.
/// </summary>
/// <remarks>
/// A dispatcher controls only the supplied callback. It does not control the
/// command handler's continuation after an awaited operation.
/// </remarks>
public interface ICommandDispatcher
{
    /// <summary>Executes an action through the host-selected dispatcher.</summary>
    /// <param name="action">The action to dispatch.</param>
    /// <param name="cancellationToken">The execution cancellation token.</param>
    /// <returns>A task that completes when the action has run.</returns>
    Task InvokeAsync(Action action, CancellationToken cancellationToken = default);

    /// <summary>Executes a function through the host-selected dispatcher and returns its value.</summary>
    /// <typeparam name="TResult">The callback result type.</typeparam>
    /// <param name="callback">The function to dispatch.</param>
    /// <param name="cancellationToken">The execution cancellation token.</param>
    /// <returns>A task that completes with the callback result.</returns>
    Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken cancellationToken = default);
}
