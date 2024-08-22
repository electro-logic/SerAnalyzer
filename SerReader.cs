using System.Text;

public class SerReader : IDisposable
{
    BinaryReader _reader;
    bool _isDisposed;
    //public SerReader(string filename)
    //{
    //    _reader = new BinaryReader(System.IO.File.Open(filename, FileMode.Open));
    //}
    public SerReader(Stream stream)
    {
        _reader = new BinaryReader(stream);
    }
    public List<DateTime> _timeStamps = new List<DateTime>();
    public Int32 _imageWidth, _imageHeight, _bitDepth, _frameCount, _frameBytes;
    public string _instrument = "", _telescope = "";
    public SerColorID _colorID = SerColorID.MONO;
    public SerEndianess _endianess = SerEndianess.BIG_ENDIAN;
    public DateTime _startDateTimeLocal, _startDateTimeUTC;
    public double FilesizeMBytes => Math.Round(_reader.BaseStream.Length / (1024.0 * 1024.0), 1);

    public TimeSpan VideoDuration => _timeStamps.Count > 0 ? _timeStamps.Last() - _timeStamps.First() : TimeSpan.Zero;

    public static int HeaderSize => 178;

    public void ReadHeader()
    {
        if (_isDisposed)
            return;
        _timeStamps.Clear();
        // 1. FileID - Fixed to "LUCAM-RECORDER"
        var fileID = _reader.ReadBytes(14); // "LUCAM-RECORDER"
        var luID = _reader.ReadInt32();
        _colorID = (SerColorID)_reader.ReadInt32();
        _endianess = (SerEndianess)_reader.ReadInt32();
        _imageWidth = _reader.ReadInt32();
        _imageHeight = _reader.ReadInt32();
        _bitDepth = _reader.ReadInt32();
        _frameCount = _reader.ReadInt32();
        var observer = ASCIIEncoding.ASCII.GetString(_reader.ReadBytes(40));
        _instrument = ASCIIEncoding.ASCII.GetString(_reader.ReadBytes(40));
        _telescope = ASCIIEncoding.ASCII.GetString(_reader.ReadBytes(40));
        _startDateTimeLocal = new DateTime(_reader.ReadInt64(), DateTimeKind.Local);
        _startDateTimeUTC = new DateTime(_reader.ReadInt64(), DateTimeKind.Utc);
        ReadTimestamps();
    }

    public void ReadTimestamps()
    {
        // Skip frame content
        var framePosition = _reader.BaseStream.Position;
        _frameBytes = (_bitDepth / 8) * _imageWidth * _imageHeight;
        _reader.BaseStream.Seek(_frameBytes * _frameCount, SeekOrigin.Current);
        _timeStamps.Clear();
        // Date / Integer_64 (little-endian) time stamps in UTC for every image frame
        for (int frameIndex = 0; frameIndex < _frameCount; frameIndex++)
        {
            _timeStamps.Add(new DateTime(_reader.ReadInt64(), DateTimeKind.Utc));
        }
        // Move back to beginning of frame content
        _reader.BaseStream.Position = framePosition;
    }

    public byte[] ReadFrame()
    {
        return _reader.ReadBytes(_frameBytes);
    }
    //public Image ReadFrameImage()
    //{
    //    var bytes = ReadFrame();
    //    var buffer = ByteBuffer.AllocateDirect(bytes.Length);
    //    buffer.Put(bytes);
    //    buffer.Rewind();
    //    return new Image(_imageWidth, _imageHeight, _bitDepth, buffer, _instrument, 0, 0, null);
    //}
    public void Close()
    {
        if (_isDisposed)
            return;
        _reader.Close();
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