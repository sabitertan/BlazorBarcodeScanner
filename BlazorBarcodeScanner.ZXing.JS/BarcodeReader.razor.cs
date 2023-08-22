using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorBarcodeScanner.ZXing.JS
{
    public partial class BarcodeReader : ComponentBase, IDisposable, IAsyncDisposable
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

        public string BarcodeText { get; set; }
        public string ErrorMessage { get; set; }

        public IEnumerable<VideoInputDevice> VideoInputDevices => _videoInputDevices;
        [Parameter]
        public EventCallback<IEnumerable<VideoInputDevice>> VideoInputDevicesChanged { get; set; }

        private string _SelectedVideoInputId = string.Empty;
        [Parameter]
        public EventCallback<string> SelectedVideoInputIdChanged { get; set; }

        public string SelectedVideoInputId
        {
            get => _SelectedVideoInputId;
            protected set
            {
                _SelectedVideoInputId = value;
                SelectedVideoInputIdChanged.InvokeAsync(value);
            }
        }
        
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        protected List<VideoInputDevice> _videoInputDevices;

        private BarcodeReaderInterop _backend;
        protected ElementReference _video;
        protected ElementReference _canvas;

        protected bool _DecodedPictureCapture;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender) {
                _backend = new BarcodeReaderInterop(JSRuntime);
                try
                {
                    _DecodedPictureCapture = DecodedPictureCapture;
                    await _backend.SetLastDecodedPictureFormat(DecodedPictureCapture ? "image/jpeg" : null);

                    await GetVideoInputDevicesAsync();

                    BarcodeReaderInterop.BarcodeReceived += ReceivedBarcodeText;
                    BarcodeReaderInterop.ErrorReceived += ReceivedErrorMessage;
                    BarcodeReaderInterop.DecodingStarted += DecodingStarted;
                    BarcodeReaderInterop.DecodingStopped += DecodingStopped;

                    if (StartCameraAutomatically && _videoInputDevices.Count > 0)
                    {
                        await _backend.SetVideoInputDevice(SelectedVideoInputId);
                        await StartDecoding();
                    }
                }
                catch (Exception ex)
                {
                    await ReceivedErrorMessage(new ErrorReceivedEventArgs { Message = ex.Message });
                }
            }
        }
        
        protected override async Task OnParametersSetAsync()
        {
            if (_DecodedPictureCapture != DecodedPictureCapture)
            {
                _DecodedPictureCapture = DecodedPictureCapture;
                await _backend.SetLastDecodedPictureFormat(DecodedPictureCapture ? "image/jpeg" : null);
            }
        }
        
        [Obsolete("Please use DisposeAsync")]
        public void Dispose()
        {
            DisposeAsync();
        }
        public async ValueTask DisposeAsync()
        {
            try
            {
                await StopDecoding();
                
                BarcodeReaderInterop.BarcodeReceived -= ReceivedBarcodeText;
                BarcodeReaderInterop.ErrorReceived -= ReceivedErrorMessage;
                BarcodeReaderInterop.DecodingStarted -= DecodingStarted;
                BarcodeReaderInterop.DecodingStopped -= DecodingStopped;
            }
            catch (Exception ex)
            {
                // Too late to do anything about it, but at least fail gracefully
                Console.WriteLine(ex.ToString());
            }
        }

        protected async Task GetVideoInputDevicesAsync()
        {
            _videoInputDevices = await _backend.GetVideoInputDevices("get");
            await VideoInputDevicesChanged.InvokeAsync(_videoInputDevices);
        }

        protected async Task RestartDecoding()
        {
            await StopDecoding();
            await StartDecoding();
        }

        public async Task StartDecoding()
        {
            ErrorMessage = null;
            var width = StreamWidth ?? 0;
            var height = StreamHeight ?? 0;
            await _backend.StartDecoding(_video, width, height);
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
                await OnErrorReceived.InvokeAsync(new ErrorReceivedEventArgs { Message = ex.Message });
            }
        }

        public async Task<string> Capture()
        {
            return await _backend.Capture(_canvas);
        }

        public async Task<string> CaptureLastDecodedPicture()
        {
            return await _backend.GetLastDecodedPicture();
        }

        public async Task StopDecoding()
        {
            BarcodeReaderInterop.OnBarcodeReceived(string.Empty);
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
                await OnErrorReceived.InvokeAsync(new ErrorReceivedEventArgs { Message = ex.Message });
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
            await _backend.ToggleTorch();
        }

        private async Task ToggleTorchSafe()
        {
            try
            {
                await ToggleTorch();
            }
            catch (Exception ex)
            {
                await OnErrorReceived.InvokeAsync(new ErrorReceivedEventArgs { Message = ex.Message });
            }
        }

        public async Task TorchOn()
        {
            await _backend.SetTorchOn();
        }

        public async Task TorchOff()
        {
            await _backend.SetTorchOff();
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
            return Task.CompletedTask;
        }
        private Task DecodingStopped(DecodingActionEventArgs _)
        {
            IsDecoding = false;
            return Task.CompletedTask;
        }

        protected async Task ChangeVideoInputSource(string deviceId)
        {
            SelectedVideoInputId = deviceId;
            await _backend.SetVideoInputDevice(deviceId);
            await RestartDecoding();
        }

        protected async Task OnVideoInputSourceChanged(ChangeEventArgs args)
        {
            try
            {
                await ChangeVideoInputSource(args.Value.ToString());
            }
            catch (Exception ex)
            {
                await OnErrorReceived.InvokeAsync(new ErrorReceivedEventArgs { Message = ex.Message });
            }
        }
    }
}
