namespace TriBalance.Application.Common;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}
