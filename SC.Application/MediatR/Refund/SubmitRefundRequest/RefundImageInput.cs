namespace SC.Application.MediatR.Refund.SubmitRefundRequest;

public sealed record RefundImageInput(
    Stream Content,
    string FileName,
    long FileSize,
    string MimeType);
