namespace OddAudiobookSplitter;

using FFMpegCore;

class Program
{
    async static Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var mediaInfo = await FFProbe.AnalyseAsync("C:\\Users\\michael\\Documents\\Audiobooks\\Rhythm of War\\Rhythm of War-Part01.mp3");

        Console.WriteLine($"Duration: {mediaInfo.Format.Duration}");

        var tags = mediaInfo.Format.Tags;

        if (tags != null)
        {
            if (tags.Where(t => t.Key == "OverDrive MediaMarkers").Count() > 0)
            {
                // ir is an overdrive audiobook.

                // this tag contains an XML of time codes like
                // <Markers><Marker><Name>Rhythm of War</Name><Time>0:00.000</Time></Marker><Marker><Name>Preface and Acknowledgements</Name><Time>0:29.000</Time></Marker><Marker><Name>Prologue</Name><Time>13:47.000</Time></Marker></Markers>
            }
        }

        Console.WriteLine($"Format: {string.Join(",", mediaInfo.Format.Tags.Select(t => $"{t.Key}={t.Value}"))}");


    }
}
