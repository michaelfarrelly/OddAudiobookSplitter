namespace OddAudiobookSplitter;

public record ChapterInfo
{
    public string? Name;
    public string? TimeCode;

    public int FileChapter;

    public string? SrcFile;
}
