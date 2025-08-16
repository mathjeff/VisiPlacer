
using System;
using System.Diagnostics;

namespace VisiPlacement
{
    class ErrorReporter
    {
        // reports an event that is supposed to be impossible
        public static void ReportParadox(string error)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }

        // whether we should attempt to debug problems
        public static bool ShouldDebug()
        {
            return Debugger.IsAttached && !UserDisabledDebug;
        }
        public static bool UserDisabledDebug { get; set; }
    }
}
