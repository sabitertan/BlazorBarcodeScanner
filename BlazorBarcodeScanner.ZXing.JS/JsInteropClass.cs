using Microsoft.JSInterop;
using System;
using System.Linq;

namespace BlazorBarcodeScanner.ZXing.JS
{
    public class JsInteropClass
    {
        [Obsolete("Please use the BarcodeReader control's OnBarcodeReceived Blazor style event callback. This method of registering callbacks is likely to be removed in future releases.")]
        public static event BarcodeReceivedEventHandler BarcodeReceived;

        /// <summary>
        /// Invoked through the new, internal interop class (<see cref="BarcodeReaderInterop"/>). 
        /// Will be removed as soon as <see cref="BarcodeReceived"/> is removed.
        /// </summary>
        /// <param name="args"></param>
        internal static void OnBarcodeReceived(BarcodeReceivedEventArgs args)
        {
            BarcodeReceived?.Invoke(args);
        }

        [JSInvokable]
        public static void ReceiveBarcode(string barcodeText)
        {
            /* Unfortunately JS is unable to invoke public methods of internal classes. Thus
             * we route the call to the internal class at this point. This allows us to hide away
             * the rest of the interop from the component's client. */
            BarcodeReaderInterop.OnBarcodeReceived(barcodeText);
        }

        [JSInvokable]
        public static void ReceiveError(object error)
        {
            // What to do with the knowledge that an error happened?
            // Looking at current examples this might indicate issues with one of the decoders
            // (namely BrowserQRCodeReader appears to throw errors occasionally...)
        }

        [JSInvokable]
        public static void ReceiveNotFound()
        {
            BarcodeReaderInterop.OnNotFoundReceived();
        }
    }
}
