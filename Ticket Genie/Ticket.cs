using System.Dynamic;

public class Ticket
{
    public int id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string response { get; set; }
    public int completed { get; set; }
    public int closedBy { get; set; }
    public int resolvedBy { get; set; }
}
