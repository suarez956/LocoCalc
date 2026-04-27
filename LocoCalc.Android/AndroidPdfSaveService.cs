using Android.Content;
using Android.Provider;
using LocoCalc.Services.PdfServices;

namespace LocoCalc;

public class AndroidPdfSaveService : IPdfSaveService
{
    private readonly Context _context;

    public static global::Android.App.Activity? CurrentActivity { get; set; }

    public AndroidPdfSaveService(Context context)
    {
        _context = context;
    }

    public Task<string?> PickSavePathAsync(string suggestedName)
    {
        var path = Path.Combine(_context.CacheDir!.AbsolutePath, suggestedName);
        return Task.FromResult<string?>(path);
    }

    public string? OpenFile(string path)
    {
        var activity = CurrentActivity;
        if (activity is null || !File.Exists(path)) return null;

        var fileName = Path.GetFileName(path);

        var values = new ContentValues();
        values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
        values.Put(MediaStore.IMediaColumns.MimeType, "application/pdf");
        values.Put(MediaStore.IMediaColumns.RelativePath, "Documents/LocoCalc ZoB");

        var resolver   = _context.ContentResolver!;
        var collection = MediaStore.Files.GetContentUri("external")!;
        var docUri     = resolver.Insert(collection, values);
        if (docUri is null) return null;

        using var output = resolver.OpenOutputStream(docUri);
        if (output is null) return null;
        output.Write(File.ReadAllBytes(path));
        output.Flush();

        try { File.Delete(path); } catch { /* best-effort */ }

        var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(docUri, "application/pdf");
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        intent.AddFlags(ActivityFlags.NewTask);

        var chooser = Intent.CreateChooser(intent, "Otevřít PDF / Open PDF");
        chooser!.AddFlags(ActivityFlags.NewTask);
        activity.StartActivity(chooser);

        return $"Documents/LocoCalc ZoB/{fileName}";
    }
}
