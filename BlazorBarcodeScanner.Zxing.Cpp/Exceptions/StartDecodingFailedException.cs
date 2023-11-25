namespace BlazorBarcodeScanner.ZXing.Cpp.Exceptions
{
    public class StartDecodingFailedException : Exception
    {
        public StartDecodingFailedException(string message, Exception e) : base(message, e)
        {
        }
    }
}
