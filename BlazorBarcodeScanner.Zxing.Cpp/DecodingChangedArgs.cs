namespace BlazorBarcodeScanner.ZXing.Cpp
{
    public class DecodingChangedArgs
    {
        public BarcodeReader? Sender { get; set; }
        public bool IsDecoding { get; set; }
    }
}
