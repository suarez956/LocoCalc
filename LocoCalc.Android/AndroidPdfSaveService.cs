using Android.Content;
using AndroidX.Core.Content;
using LocoCalc.Services;

namespace LocoCalc;

/// <summary>
/// Android PDF implementation using the built-in Android.Graphics.Pdf.PdfDocument API.
/// No QuestPDF/SkiaSharp needed. Saves to CacheDir and shares via FileProvider intent.
/// </summary>
public class AndroidPdfSaveService : IPdfSaveService
{
    private readonly Context _context;
    private string? _pendingPath;

    // Set by MainActivity so we can startActivity
    public static global::Android.App.Activity? CurrentActivity { get; set; }

    public AndroidPdfSaveService(Context context)
    {
        _context = context;
    }

    /// <summary>
    /// On Android we don't show a save dialog — we save to cache and share later.
    /// Returns the temp path so the caller can generate + write the bytes.
    /// </summary>
    public Task<string?> PickSavePathAsync(string suggestedName)
    {
        var dir  = _context.CacheDir!.AbsolutePath;
        var path = global::System.IO.Path.Combine(dir, suggestedName);
        _pendingPath = path;
        return Task.FromResult<string?>(path);
    }

    /// <summary>Opens the PDF via FileProvider share intent.</summary>
    public void OpenFile(string path)
    {
        var activity = CurrentActivity;
        if (activity is null || !File.Exists(path)) return;

        var file = new Java.IO.File(path);
        var uri  = FileProvider.GetUriForFile(
            _context,
            _context.PackageName + ".fileprovider",
            file);

        var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(uri, "application/pdf");
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        intent.AddFlags(ActivityFlags.NewTask);

        // Use chooser so user can pick PDF viewer or share to other apps
        var chooser = Intent.CreateChooser(intent, "Otevřít PDF / Open PDF");
        chooser!.AddFlags(ActivityFlags.NewTask);
        activity.StartActivity(chooser);
    }
}
