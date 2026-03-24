package net.dot.android;

public class ApplicationRegistration {

	public static android.content.Context Context;

	public static void registerApplications ()
	{
		// Application and Instrumentation ACWs must be registered first.
		mono.android.Runtime.register ("LocoCalcAvalonia.Android.MainApplication, LocoCalcAvalonia.Android, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", crc64bfda960072047588.MainApplication.class, crc64bfda960072047588.MainApplication.__md_methods);
		
	}
}
