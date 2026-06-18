namespace SC.Domain.Domain.RobotEventLog.Enum;

/// <summary>
/// Loại sự kiện robot service báo về BE (audit + realtime).
/// </summary>
public enum RobotEventType
{
    Connected = 0,
    Disconnected = 1,
    JobReceived = 2,
    PickStarted = 3,
    PickCompleted = 4,
    PlaceCompleted = 5,
    Error = 6,
    Recovered = 7,
    EmergencyStop = 8
}
