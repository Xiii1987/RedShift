public class RRNReceivedEmail
{
    public RRNEmailDefinition Definition { get; private set; }

    public string Date { get; private set; }
    public string Time { get; private set; }

    public bool IsRead { get; private set; }
    public bool IsArchived { get; private set; }
    public bool HasResponded { get; private set; }
    public RRNEmailResponseDefinition SelectedResponse { get; private set; }

    public RRNReceivedEmail(
        RRNEmailDefinition definition,
        string date,
        string time)
    {
        Definition = definition;
        Date = date;
        Time = time;

        IsRead = false;
        IsArchived = false;
        HasResponded = false;
        SelectedResponse = null;
    }

    public void MarkRead()
    {
        IsRead = true;
    }

    public void MarkArchived()
    {
        IsArchived = true;
    }

    public void MarkResponded(RRNEmailResponseDefinition response)
    {
        HasResponded = true;
        SelectedResponse = response;
    }
}
