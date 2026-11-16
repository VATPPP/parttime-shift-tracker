using SQLite;

namespace ShiftTracker.App.Models;

[Table("coworker_tags")]
public class CoworkerTag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

