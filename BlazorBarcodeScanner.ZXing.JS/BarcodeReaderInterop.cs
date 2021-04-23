using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorBarcodeScanner.ZXing.JS
{
    internal class BarcodeReaderInterop
    {
        private IJSRuntime jSRuntime;

        public BarcodeReaderInterop(IJSRuntime runtime)
        {
            jSRuntime = runtime;
        }

        public ValueTask<List<VideoInputDevice>> GetVideoInputDevices(string message)
        {
            // Implemented in BlazorBarcodeScannerJsInterop.js

            return jSRuntime.InvokeAsync<List<VideoInputDevice>>(
                "BlazorBarcodeScanner.listVideoInputDevices",
                message);
        }

        public void StartDecoding(string videoElementId, int width, int height)
        {
            SetVideoResolution(width, height);
            StartDecoding(videoElementId);
        }

        public void StartDecoding(string videoElementId)
        {
            jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.startDecoding", videoElementId);
        }

        public void StopDecoding()
        {
            jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.stopDecoding");
        }

        public void SetVideoInputDevice(string deviceId)
        {
            jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.setSelectedDeviceId", deviceId);
        }

        public void SetVideoResolution(int width, int height)
        {
            jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.setVideoResolution", width, height);
        }

        public void SetTorchOn()
        {
            jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.setTorchOn");
        }

        public void SetTorchOff()
        {
            jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.setTorchOff");
        }

        public void ToggleTorch()
        {
            jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.toggleTorch");
        }

        public static void OnBarcodeReceived(string barcodeText)
        {
            if (string.IsNullOrEmpty(barcodeText))
            {
                return;
            }

            BarcodeReceivedEventArgs args = new BarcodeReceivedEventArgs()
            {
                BarcodeText = barcodeText,
                TimeReceived = DateTime.Now,
            };

            JsInteropClass.OnBarcodeReceived(args);
            BarcodeReceived?.Invoke(args);
        }

        public static event BarcodeReceivedEventHandler BarcodeReceived;
    }

    public class BarcodeReceivedEventArgs : EventArgs
    {
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
