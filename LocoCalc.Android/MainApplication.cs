using Android.Runtime;

namespace LocoCalc;

[global::Android.App.Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership)
    : global::Android.App.Application(handle, ownership)
{
    // Avalonia is fully initialised in MainActivity.
}
