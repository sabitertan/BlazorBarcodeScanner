using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorBarcodeScanner.ZXing.JS
{
    internal class BarcodeReaderInterop
    {
        private readonly IJSRuntime jSRuntime;

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

        public void StartDecoding(ElementReference video, int width, int height)
        {
            SetVideoResolution(width, height);
            StartDecoding(video);
        }

        public async void StartDecoding(ElementReference video)
        {
            await jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.startDecoding", video);
        }

        public async void StopDecoding()
        {
            await  jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.stopDecoding");
        }

        public async void SetVideoInputDevice(string deviceId)
        {
            await  jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.setSelectedDeviceId", deviceId);
        }

        public async Task<string> GetVideoInputDevice()
        {
            return await jSRuntime.InvokeAsync<string>("BlazorBarcodeScanner.getSelectedDeviceId");
        }

        public async void SetVideoResolution(int width, int height)
        {
            await  jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.setVideoResolution", width, height);
        }

        public async void SetTorchOn()
        {
            await  jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.setTorchOn");
        }

        public async void SetTorchOff()
        {
            await jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.setTorchOff");
        }

        public async void ToggleTorch()
        {
            await jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.toggleTorch");
        }

        public async Task<string> Capture(ElementReference canvas)
        {
            var result = string.Empty;

            await jSRuntime.InvokeVoidAsync("BlazorBarcodeScanner.capture", "image/jpeg", canvas);

            /* 
             * Due to the size of the expected images, on .NET Core 5.0.5 it proved beneficial to 
             * transfer the string unmarshalled rather than having it packed through the standard 
             * mechanisms. Brief benchmarks on a recent PC over three FullHD snapshots in a row 
             * yielded following results (in milliseconds):
             * 
             *  Edge 90.0.818.42:
             *      Capturing:                  309     217     336     389
             *      Transfer:   Marshalled      600     534     638     618
             *                  Unmarshalled      9        3     10       4
             *
             *  Chrome 90.0.4430.85:
             *      Capturing:                  334     231     338     233
             *      Transfer:   Marshalled      571     453     466     451
             *                  Unmarshalled     11       5      11       2
             *                  
             * As a consequence we try to use the unmarshalled path as often as possible.
             */
#if !NETSTANDARD2_1
            if (jSRuntime is IJSUnmarshalledRuntime jS)
            {
                result = CaptureGetUnMarshalled(jS);
            }
            else
            {
                result = await CaptureGetMarshalled();
            }
#else
            result = await CaptureGetMarshalled();
#endif
            return result;
        }

        private async Task<string> CaptureGetMarshalled()
        {
            return await jSRuntime.InvokeAsync<string>("BlazorBarcodeScanner.pictureGetBase64");
        }

#if !NETSTANDARD2_1
        private static string CaptureGetUnMarshalled(IJSUnmarshalledRuntime jS)
        {
            return jS.InvokeUnmarshalled<string>("BlazorBarcodeScanner.pictureGetBase64Unmarshalled");
        }
#endif 
        private static string lastCode = string.Empty;
        public static void OnBarcodeReceived(string barcodeText)
        {
            if (string.IsNullOrEmpty(barcodeText))
            {
                return;
            }
            /* Debounce code */
            if (barcodeText == lastCode)
            {
                return;
            }
            lastCode = barcodeText;
            BarcodeReceivedEventArgs args = new BarcodeReceivedEventArgs()
            {
                BarcodeText = barcodeText,
                TimeReceived = DateTime.Now,
            };

            JsInteropClass.OnBarcodeReceived(args);
            BarcodeReceived?.Invoke(args);
        }

        public static void OnNotFoundReceived()
        {
            if (!string.IsNullOrEmpty(lastCode))
            {
                lastCode = string.Empty;
                BarcodeNotFound?.Invoke();
            }
        }

        public static event BarcodeReceivedEventHandler BarcodeReceived;
        public static event Action BarcodeNotFound;
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
