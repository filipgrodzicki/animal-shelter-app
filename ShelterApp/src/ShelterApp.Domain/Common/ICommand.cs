using MediatR;

namespace ShelterApp.Domain.Common;

/// <summary>
/// Marker interface for commands that modify state
/// </summary>
public interface ICommand<TResponse> : IRequest<TResponse>;

/// <summary>
/// Marker interface for commands without response
/// </summary>
public interface ICommand : IRequest<Unit>;

/// <summary>
/// Handler for commands with response
/// </summary>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;

/// <summary>
/// Handler for commands without response
/// </summary>
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Unit>
    where TCommand : ICommand;
