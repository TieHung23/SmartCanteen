using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session.DeleteSession;

internal class DeleteSessionCommandHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteSessionCommandHandler> logger
) : ICommandHandler<DeleteSessionCommand, DeleteSessionResponse>
{
    public async Task<Result<DeleteSessionResponse>> Handle(
        DeleteSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionRepository.GetByIdAsync(request.Id, cancellationToken);
            if (session is null)
            {
                return Result.Failure<DeleteSessionResponse>(
                    Error.NullValue,
                    $"Session with id {request.Id} not found.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            session.SoftDelete();
            sessionRepository.Update(session);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new DeleteSessionResponse
            {
                Id = request.Id,
                Message = "Session deleted successfully (soft delete)."
            };

            return Result.Success(response, "Session deleted successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error deleting session with id {SessionId}", request.Id);
            return Result.Failure<DeleteSessionResponse>(
                Error.ServerError,
                "An error occurred while deleting the session.");
        }
    }
}
