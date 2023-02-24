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
        public bool DecodedPictureCapture { get; set; } = false;

        [Parameter]
        public string Title { get; set; } = "Scan Barcode from Camera";

        [Parameter]
        public bool StartCameraAutomatically { get; set; } = false;

        [Parameter]
        public bool ShowStart { get; set; } = true;

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
            get
            {
                return _isDecoding;
            }

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

        public string SelectedVideoInputId { get; private set; } = string.Empty;
        
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        private List<VideoInputDevice> _videoInputDevices;

        private BarcodeReaderInterop _backend;
        private ElementReference _video;
        private ElementReference _canvas;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender) {
                _backend = new BarcodeReaderInterop(JSRuntime);
                try
                {
                    await _backend.SetLastDecodedPictureFormat(DecodedPictureCapture ? "image/jpeg" : null);

                    await GetVideoInputDevicesAsync();

                    BarcodeReaderInterop.BarcodeReceived += ReceivedBarcodeText;
                    BarcodeReaderInterop.ErrorReceived += ReceivedErrorMessage;
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
        
        [Obsolete("Please use DisposeAsync")]
        public void Dispose()
        {
            StopDecoding();
        }
        public async ValueTask DisposeAsync()
        {
            try
            {
                await StopDecoding();
            }
            catch (Exception ex)
            {
                // Too late to do anything about it, but at least fail gracefully
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task GetVideoInputDevicesAsync()
        {
            _videoInputDevices = await _backend.GetVideoInputDevices("get");
        }

        private async Task RestartDecoding()
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
            IsDecoding = true;
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
            IsDecoding = false;
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

        protected async Task ChangeVideoInputSource(string deviceId)
        {
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
