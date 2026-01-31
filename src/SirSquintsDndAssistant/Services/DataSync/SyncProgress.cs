namespace SirSquintsDndAssistant.Services.DataSync;

public class SyncProgress
{
    public int Percentage { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CurrentItem { get; set; }
    public int TotalItems { get; set; }
    public bool IsComplete { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}
