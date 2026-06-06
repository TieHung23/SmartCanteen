using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Payment.GetPaymentById;

public class GetPaymentByIdQuery : IQuery<GetPaymentByIdResponse>
{
    public Guid Id { get; set; }

    public GetPaymentByIdQuery()
    {
    }

    public GetPaymentByIdQuery(Guid id)
    {
        Id = id;
    }
}
