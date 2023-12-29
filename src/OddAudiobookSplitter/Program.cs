namespace OddAudiobookSplitter;

using System.Xml;
using FFMpegCore;

class Program
{
    async static Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var folderInfo = new DirectoryInfo("C:\\Users\\michael\\Documents\\Audiobooks\\Rhythm of War\\");

        // filename count of chapters (prologues and pretext increment)
        var chapterCount = 0;

        var actions = new List<ChapterInfo>();
        foreach (var file in folderInfo.GetFiles())
        {
            if (file.Name.EndsWith("mp3", StringComparison.OrdinalIgnoreCase))
            {
                var result = await GetMediaInfo(file.FullName, chapterCount);

                chapterCount += result.chapterCount;
                actions.AddRange(result.chapterInfos);
            }
        }


        foreach (var action in actions)
        {
            Console.WriteLine(action.SrcFile);
            Console.WriteLine(action.FileChapter);
            Console.WriteLine(action.Name);
            Console.WriteLine(action.TimeCode);
        }
    }

    async static Task<(int chapterCount, List<ChapterInfo> chapterInfos)> GetMediaInfo(string inputPath, int chapterCount)
    {
        var internalChapterCount = chapterCount;
        var mediaInfo = await FFProbe.AnalyseAsync(inputPath);

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

                XmlElement root = doc.DocumentElement;

                if (root.Name == "Markers")
                {
                    var markers = root.GetElementsByTagName("Marker");

                    ///Console.WriteLine($"Found: {markers.Count} markers");

                    foreach (XmlElement marker in markers)
                    {
                        var name = marker.GetElementsByTagName("Name").Cast<XmlElement>().Single().InnerText;
                        var time = marker.GetElementsByTagName("Time").Cast<XmlElement>().Single().InnerText;

                        // Console.WriteLine($"Name: {name} markers");
                        // Console.WriteLine($"Time: {time} markers");

                        actions.Add(new ChapterInfo() { Name = name, TimeCode = time, FileChapter = internalChapterCount, SrcFile = inputPath });

                        internalChapterCount++;
                    }
                }


                // split current mp3 file into parts.
            }

            // Console.WriteLine($"====================");


        }
        return (internalChapterCount, actions);
    }

}
