using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorBarcodeScanner.ZXing.Cpp
{
    public partial class BarcodeReader : ComponentBase, IAsyncDisposable
    {
        [Parameter]
        public string TextWithoutDevices { get; set; } = "looking for devices";

        [Parameter]
        public string LabelVideoDeviceListText { get; set; } = "Change video source:";

        [Parameter]
        public string ButtonStartText { get; set; } = "Start";

        [Parameter]
        public string ButtonResetText { get; set; } = "Reset";

        [Parameter]
        public string ButtonStopText { get; set; } = "Stop";

        [Parameter]
        public string ButtonToggleTorchText { get; set; } = "Toggle Torch";

        [Parameter]
        public bool DecodedPictureCapture { get; set; } = false;

        [Parameter]
        public string Title { get; set; } = "Scan Barcode from Camera";

        [Parameter]
        public bool StartCameraAutomatically { get; set; } = false;

        [Parameter]
        public bool ShowStart { get; set; } = true;

        [Parameter]
        public bool ShowStop { get; set; } = true;

        [Parameter]
        public bool ShowReset { get; set; } = true;

        [Parameter]
        public bool ShowToggleTorch { get; set; } = true;

        [Parameter]
        public bool ShowResult { get; set; } = true;

        [Parameter]
        public bool ShowVideoDeviceList { get; set; } = true;

        [Parameter]
        public int VideoWidth { get; set; } = 300;

        [Parameter]
        public int VideoHeight { get; set; } = 200;

        [Parameter]
        public bool FullWidthVideo { get; set; } = false;

        [Parameter]
        public int? StreamHeight { get; set; } = null;

        [Parameter]
        public int? StreamWidth { get; set; } = null;

        /// <summary>
        /// Pipe separated list of zxing-cpp format names to look for, for example
        /// <c>QRCode|EAN-13</c>. Empty (the default) enables every supported format.
        /// </summary>
        [Parameter]
        public string Formats { get; set; } = string.Empty;

        /// <summary>
        /// Spend more time per frame to also find codes that are rotated, blurry or low contrast.
        /// </summary>
        [Parameter]
        public bool TryHarder { get; set; } = true;

        /// <summary>
        /// Minimum time between two decode attempts. Lower values react faster at the cost of CPU.
        /// </summary>
        [Parameter]
        public int ScanIntervalMilliseconds { get; set; } = 100;

        [Parameter]
        public EventCallback<BarcodeReceivedEventArgs> OnBarcodeReceived { get; set; }

        [Parameter]
        public EventCallback<ErrorReceivedEventArgs> OnErrorReceived { get; set; }

        [Parameter]
        public EventCallback<DecodingChangedArgs> OnDecodingChanged { get; set; }

        private bool _isDecoding = false;

        public bool IsDecoding
        {
            get => _isDecoding;
            protected set
            {
                var hasChanged = _isDecoding != value;

                _isDecoding = value;
                if (hasChanged)
                {
                    var args = new DecodingChangedArgs()
                    {
                        Sender = this,
                        IsDecoding = _isDecoding,
                    };
                    OnDecodingChanged.InvokeAsync(args);
                }
            }
        }

        public string BarcodeText { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }

        public IEnumerable<VideoInputDevice> VideoInputDevices => _videoInputDevices ?? Enumerable.Empty<VideoInputDevice>();

        [Parameter]
        public EventCallback<IEnumerable<VideoInputDevice>> VideoInputDevicesChanged { get; set; }

        private string _selectedVideoInputId = string.Empty;

        [Parameter]
        public EventCallback<string> SelectedVideoInputIdChanged { get; set; }

        public string SelectedVideoInputId
        {
            get => _selectedVideoInputId;
            protected set
            {
                _selectedVideoInputId = value;
                SelectedVideoInputIdChanged.InvokeAsync(value);
            }
        }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        protected List<VideoInputDevice>? _videoInputDevices;

        private BarcodeReaderInterop? _backend;
        protected ElementReference _video;
        protected ElementReference _overlay;

        private bool _decodedPictureCapture;
        private string _formats = string.Empty;
        private bool _tryHarder = true;
        private int _scanIntervalMilliseconds = 100;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            var backend = new BarcodeReaderInterop(JSRuntime);
            _backend = backend;

            try
            {
                await backend.InitializeAsync();

                _decodedPictureCapture = DecodedPictureCapture;
                await backend.SetLastDecodedPictureFormat(DecodedPictureCapture ? "image/jpeg" : null);
                await ApplyDecodeOptionsAsync();

                await GetVideoInputDevicesAsync();

                backend.BarcodeReceived += ReceivedBarcodeText;
                backend.ErrorReceived += ReceivedErrorMessage;
                backend.DecodingStarted += DecodingStarted;
                backend.DecodingStopped += DecodingStopped;

                if (StartCameraAutomatically && _videoInputDevices?.Count > 0)
                {
                    await backend.SetVideoInputDevice(SelectedVideoInputId);
                    await StartDecoding();
                }
            }
            catch (Exception ex)
            {
                await ReceivedErrorMessage(new ErrorReceivedEventArgs { Message = ex.Message });
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            /* Before the first render there is no JS counterpart to configure yet -
             * OnAfterRenderAsync pushes the initial values. */
            if (_backend is null)
            {
                return;
            }

            if (_decodedPictureCapture != DecodedPictureCapture)
            {
                _decodedPictureCapture = DecodedPictureCapture;
                await _backend.SetLastDecodedPictureFormat(DecodedPictureCapture ? "image/jpeg" : null);
            }

            if (_formats != Formats || _tryHarder != TryHarder || _scanIntervalMilliseconds != ScanIntervalMilliseconds)
            {
                await ApplyDecodeOptionsAsync();
            }
        }

        private async Task ApplyDecodeOptionsAsync()
        {
            _formats = Formats;
            _tryHarder = TryHarder;
            _scanIntervalMilliseconds = ScanIntervalMilliseconds;

            if (_backend is not null)
            {
                await _backend.SetDecodeOptions(_formats, _tryHarder, _scanIntervalMilliseconds);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_backend is null)
            {
                return;
            }

            try
            {
                _backend.BarcodeReceived -= ReceivedBarcodeText;
                _backend.ErrorReceived -= ReceivedErrorMessage;
                _backend.DecodingStarted -= DecodingStarted;
                _backend.DecodingStopped -= DecodingStopped;

                await _backend.DisposeAsync();
            }
            catch (Exception ex)
            {
                // Too late to do anything about it, but at least fail gracefully
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                _backend = null;
            }

            GC.SuppressFinalize(this);
        }

        protected async Task GetVideoInputDevicesAsync()
        {
            if (_backend is null)
            {
                return;
            }

            _videoInputDevices = await _backend.GetVideoInputDevices();
            await VideoInputDevicesChanged.InvokeAsync(_videoInputDevices);

            /* Blazor does not re-render on its own once OnAfterRenderAsync completes, so the
             * freshly discovered cameras would otherwise never reach the markup. */
            StateHasChanged();
        }

        protected async Task RestartDecoding()
        {
            await StopDecoding();
            await StartDecoding();
        }

        public async Task StartDecoding()
        {
            if (_backend is null)
            {
                return;
            }

            ErrorMessage = null;
            var width = StreamWidth ?? 0;
            var height = StreamHeight ?? 0;
            await _backend.StartDecoding(_video, _overlay, width, height);
            SelectedVideoInputId = await _backend.GetVideoInputDevice();
            StateHasChanged();
        }

        private async Task StartDecodingSafe()
        {
            try
            {
                await StartDecoding();
            }
            catch (Exception ex)
            {
                await ReceivedErrorMessage(new ErrorReceivedEventArgs { Message = ex.Message });
            }
        }

        public async Task<string> Capture()
        {
            return _backend is null ? string.Empty : await _backend.Capture();
        }

        public async Task<string> CaptureLastDecodedPicture()
        {
            return _backend is null ? string.Empty : await _backend.GetLastDecodedPicture();
        }

        public async Task StopDecoding()
        {
            if (_backend is null)
            {
                return;
            }

            await _backend.StopDecoding();
            StateHasChanged();
        }

        private async Task StopDecodingSafe()
        {
            try
            {
                await StopDecoding();
            }
            catch (Exception ex)
            {
                await ReceivedErrorMessage(new ErrorReceivedEventArgs { Message = ex.Message });
            }
        }

        private async Task RestartDecodingSafe()
        {
            await StopDecodingSafe();
            await StartDecodingSafe();
        }

        public async Task UpdateResolution()
        {
            await RestartDecoding();
        }

        public async Task ToggleTorch()
        {
            if (_backend is not null)
            {
                await _backend.ToggleTorch();
            }
        }

        private async Task ToggleTorchSafe()
        {
            try
            {
                await ToggleTorch();
            }
            catch (Exception ex)
            {
                await ReceivedErrorMessage(new ErrorReceivedEventArgs { Message = ex.Message });
            }
        }

        public async Task TorchOn()
        {
            if (_backend is not null)
            {
                await _backend.SetTorchOn();
            }
        }

        public async Task TorchOff()
        {
            if (_backend is not null)
            {
                await _backend.SetTorchOff();
            }
        }

        public async Task SelectVideoInput(VideoInputDevice device)
        {
            await ChangeVideoInputSource(device.DeviceId);
        }

        private async Task ReceivedBarcodeText(BarcodeReceivedEventArgs args)
        {
            BarcodeText = args.BarcodeText;
            await OnBarcodeReceived.InvokeAsync(args);
            StateHasChanged();
        }

        private async Task ReceivedErrorMessage(ErrorReceivedEventArgs args)
        {
            ErrorMessage = args.Message;
            await OnErrorReceived.InvokeAsync(args);
            StateHasChanged();
        }

        private Task DecodingStarted(DecodingActionEventArgs _)
        {
            IsDecoding = true;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private Task DecodingStopped(DecodingActionEventArgs _)
        {
            IsDecoding = false;
            StateHasChanged();
            return Task.CompletedTask;
        }

        protected async Task ChangeVideoInputSource(string deviceId)
        {
            if (_backend is null)
            {
                return;
            }

            SelectedVideoInputId = deviceId;
            await _backend.SetVideoInputDevice(deviceId);
            await RestartDecoding();
        }

        protected async Task OnVideoInputSourceChanged(ChangeEventArgs args)
        {
            try
            {
                await ChangeVideoInputSource(args.Value?.ToString() ?? string.Empty);
            }
            catch (Exception ex)
            {
                await ReceivedErrorMessage(new ErrorReceivedEventArgs { Message = ex.Message });
            }
        }
    }
}
