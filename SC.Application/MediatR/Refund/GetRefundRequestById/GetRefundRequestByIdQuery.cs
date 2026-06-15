using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Refund.GetRefundRequestById;

public sealed record GetRefundRequestByIdQuery(Guid Id)
    : IQuery<GetRefundRequestByIdResponse>;
