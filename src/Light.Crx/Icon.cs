using System.IO.Compression;

namespace Light.Crx;

internal class IconExtractor
{
    private ZipArchive _zipArchive;

    public IconExtractor(ZipArchive zipArchive)
    {
        _zipArchive = zipArchive;
    }

    public Icon Extract(string iconPath)
    {
        var entry = _zipArchive.GetEntry(iconPath);

        var name = entry.Name;
        var data = new byte[entry.Length];
        using var stream = entry.Open();
        stream.Read(data, 0, (int)entry.Length);

        return new Icon { Name = name, Data = data };
    }
}

public class Icon
{
    public string Name { get; set; } = null!;
    public byte[] Data { get; set; } = null!;
}

