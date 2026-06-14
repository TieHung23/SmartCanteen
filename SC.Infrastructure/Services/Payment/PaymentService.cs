using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SC.Contract.Services.Payment;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Payment.Enum;
using SC.Domain.Domain.WalletTransaction.Entity;
using SC.Domain.Domain.WalletTransaction.Enum;
using SC.Infrastructure.DependencyInjection.Options;
using PaymentAggregate = SC.Domain.Domain.Payment.AggregateRoot.Payment;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Infrastructure.Services.Payment;

public class PaymentService(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<PaymentAggregate, Guid> paymentRepository,
    IGenericRepository<WalletTransaction, Guid> walletTransactionRepository,
    IGenericRepository<SettingAggregate, Guid> settingRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IOptions<SePayOptions> sePayOptions,
    ILogger<PaymentService> logger) : IPaymentService
{
    private const string VndPerPointSettingCode = "VND_PER_POINT";
    private const string MinTopUpAmountSettingCode = "MIN_TOPUP_AMOUNT";
    private const string MaxTopUpAmountSettingCode = "MAX_TOPUP_AMOUNT";
    private readonly SePayOptions _sePayOptions = sePayOptions.Value;

    public async Task<Result<TopUpWalletResult>> TopUpWalletAsync(
        decimal amountVnd,
        int method,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (amountVnd <= 0)
            {
                return Result.Failure<TopUpWalletResult>(
                    Error.InvalidValue,
                    "Top-up amount must be greater than zero.");
            }

            var vndPerPointResult = await GetRequiredDecimalSettingAsync(
                VndPerPointSettingCode,
                cancellationToken);
            if (vndPerPointResult.IsFailure)
            {
                return Result.Failure<TopUpWalletResult>(
                    vndPerPointResult.Error ?? Error.ServerError,
                    vndPerPointResult.Message);
            }

            var minTopUpAmountResult = await GetRequiredDecimalSettingAsync(
                MinTopUpAmountSettingCode,
                cancellationToken);
            if (minTopUpAmountResult.IsFailure)
            {
                return Result.Failure<TopUpWalletResult>(
                    minTopUpAmountResult.Error ?? Error.ServerError,
                    minTopUpAmountResult.Message);
            }

            var maxTopUpAmountResult = await GetRequiredDecimalSettingAsync(
                MaxTopUpAmountSettingCode,
                cancellationToken);
            if (maxTopUpAmountResult.IsFailure)
            {
                return Result.Failure<TopUpWalletResult>(
                    maxTopUpAmountResult.Error ?? Error.ServerError,
                    maxTopUpAmountResult.Message);
            }

            var vndPerPoint = vndPerPointResult.Value;
            var minTopUpAmount = minTopUpAmountResult.Value;
            var maxTopUpAmount = maxTopUpAmountResult.Value;

            if (minTopUpAmount > maxTopUpAmount)
            {
                return Result.Failure<TopUpWalletResult>(
                    Error.InvalidValue,
                    "Payment top-up min amount cannot be greater than max amount.");
            }

            if (amountVnd < minTopUpAmount)
            {
                return Result.Failure<TopUpWalletResult>(
                    Error.InvalidValue,
                    $"Top-up amount must be at least {minTopUpAmount} VND.");
            }

            if (amountVnd > maxTopUpAmount)
            {
                return Result.Failure<TopUpWalletResult>(
                    Error.InvalidValue,
                    $"Top-up amount must not exceed {maxTopUpAmount} VND.");
            }

            if (amountVnd % vndPerPoint != 0)
            {
                return Result.Failure<TopUpWalletResult>(
                    Error.InvalidValue,
                    $"Top-up amount must be divisible by {vndPerPoint} VND.");
            }

            if (!Enum.IsDefined(typeof(PaymentMethod), method))
            {
                return Result.Failure<TopUpWalletResult>(
                    Error.InvalidValue,
                    "Payment method is invalid.");
            }

            if ((PaymentMethod)method != PaymentMethod.SePay)
            {
                return Result.Failure<TopUpWalletResult>(
                    Error.InvalidValue,
                    "Only SePay top-up is currently supported.");
            }

            var currentUserId = currentUserService.UserId;
            var user = await userRepository.GetByIdAsync(currentUserId, cancellationToken);

            if (user is null)
            {
                return Result.Failure<TopUpWalletResult>(
                    Error.NullValue,
                    "User not found.");
            }

            var sePayConfigurationError = ValidateSePayConfiguration();
            if (sePayConfigurationError is not null)
            {
                return Result.Failure<TopUpWalletResult>(
                    Error.InvalidValue,
                    sePayConfigurationError);
            }

            var convertedPoints = amountVnd / vndPerPoint;
            var gatewayOrderId = CreatePaymentCode();
            var paymentContent = CreatePaymentContent(gatewayOrderId);
            var payUrl = CreateQrUrl(amountVnd, paymentContent);

            var payment = PaymentAggregate.Create(
                gatewayOrderId,
                amountVnd,
                convertedPoints,
                (PaymentMethod)method,
                currentUserId,
                currentUserId,
                PaymentType.TopUp);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await paymentRepository.AddAsync(payment, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var result = new TopUpWalletResult(
                payment.Id,
                amountVnd,
                convertedPoints,
                method,
                payment.Status.ToString(),
                payment.GatewayOrderId,
                paymentContent,
                payUrl,
                _sePayOptions.BankName.Trim(),
                _sePayOptions.BankAccountNumber.Trim(),
                _sePayOptions.BankAccountName.Trim());

            return Result.Success(result, "SePay top-up payment created successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error topping up wallet for user {UserId}", currentUserService.UserId);
            return Result.Failure<TopUpWalletResult>(
                Error.ServerError,
                "An error occurred while topping up wallet.");
        }
    }

    public async Task<Result<CompletePaymentResult>> HandleSepayIpnAsync(
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        PaymentAggregate? payment = null;

        try
        {
            var transferType = GetValue(data, "transferType");
            if (!string.Equals(transferType, "in", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<CompletePaymentResult>(
                    Error.InvalidValue,
                    "SePay transaction is not an incoming transfer.");
            }

            var orderId = ResolveSepayOrderId(data);
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return Result.Failure<CompletePaymentResult>(
                    Error.InvalidValue,
                    "SePay payment code is missing.");
            }

            payment = await paymentRepository.GetQueryable(
                x => x.GatewayOrderId == orderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (payment is null)
            {
                return Result.Failure<CompletePaymentResult>(
                    Error.NullValue,
                    "Payment not found.");
            }

            if (payment.Method != PaymentMethod.SePay)
            {
                return Result.Failure<CompletePaymentResult>(
                    Error.InvalidValue,
                    "Payment method does not match SePay top-up.");
            }

            if (payment.Status == PaymentStatus.Completed)
            {
                return Result.Success(
                    new CompletePaymentResult(
                        payment.Id,
                        payment.GatewayOrderId,
                        payment.Status.ToString(),
                        payment.ConvertedPoints),
                    "Payment was already completed.");
            }

            var gatewayTransactionId = GetValue(data, "id");
            if (string.IsNullOrWhiteSpace(gatewayTransactionId))
            {
                return Result.Failure<CompletePaymentResult>(
                    Error.InvalidValue,
                    "SePay transaction ID is missing.");
            }

            if (!decimal.TryParse(
                    GetValue(data, "transferAmount"),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var amountVnd)
                || amountVnd != payment.AmountVnd)
            {
                return Result.Failure<CompletePaymentResult>(
                    Error.InvalidValue,
                    "SePay transfer amount does not match payment.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await unitOfWork.LockUserAsync(payment.UserId, cancellationToken);

            var balanceAfter = await unitOfWork.TryCreditUserBalanceAsync(
                payment.UserId,
                payment.ConvertedPoints,
                cancellationToken);

            if (balanceAfter is null)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<CompletePaymentResult>(
                    Error.NullValue,
                    "Payment user not found.");
            }

            var balanceBefore = balanceAfter.Value - payment.ConvertedPoints;
            payment.MarkAsCompleted(gatewayTransactionId, payment.UserId);

            var transaction = WalletTransaction.Create(
                payment.UserId,
                payment.ConvertedPoints,
                balanceBefore,
                balanceAfter.Value,
                WalletTransactionType.TopUp,
                payment.Id);

            paymentRepository.Update(payment);
            await walletTransactionRepository.AddAsync(transaction, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(
                new CompletePaymentResult(
                    payment.Id,
                    payment.GatewayOrderId,
                    payment.Status.ToString(),
                    payment.ConvertedPoints),
                "SePay payment completed successfully.");
        }
        catch (DbUpdateException ex)
            when (payment is not null
                && IsUniqueViolation(ex, "IX_WalletTransaction_PaymentId"))
        {
            await unitOfWork.RollbackAsync(cancellationToken);

            var persistedPayment = await paymentRepository
                .GetQueryable(x => x.Id == payment.Id)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (persistedPayment?.Status == PaymentStatus.Completed)
            {
                logger.LogInformation(
                    "SePay payment {PaymentId} was completed by another callback.",
                    payment.Id);

                return Result.Success(
                    new CompletePaymentResult(
                        persistedPayment.Id,
                        persistedPayment.GatewayOrderId,
                        persistedPayment.Status.ToString(),
                        persistedPayment.ConvertedPoints),
                    "Payment was already completed.");
            }

            logger.LogError(ex, "Duplicate wallet transaction detected for payment {PaymentId}", payment.Id);
            return Result.Failure<CompletePaymentResult>(
                Error.ServerError,
                "Payment completion could not be confirmed.");
        }
        catch (DbUpdateException ex)
            when (IsUniqueViolation(ex, "IX_Payments_GatewayTransactionId"))
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogWarning(ex, "SePay transaction was already linked to another payment.");
            return Result.Failure<CompletePaymentResult>(
                Error.InvalidValue,
                "SePay transaction was already processed.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error handling SePay payment IPN");
            return Result.Failure<CompletePaymentResult>(
                Error.ServerError,
                "An error occurred while handling SePay IPN.");
        }
    }

    private async Task<Result<decimal>> GetRequiredDecimalSettingAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetQueryable(
            x => !x.IsDeleted && x.Code == code)
            .FirstOrDefaultAsync(cancellationToken);

        if (setting is null)
        {
            return Result.Failure<decimal>(
                Error.InvalidValue,
                $"Payment setting {code} is missing.");
        }

        if (!decimal.TryParse(setting.Value, out var value) || value <= 0)
        {
            return Result.Failure<decimal>(
                Error.InvalidValue,
                $"Payment setting {code} is invalid.");
        }

        return Result.Success(value, $"Payment setting {code} loaded.");
    }

    private static string GetValue(IReadOnlyDictionary<string, string> data, string key)
    {
        return data.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private string ResolveSepayOrderId(IReadOnlyDictionary<string, string> data)
    {
        var paymentCodePrefix = _sePayOptions.PaymentCodePrefix.Trim();
        var code = GetValue(data, "code");
        if (code.StartsWith(paymentCodePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return code;
        }

        var content = GetValue(data, "content");
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(part => part.StartsWith(
                paymentCodePrefix,
                StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
    }

    private string? ValidateSePayConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_sePayOptions.BankName))
        {
            return "SePay bank name is not configured.";
        }

        if (string.IsNullOrWhiteSpace(_sePayOptions.BankAccountNumber))
        {
            return "SePay bank account number is not configured.";
        }

        var prefix = _sePayOptions.PaymentCodePrefix.Trim();
        if (prefix.Length is < 2 or > 5 || !prefix.All(char.IsLetterOrDigit))
        {
            return "SePay payment code prefix must contain 2 to 5 letters or digits.";
        }

        return null;
    }

    private string CreatePaymentCode()
    {
        var prefix = _sePayOptions.PaymentCodePrefix.Trim().ToUpperInvariant();
        return $"{prefix}{Guid.NewGuid():N}"[..30];
    }

    private string CreatePaymentContent(string paymentCode)
    {
        var requiredPrefix = _sePayOptions.RequiredTransferContentPrefix.Trim();
        return string.IsNullOrWhiteSpace(requiredPrefix)
            ? paymentCode
            : $"{requiredPrefix} {paymentCode}";
    }

    private string CreateQrUrl(decimal amountVnd, string paymentContent)
    {
        var query = new Dictionary<string, string>
        {
            ["acc"] = _sePayOptions.BankAccountNumber.Trim(),
            ["bank"] = _sePayOptions.BankName.Trim(),
            ["amount"] = amountVnd.ToString("0.##", CultureInfo.InvariantCulture),
            ["des"] = paymentContent,
            ["template"] = string.IsNullOrWhiteSpace(_sePayOptions.QrTemplate)
                ? "compact"
                : _sePayOptions.QrTemplate.Trim()
        };

        var queryString = string.Join(
            "&",
            query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        return $"https://qr.sepay.vn/img?{queryString}";
    }

    private static bool IsUniqueViolation(DbUpdateException exception, string constraintName)
    {
        return exception.InnerException is DbException { SqlState: "23505" } databaseException
            && databaseException.Message.Contains(constraintName, StringComparison.Ordinal);
    }
}
