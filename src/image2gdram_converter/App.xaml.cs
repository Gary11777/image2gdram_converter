using System.Windows;
using Image2Gdram.Core.Text;

namespace image2gdram_converter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Cp1251.RegisterEncodingProvider();
        base.OnStartup(e);
    }
}
