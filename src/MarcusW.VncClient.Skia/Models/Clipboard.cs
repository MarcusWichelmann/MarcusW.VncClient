namespace MarcusW.VncClient.Skia.Models;

public static class Clipboard
{
    public static string? Text { get; set; }

    public static void SetText(string text)
    {
        Text = text;
    }
}
