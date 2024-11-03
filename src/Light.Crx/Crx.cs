using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CrxFile;

namespace Light.Crx;

public class Crx : IDisposable
{
    public string Id { get; }
    public Manifest Manifest { get; }

    private readonly ZipArchive _zipArchive;

    private readonly Translator _translator;
    private readonly IconExtractor _iconExtractor;

    private Crx(string id, ZipArchive zipArchive)
    {
        Id = id;
        _zipArchive = zipArchive;
        Manifest = ExtractManifest(_zipArchive);
        _translator = new Translator(Manifest.DefaultLocale, _zipArchive);
        _iconExtractor = new IconExtractor(_zipArchive);
    }

    public static Crx FromStream(Stream stream)
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

        var id = CrxId(hdr);
        return new Crx(id, new ZipArchive(stream, ZipArchiveMode.Read));
    }

    public static Crx FromZipArchive(ZipArchive zipArchive)
    {
        return new Crx("", zipArchive);
    }

    private static Manifest ExtractManifest(ZipArchive zipArchive)
    {
        var manifestEntry = zipArchive.GetEntry("manifest.json") ?? throw new Exception("manifest.json not found");
        using var manifestStream = manifestEntry.Open();
        return JsonSerializer.Deserialize<Manifest>(manifestStream, ManifestJsonOptions) ?? throw new Exception("manifest.json parse failed");
    }

    public Dictionary<string, Icon> Icons()
    {
        return Manifest.Icons.Select(m => _iconExtractor.Extract(m.Value)).ToDictionary(m => m.Name, m => m);
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

    public void Dispose()
    {
        _zipArchive.Dispose();
    }
}

