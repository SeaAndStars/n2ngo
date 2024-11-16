namespace N2NGO.Utils;

public class RoomConnection
{
    public bool IsConnected { get; set; } = false;
    public string CurrentRoomCode { get; set; } = string.Empty;
    public string MemberID { get; set; } = string.Empty;  // Logical MemberID relative to room
}
