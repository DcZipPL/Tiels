public class Program
{
    [System.STAThreadAttribute()]
    public static void Main()
    {
        using (var uwp = new TielsUWPApp.App())
        {
            Tiels.App app = new Tiels.App();
            app.InitializeComponent();
            app.Run();
        }
    }
}