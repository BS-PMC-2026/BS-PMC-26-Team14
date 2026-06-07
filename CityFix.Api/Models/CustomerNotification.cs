public class CustomerNotification
{
    public int Id { get; set; }

    public string CustomerEmail { get; set; } = "";

    public int ReportId { get; set; }

    public string Message { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }
}