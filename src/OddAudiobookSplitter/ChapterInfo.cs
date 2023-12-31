

namespace OddAudiobookSplitter;

public record ChapterInfo
{
    public string? Name;
    public string? TimeCode;

    public int FileChapter;

    public string? SrcFile;

    public TimeSpan SrcDuration;

    public TimeSpan? StartTime;
    public TimeSpan? EndTime;

    public Dictionary<string, string>? Tags;
}
