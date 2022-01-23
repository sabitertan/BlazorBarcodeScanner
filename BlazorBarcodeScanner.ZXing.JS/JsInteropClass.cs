using Microsoft.JSInterop;
using System;
using System.Linq;

namespace BlazorBarcodeScanner.ZXing.JS
{
    public class JsInteropClass
    {

        [JSInvokable]
        public static void ReceiveBarcode(string barcodeText)
        {
            /* Unfortunately JS is unable to invoke public methods of internal classes. Thus
             * we route the call to the internal class at this point. This allows us to hide away
             * the rest of the interop from the component's client. */
            BarcodeReaderInterop.OnBarcodeReceived(barcodeText);
        }

        [JSInvokable]
        public static void ReceiveError(Exception error)
        {
            BarcodeReaderInterop.OnErrorReceived(error);
        }

        [JSInvokable]
        public static void ReceiveNotFound()
        {
            BarcodeReaderInterop.OnNotFoundReceived();
        }
    }
}
