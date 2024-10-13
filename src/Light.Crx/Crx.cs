using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CrxFile;

namespace Light.Crx;

public class Crx : IDisposable
{
    public string Id { get; }
    public Manifest Manifest { get; }
    private readonly MemoryStream memoryStream;
    private readonly ZipArchive _content;
    private readonly Translator _translator;

    public Crx(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, true);

        if (!reader.ReadChars(4).SequenceEqual("Cr24"))
        {
            throw new InvalidDataException("Invalid CRX file header");
        }

        var version = reader.ReadUInt32();
        if (version != 3)
        {
            throw new InvalidDataException("Invalid CRX file version");
        }

        var n = reader.ReadUInt32();
        var hdr = CrxFileHeader.Parser.ParseFrom(reader.ReadBytes((int)n));

        memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        Id = CrxId(hdr);
        _content = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        var manifestEntry = _content.GetEntry("manifest.json") ?? throw new Exception("manifest.json not found");
        using var manifestStream = manifestEntry.Open();
        Manifest = JsonSerializer.Deserialize<Manifest>(manifestStream, ManifestJsonOptions) ?? throw new Exception("manifest.json parse failed");
        _translator = Manifest.DefaultLocale == null ? Translator.Default : new Translator(Manifest.DefaultLocale, _content);
    }

    public Dictionary<string, Icon> Icon()
    {
        return Manifest.Icons.Select(m => new Icon(_content.GetEntry(m.Value))).ToDictionary(m => m.Name, m => m);
    }

    public string GetMessage(string msg, string lang = "")
    {
        return _translator.Message(lang, msg);
    }

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static string CrxId(CrxFileHeader hdr)
    {
        var signedData = SignedData.Parser.ParseFrom(hdr.SignedHeaderData);
        if (signedData == null)
        {
            return "";
        }

        var sb = new StringBuilder(32);
        foreach (var b in signedData.CrxId)
        {
            sb.Append((char)((b >> 4) + 'a'));
            sb.Append((char)((b & 0x0F) + 'a'));
        }

        return sb.ToString();
    }

    public byte[] ToByteArray()
    {
        return memoryStream.ToArray();
    }

    public void Dispose()
    {
        _content.Dispose();
    }
}

public class Icon
{
    public string Name { get; set; }
    public byte[] Data { get; set; }

    internal Icon(ZipArchiveEntry entry)
    {
        Name = entry.Name;
        Data = new byte[entry.Length];
        using var stream = entry.Open();
        stream.Read(Data, 0, (int)entry.Length);
    }
}

