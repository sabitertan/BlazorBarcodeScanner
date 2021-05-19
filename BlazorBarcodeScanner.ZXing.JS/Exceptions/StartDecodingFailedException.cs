using System;

namespace BlazorBarcodeScanner.ZXing.JS.Exceptions
{
    public class StartDecodingFailedException : Exception
    {
        public StartDecodingFailedException(string message, Exception e) : base(message, e)
        {
        }
    }
}
