
namespace Light.Crx.Test;

public class CrxTest
{
    [Fact]
    public async Task TestCrx()
    {
        string downloadUrlTemplate =
            "https://clients2.google.com/service/update2/crx?response=redirect&prodversion={0}&acceptformat=crx2,crx3&x=id%3D{1}%26uc";
        string version = "118.0.5993.96";
        string pluginId = "ekmbchepcdggpcbdpjpijphjiiiimfga";

        var downloadUrl = string.Format(downloadUrlTemplate, version, pluginId);
        var client = new HttpClient();
        var stream = await client.GetStreamAsync(downloadUrl);
        var crx = Crx.FromStream(stream);

        Assert.Equal(crx.Id, pluginId);
    }
}