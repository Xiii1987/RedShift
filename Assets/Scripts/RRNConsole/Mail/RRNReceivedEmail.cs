public class RRNReceivedEmail
{
    public RRNEmailDefinition Definition { get; private set; }

    public string Date { get; private set; }
    public string Time { get; private set; }

    public bool IsRead { get; private set; }
    public bool HasResponded { get; private set; }

    public RRNReceivedEmail(
        RRNEmailDefinition definition,
        string date,
        string time)
    {
        Definition = definition;
        Date = date;
        Time = time;

        IsRead = false;
        HasResponded = false;
    }

    public void MarkRead()
    {
        IsRead = true;
    }

    public void MarkResponded()
    {
        HasResponded = true;
    }
}
