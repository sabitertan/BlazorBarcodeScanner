using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorBarcodeScanner.ZXing.JS
{
    public class JsInteropClass
    {
        public static ValueTask<List<VideoInputDevice>> GetVideoInputDevices(IJSRuntime jsRuntime, string message)
        {
            // Implemented in BlazorBarcodeScannerJsInterop.js

            return jsRuntime.InvokeAsync<List<VideoInputDevice>>(
                "BlazorBarcodeScanner.listVideoInputDevices",
                message);
        }
        public static void StartDecoding(IJSRuntime jSRuntime, string videoElementId) {
            jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.startDecoding", videoElementId);
        }
        public static void StopDecoding(IJSRuntime jSRuntime)
        {
            jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.stopDecoding");
        }
        public static void SetVideoInputDevice(IJSRuntime jSRuntime, string deviceId) {
            jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.setSelectedDeviceId", deviceId);
        }
        [JSInvokable]
        public static void ReceiveBarcode(string barcodeText) {
            if (!String.IsNullOrEmpty(barcodeText)) {
                BarcodeReceivedEventArgs args = new BarcodeReceivedEventArgs();
                args.BarcodeText = barcodeText;
                args.TimeReceived = DateTime.Now;
                OnBarcodeReceived(args);
            }
            
        }
        protected static void OnBarcodeReceived( BarcodeReceivedEventArgs args) {
            BarcodeReceivedEventHandler handler = BarcodeReceived;
            BarcodeReceived?.Invoke(args); //same as below
            // if(handler != null ){
            //    handler(this, e);
            // }
        }
        public static event BarcodeReceivedEventHandler BarcodeReceived;
    }
    public class BarcodeReceivedEventArgs : EventArgs { 
        public string BarcodeText { get; set; }
        public DateTime TimeReceived { get; set; } = new DateTime();
    }
    public delegate void BarcodeReceivedEventHandler(BarcodeReceivedEventArgs args);
    public class VideoInputDevice
    {
        public string DeviceId { get; set; }
        public string GroupId { get; set; }
        public string Kind { get; set; }
        public string Label { get; set; }

    }
}
