﻿namespace Elmish.Uno.Samples.iOS;

using UIKit;

public static class EntryPoint
{
    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppHead));
    }
}