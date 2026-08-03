using BlazorBarcodeScanner.ZXing.Cpp.Exceptions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorBarcodeScanner.ZXing.Cpp
{
    /// <summary>
    /// Wraps the zxing-cpp WebAssembly reader. Every instance owns its own JS scanner
    /// object, so multiple <see cref="BarcodeReader"/> components can live on one page.
    /// </summary>
    internal sealed class BarcodeReaderInterop : IAsyncDisposable
    {
        private const string JsNamespace = "BlazorBarcodeScannerZXingCpp";

        private readonly IJSRuntime _jsRuntime;
        private DotNetObjectReference<BarcodeReaderInterop>? _selfReference;
        private IJSObjectReference? _reader;
        private string _lastCode = string.Empty;

        public BarcodeReaderInterop(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        private IJSObjectReference Reader =>
            _reader ?? throw new InvalidOperationException($"{nameof(BarcodeReaderInterop)} has not been initialized yet.");

        public async Task InitializeAsync()
        {
            if (_reader is not null)
            {
                return;
            }

            _selfReference = DotNetObjectReference.Create(this);
            _reader = await _jsRuntime.InvokeAsync<IJSObjectReference>($"{JsNamespace}.createReader", _selfReference);
        }

        public ValueTask<List<VideoInputDevice>> GetVideoInputDevices()
        {
            return _jsRuntime.InvokeAsync<List<VideoInputDevice>>($"{JsNamespace}.listVideoInputDevices");
        }

        public async Task StartDecoding(ElementReference video, ElementReference overlay, int width, int height)
        {
            await SetVideoResolution(width, height);
            await StartDecoding(video, overlay);
        }

        public async Task StartDecoding(ElementReference video, ElementReference overlay)
        {
            try
            {
                await Reader.InvokeVoidAsync("startDecoding", video, overlay);
            }
            catch (JSException e)
            {
                if (e.Message.IndexOf("Permission denied") > -1 || e.Message.IndexOf("The request is not allowed by the user agent") > -1)
                {
                    await RaiseError("Camera access is blocked. Please give access to the camera to use the barcode scanner.");
                }
                else
                {
                    throw new StartDecodingFailedException(e.Message, e);
                }
            }
        }

        public async Task StopDecoding()
        {
            /* Forget the previous hit so scanning the same code again after a restart still raises the event. */
            _lastCode = string.Empty;
            await Reader.InvokeVoidAsync("stopDecoding");
        }

        public async Task SetVideoInputDevice(string deviceId)
        {
            await Reader.InvokeVoidAsync("setSelectedDeviceId", deviceId);
        }

        public async Task<string> GetVideoInputDevice()
        {
            return await Reader.InvokeAsync<string>("getSelectedDeviceId");
        }

        public async Task SetVideoResolution(int width, int height)
        {
            await Reader.InvokeVoidAsync("setVideoResolution", width, height);
        }

        /// <param name="formats">
        /// Pipe separated zxing-cpp format names, for example <c>QRCode|EAN-13</c>.
        /// An empty string enables every supported format.
        /// </param>
        /// <param name="tryHarder">Spend more time per frame to find codes that are rotated, blurry or low contrast.</param>
        /// <param name="scanIntervalMilliseconds">Minimum time between two decode attempts.</param>
        public async Task SetDecodeOptions(string formats, bool tryHarder, int scanIntervalMilliseconds)
        {
            await Reader.InvokeVoidAsync("setDecodeOptions", formats, tryHarder, scanIntervalMilliseconds);
        }

        public async Task SetTorchOn()
        {
            await Reader.InvokeVoidAsync("setTorchOn");
        }

        public async Task SetTorchOff()
        {
            await Reader.InvokeVoidAsync("setTorchOff");
        }

        public async Task ToggleTorch()
        {
            await Reader.InvokeVoidAsync("toggleTorch");
        }

        public async Task<string> Capture()
        {
            return await Reader.InvokeAsync<string>("capture", "image/jpeg");
        }

        public async Task SetLastDecodedPictureFormat(string? format)
        {
            await Reader.InvokeVoidAsync("setLastDecodedPictureFormat", format);
        }

        public async Task<string> GetLastDecodedPicture()
        {
            return await Reader.InvokeAsync<string>("pictureGetBase64", "decoded");
        }

        [JSInvokable]
        public async Task OnBarcodeReceived(string barcodeText)
        {
            if (string.IsNullOrEmpty(barcodeText))
            {
                return;
            }

            /* Debounce code */
            if (barcodeText == _lastCode)
            {
                return;
            }
            _lastCode = barcodeText;

            var args = new BarcodeReceivedEventArgs
            {
                BarcodeText = barcodeText,
                TimeReceived = DateTime.Now,
            };

            await Raise(BarcodeReceived, args);
        }

        [JSInvokable]
        public async Task OnErrorReceived(string message)
        {
            await RaiseError(message);
        }

        [JSInvokable]
        public void OnNotFoundReceived()
        {
            if (_lastCode.Length == 0)
            {
                return;
            }

            _lastCode = string.Empty;
            BarcodeNotFound?.Invoke();
        }

        [JSInvokable]
        public async Task OnDecodingStarted(string deviceId)
        {
            await Raise(DecodingStarted, new DecodingActionEventArgs { DeviceId = deviceId });
        }

        [JSInvokable]
        public async Task OnDecodingStopped(string deviceId)
        {
            await Raise(DecodingStopped, new DecodingActionEventArgs { DeviceId = deviceId });
        }

        internal async Task RaiseError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            await Raise(ErrorReceived, new ErrorReceivedEventArgs { Message = message });
        }

        private static Task Raise(BarcodeReceivedEventHandler? handler, BarcodeReceivedEventArgs args) =>
            handler is null ? Task.CompletedTask : Task.WhenAll(handler.GetInvocationList().Cast<BarcodeReceivedEventHandler>().Select(h => h(args)));

        private static Task Raise(ErrorReceivedEventHandler? handler, ErrorReceivedEventArgs args) =>
            handler is null ? Task.CompletedTask : Task.WhenAll(handler.GetInvocationList().Cast<ErrorReceivedEventHandler>().Select(h => h(args)));

        private static Task Raise(DecodingStartedEventHandler? handler, DecodingActionEventArgs args) =>
            handler is null ? Task.CompletedTask : Task.WhenAll(handler.GetInvocationList().Cast<DecodingStartedEventHandler>().Select(h => h(args)));

        private static Task Raise(DecodingStoppedEventHandler? handler, DecodingActionEventArgs args) =>
            handler is null ? Task.CompletedTask : Task.WhenAll(handler.GetInvocationList().Cast<DecodingStoppedEventHandler>().Select(h => h(args)));

        public async ValueTask DisposeAsync()
        {
            if (_reader is not null)
            {
                try
                {
                    await _reader.InvokeVoidAsync("dispose");
                    await _reader.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    /* The browser is already gone - nothing left to clean up. */
                }
                catch (ObjectDisposedException)
                {
                }

                _reader = null;
            }

            _selfReference?.Dispose();
            _selfReference = null;
        }

        public event BarcodeReceivedEventHandler? BarcodeReceived;
        public event ErrorReceivedEventHandler? ErrorReceived;

        public event DecodingStartedEventHandler? DecodingStarted;
        public event DecodingStoppedEventHandler? DecodingStopped;

        public event Action? BarcodeNotFound;
    }

    public class ErrorReceivedEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
    }

    public delegate Task ErrorReceivedEventHandler(ErrorReceivedEventArgs args);

    public class BarcodeReceivedEventArgs : EventArgs
    {
        public string BarcodeText { get; set; } = string.Empty;
        public DateTime TimeReceived { get; set; } = new DateTime();
    }

    public delegate Task BarcodeReceivedEventHandler(BarcodeReceivedEventArgs args);

    public class DecodingActionEventArgs : EventArgs
    {
        public string DeviceId { get; set; } = string.Empty;
    }

    public delegate Task DecodingStartedEventHandler(DecodingActionEventArgs args);

    public delegate Task DecodingStoppedEventHandler(DecodingActionEventArgs args);

    public class VideoInputDevice
    {
        public string DeviceId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
