using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBarcodeScanner.ZXing.JS
{
    public class JsInteropClass
    {
        [Obsolete]
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
    }
}
