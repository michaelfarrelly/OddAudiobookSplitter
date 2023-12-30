namespace OddAudiobookSplitter;

using System.ComponentModel.DataAnnotations;
using System.Xml;
using FFMpegCore;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("OddAudiobookSplitter");

        var unvalidatedFolderName = args[0];
        var result = ValidateFolderName(unvalidatedFolderName)
            .OnSuccess(Analyze);

        if (result.Failure)
        {
            // report failures
            Console.WriteLine(result.Error);
        }

        if (result.Success)
        {
            Run(result.Value);
        }

    }

    private static Result<(DirectoryInfo folderInfo, List<ChapterInfo>)> Analyze(DirectoryInfo folderInfo)
    {

        // "C:\\Users\\michael\\Documents\\Audiobooks\\Rhythm of War\\"
        Console.WriteLine($"Using folder: {folderInfo}");

        // filename count of chapters (prologues and pretext increment)
        var chapterCount = 0;

        var actions = new List<ChapterInfo>();
        foreach (var file in folderInfo.GetFiles())
        {
            if (file.Name.EndsWith("mp3", StringComparison.OrdinalIgnoreCase))
            {
                var result = GetMediaInfo(file.FullName, chapterCount);

                chapterCount += result.chapterCount;
                actions.AddRange(result.chapterInfos);
            }
        }

        return Result.Ok<(DirectoryInfo folderInfo, List<ChapterInfo>)>((folderInfo, actions));

    }

    private static Result Run((DirectoryInfo folderInfo, List<ChapterInfo> actions) data)
    {
        var actions = data.actions;
        var folderInfo = data.folderInfo;

        // split current mp3 file into parts.
        foreach (var action in actions)
        {
            Console.WriteLine(action.SrcFile);
            Console.WriteLine($"{action.FileChapter}-{action.Name}-{action.TimeCode}-{action.StartTime}-{action.EndTime} ({action.SrcDuration})");
            // Console.WriteLine(action.TimeCode);
            // Console.WriteLine(action.StartTime);
            // Console.WriteLine(action.EndTime);

            // FFMpeg.ExtractAudio()
            // FFMpeg.SubVideo(action.SrcFile, "output.mp3", startTime, endTime);

            Console.WriteLine($"Extracting audio");

            var output = $"{folderInfo.FullName}output\\{folderInfo.Name}-{action.FileChapter}-{action.Name}.mp3";
            FFMpegArguments
                 .FromFileInput(action.SrcFile, true, options => options.Seek(action.StartTime).EndSeek(action.EndTime))
                 .OutputToFile(output, true, options => options.CopyChannel())
                 .ProcessSynchronously();

        }

        return Result.Ok();
    }

    private static Result<DirectoryInfo> ValidateFolderName(string unvalidatedFolderName)
    {
        if (string.IsNullOrEmpty(unvalidatedFolderName))
        {
            return Result.Fail<DirectoryInfo>("folder name must be provided");
        }

        var directoryInfo = new DirectoryInfo(unvalidatedFolderName);
        if (!directoryInfo.Exists)
        {
            return Result.Fail<DirectoryInfo>("folder name must exist");
        }

        return Result.Ok(directoryInfo);
    }

    /// <summary>
    /// This doesn't validate all TimeSpans. Just ones from the Overdrive Metadata tag on their MP3 files.
    ///
    /// Overdrive uses MM:SS.SSSS format, and if the minutes exceeds 59, it keeps counting (has no hour unit, which TimeSpan expects).
    /// </summary>
    private static Result<TimeSpan> ValidateTimeSpan(string? unvalidatedTimeSpan)
    {
        if (string.IsNullOrEmpty(unvalidatedTimeSpan))
        {
            return Result.Fail<TimeSpan>("TimeSpan must be provided");
        }

        var parts = unvalidatedTimeSpan.Split(":");
        if (parts.Count() == 2)
        {
            // check first part to see if its greater than 60.

            var minutes = int.Parse(parts[0]);
            if (minutes > 59)
            {
                // fix it.
                // 63:25.000 -> 01:03:25.000
                unvalidatedTimeSpan = $"01:{(minutes - 59).ToString().PadLeft(2, '0')}:{parts[1]}";
            }
            else
            {
                // pad the time string
                unvalidatedTimeSpan = $"00:{unvalidatedTimeSpan}";
            }

        }

        var ts = TimeSpan.Parse(unvalidatedTimeSpan);

        return Result.Ok(ts);
    }

    static (int chapterCount, List<ChapterInfo> chapterInfos) GetMediaInfo(string inputPath, int chapterCount)
    {
        var fileChapterCount = 0;
        var mediaInfo = FFProbe.Analyse(inputPath);

        // Console.WriteLine($"Filename: {inputPath}");
        // Console.WriteLine($"Duration: {mediaInfo.Format.Duration}");
        // Console.WriteLine($"Tags: {string.Join(",", mediaInfo.Format.Tags.Select(t => $"{t.Key}={t.Value}"))}");

        var tags = mediaInfo.Format.Tags;

        var actions = new List<ChapterInfo>();

        if (tags != null)
        {
            var overdriveTag = tags.Where(t => t.Key == "OverDrive MediaMarkers");
            if (overdriveTag.Count() > 0)
            {
                // ir is an overdrive audiobook.

                // this tag contains an XML of time codes like
                // <Markers><Marker><Name>Rhythm of War</Name><Time>0:00.000</Time></Marker><Marker><Name>Preface and Acknowledgements</Name><Time>0:29.000</Time></Marker><Marker><Name>Prologue</Name><Time>13:47.000</Time></Marker></Markers>

                var xmlMarkers = overdriveTag.First().Value;

                // parse xmlMarkers
                var doc = new XmlDocument();
                doc.LoadXml(xmlMarkers);

                XmlElement? root = doc?.DocumentElement;

                if (root?.Name == "Markers")
                {
                    var markers = root.GetElementsByTagName("Marker");

                    ///Console.WriteLine($"Found: {markers.Count} markers");


                    foreach (XmlElement marker in markers)
                    {
                        var name = marker.GetElementsByTagName("Name").Cast<XmlElement>().Single().InnerText.Trim();
                        var time = marker.GetElementsByTagName("Time").Cast<XmlElement>().Single().InnerText.Trim();

                        var ts = ValidateTimeSpan(time);

                        // check for next sibling
                        var endTime = GetNextTimeSpan(marker, mediaInfo.Format.Duration);

                        // Console.WriteLine($"Name: {name} markers");
                        // Console.WriteLine($"Time: {time} markers");

                        actions.Add(new ChapterInfo
                        {
                            Name = name,
                            TimeCode = time,
                            StartTime = ts.Success ? ts.Value : null,
                            EndTime = endTime,
                            FileChapter = chapterCount + fileChapterCount,
                            SrcFile = inputPath,
                            SrcDuration = mediaInfo.Format.Duration
                        });



                        fileChapterCount++;
                    }
                }



            }

            // Console.WriteLine($"====================");


        }
        return (fileChapterCount, actions);
    }

    public static TimeSpan GetNextTimeSpan(XmlElement marker, TimeSpan fileDuration)
    {
        var nextText = (marker.NextSibling as XmlElement)?
            .GetElementsByTagName("Time")
            .Cast<XmlElement>()
            .Single().InnerText;

        var ts = ValidateTimeSpan(nextText);

        return ts.Success ? ts.Value : fileDuration;
    }

}
