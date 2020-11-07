
namespace VisiPlacement
{
    class ErrorReporter
    {
        // reports an event that is supposed to be impossible
        public static void ReportParadox(string error)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
    }
}
