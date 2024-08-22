namespace SerInfo;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("SER Analyzer v1");
        if (args.Length != 1)
        {
            Console.WriteLine("usage: SerAnalyzer.exe [video.ser]");
            return;
        }
        var file = args[0];
        Console.WriteLine($"File Name\t\t{file}");
        var fileInfo = new FileInfo(file);
        var fileSize = fileInfo.Length;
        Console.WriteLine($"File Size:\t\t{fileSize}");
        var reader = new SerReader(File.OpenRead(file));
        reader.ReadHeader();
        Console.WriteLine($"Frame format:\t\t{reader._imageWidth}x{reader._imageHeight}x{reader._bitDepth}");
        var frameBytes = reader._imageWidth * reader._imageHeight * (reader._bitDepth / 8);
        Console.WriteLine($"Frame bytes:\t\t{frameBytes}");
        Console.WriteLine($"Frame count:\t\t{reader._frameCount}");
        if (reader._frameCount == 0)
        {
            var frames = (int)Math.Floor((fileSize - SerReader.HeaderSize) / (double)frameBytes);
            var timestampSize = frames * 8;
            while (fileSize - SerReader.HeaderSize - (frames * frameBytes) - timestampSize < 0)
            {
                frames--;
                timestampSize = frames * 8;
            }
            Console.WriteLine($"Estimated frames:\t{frames}");
            reader._frameCount = frames;
            reader.ReadTimestamps();
            var remaining = fileSize - SerReader.HeaderSize - (frames * frameBytes) - timestampSize;
            Console.WriteLine($"Spare bytes:\t\t{remaining}");
            Console.WriteLine($"Recovering metadata..");
            var writer = new SerWriter(File.OpenWrite($"{fileInfo.Name}_restored.ser"));
            writer.WriteHeader(reader._imageWidth, reader._imageHeight, reader._bitDepth, reader._instrument, reader._telescope, reader._colorID, reader._endianess);
            for (int frame = 0; frame < frames; frame++)
            {
                await writer.WriteFrameAsync(reader.ReadFrame(), reader._timeStamps[frame].Ticks);
            }
            writer.Close();
            Console.WriteLine($"Done");
        }
        Console.ReadKey();
    }
}