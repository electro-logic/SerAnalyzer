using System.Text;

public enum SerColorID
{
    MONO = 0,
    BAYER_RGGB = 8,
    BAYER_GRBG = 9,
    BAYER_GBRG = 10,
    BAYER_BGGR = 11,
    BAYER_CYYM = 16,
    BAYER_YCMY = 17,
    BAYER_YMCY = 18,
    BAYER_MYYC = 19,
    RGB = 100,
    BGR = 101
}

public enum SerEndianess
{
    BIG_ENDIAN = 0,
    LITTLE_ENDIAN = 1
}

// SER file format (http://www.grischa-hahn.homepage.t-online.de/astro/ser/SER%20Doc%20V3b.pdf)
public class SerWriter : IDisposable
{
    BinaryWriter _writer;
    List<Int64> _timeStamps = new List<Int64>();
    bool _isDisposed;
    static object _lock = new object();
    public SerWriter(Stream outStream)
    {
        _writer = new BinaryWriter(outStream);
    }
    public double FilesizeMBytes => Math.Round(_writer.BaseStream.Length / (1024.0 * 1024.0), 1);

    public void WriteHeader(Int32 imageWidth, Int32 imageHeight, Int32 bitDepth,
        string instrument = "", string telescope = "", SerColorID colorID = SerColorID.MONO, SerEndianess endianess = SerEndianess.BIG_ENDIAN)
    {
        lock (_lock)
        {
            if (_isDisposed)
                return;
            _timeStamps.Clear();
            // 1. FileID - Fixed to "LUCAM-RECORDER"
            _writer.Write(ASCIIEncoding.ASCII.GetBytes("LUCAM-RECORDER"));
            // 2. LuID - Lumenera camera series ID (currently unused; default = 0)
            _writer.Write((Int32)0);
            // 3. ColorID
            _writer.Write((Int32)colorID);
            // 4. LittleEndian
            _writer.Write((Int32)endianess);
            // 5. ImageWidth
            _writer.Write(imageWidth);
            // 6. ImageHeight
            _writer.Write(imageHeight);
            // 7. PixelDepthPerPlane - True bit depth per pixel per plane
            _writer.Write(bitDepth);
            // 8. FrameCount - Number of image frames in SER file
            _writer.Write((Int32)0);   // we write this at the end
            // 9. Observer - Name of observer
            _writer.Write(new byte[40]);
            // 10. Instrument - Name of used camera
            _writer.Write(StringToBytes(instrument, 40));
            // 11. Telescope - Name of used telescope
            _writer.Write(StringToBytes(telescope, 40));
            // 12. DateTime - Start time of image stream (local time)
            _writer.Write((Int64)DateTime.Now.Ticks);
            // 13. Start time of image stream in UTC
            _writer.Write((Int64)DateTime.UtcNow.Ticks);
            _writer.Flush();
        }
    }
    public async Task WriteFrameAsync(byte[] bytes, long timestamp)
    {
        //lock (_lock)
        {
            if (_isDisposed)
                return;

            _timeStamps.Add(timestamp);
            await _writer.BaseStream.WriteAsync(bytes);
            //_writer.Write(bytes);

            //_writer.Flush();
        }
    }
    public async Task WriteFrameAsync(byte[] bytes)
    {
        //lock (_lock)
        {
            if (_isDisposed)
                return;

            _timeStamps.Add(DateTime.UtcNow.Ticks);
            await _writer.BaseStream.WriteAsync(bytes);
            //_writer.Write(bytes);

            //_writer.Flush();
        }
    }
    public void Close()
    {
        lock (_lock)
        {
            if (_isDisposed)
                return;
            // Date / Integer_64 (little-endian) time stamps in UTC for every image frame
            foreach (var timeStamp in _timeStamps)
            {
                _writer.Write(timeStamp);
            }
            _writer.Flush();
            // Write FrameCount
            _writer.Seek(38, SeekOrigin.Begin);
            _writer.Write((Int32)_timeStamps.Count);
            _writer.Close();
        }
    }
    byte[] StringToBytes(string str, int lenght)
    {
        if (str == null)
            str = string.Empty;
        var s = str.Substring(0, Math.Min(lenght, str.Length));
        var b = ASCIIEncoding.ASCII.GetBytes(s);
        var r = b.Concat(Enumerable.Repeat((byte)0, lenght - b.Length)).ToArray();
        return r;
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Close();
            }
            _isDisposed = true;
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}