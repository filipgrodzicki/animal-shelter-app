using MediatR;

namespace ShelterApp.Domain.Common;

/// <summary>
/// Marker interface for queries that return data without modifying state
/// </summary>
public interface IQuery<TResponse> : IRequest<TResponse>;

/// <summary>
/// Handler for queries
/// </summary>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
