public class Ticket
{
    public int id { get; set; }
    public int type { get; set; }
    public int playerGUID { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public int createTime { get; set; }
    public int lastModifiedTime { get; set; }
    public int closedBy { get; set; }
    public string handledBy { get; set; }
    public string response { get; set; }
    public int completed { get; set; }
    public int viewed { get; set; }
    public int resolvedBy { get; set; }
}
