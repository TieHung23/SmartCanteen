namespace SC.Application.MediatR.Session;

/// <summary>
/// Settings behind the refunds a session deletion issues. Deliberately separate from
/// <c>CHANGE_PROPOSAL</c>: money handed back because a manager removed a whole session is a
/// different story from money handed back because the kitchen came up short, and the refund report
/// groups by <c>PolicyCode</c> - sharing one code would merge the two into a single unreadable line.
/// </summary>
public static class SessionRefundSettingConstants
{
    public const string Group = "SESSION";
    public const string RefundScope = "REFUND";
    public const string DeleteRefundPolicyCode = "DELETE_REFUND_POLICY_CODE";

    /// <summary>
    /// The refund policy seeded for this flow. Only the fallback documented in the migration -
    /// the setting above is what the handler actually reads, so an instance can point the flow at
    /// a different policy without a code change.
    /// </summary>
    public const string DefaultPolicyCode = "SESSION_DELETED_NO_IMAGE";
}
