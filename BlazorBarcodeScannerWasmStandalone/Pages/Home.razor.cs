using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using CppScanner = BlazorBarcodeScanner.ZXing.Cpp;
using JsScanner = BlazorBarcodeScanner.ZXing.JS;

namespace BlazorBarcodeScannerWasmStandalone.Pages
{
    public enum ScannerEngine
    {
        ZXingJs,
        ZXingCpp,
    }

    /// <summary>
    /// Drives whichever of the two reader components is currently selected. Both expose the
    /// same surface, but they are distinct types, so each call has to be dispatched by engine.
    /// </summary>
    public partial class Home
    {
        private static readonly ScannerEngine[] Engines = [ScannerEngine.ZXingJs, ScannerEngine.ZXingCpp];

        private static readonly (string Value, string Label)[] FormatChoices =
        [
            ("", "Every supported format"),
            ("QRCode|MicroQRCode", "QR codes"),
            ("EAN-13|EAN-8|UPC-A|UPC-E", "Retail (EAN / UPC)"),
            ("Code128|Code39|Code93|ITF", "1D industrial"),
            ("DataMatrix|Aztec|PDF417", "2D (DataMatrix, Aztec, PDF417)"),
        ];

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        private ScannerEngine Engine { get; set; } = ScannerEngine.ZXingJs;

        private JsScanner.BarcodeReader? _jsReader;
        private CppScanner.BarcodeReader? _cppReader;

        private int StreamWidth { get; set; } = 1280;
        private int StreamHeight { get; set; } = 720;

        private string Formats { get; set; } = string.Empty;
        private bool TryHarder { get; set; } = true;
        private int ScanInterval { get; set; } = 100;
        private bool ShowBuiltInUi { get; set; }

        private bool IsScanning { get; set; }

        private string LocalBarcodeText = string.Empty;
        private int _currentVideoSourceIdx = 0;

        private string _imgSrc = string.Empty;
        private string _lastError = string.Empty;
        private bool _copied;

        private static string EngineName(ScannerEngine engine) => engine switch
        {
            ScannerEngine.ZXingJs => "zxing-js",
            _ => "zxing-cpp",
        };

        private static string EngineDescription(ScannerEngine engine) => engine switch
        {
            ScannerEngine.ZXingJs => "BlazorBarcodeScanner.ZXing.JS — the pure JavaScript zxing-js/library port.",
            _ => "BlazorBarcodeScanner.ZXing.Cpp — the zxing-cpp reader compiled to WebAssembly.",
        };

        private void SwitchEngine(ScannerEngine engine)
        {
            if (Engine == engine)
            {
                return;
            }

            Engine = engine;

            /* The previous component is removed from the render tree and disposes itself,
             * which releases the camera. Drop our references so nothing calls into it. */
            _jsReader = null;
            _cppReader = null;

            LocalBarcodeText = string.Empty;
            _lastError = string.Empty;
            _imgSrc = string.Empty;
            _copied = false;
            IsScanning = false;
            _currentVideoSourceIdx = 0;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender && !string.IsNullOrWhiteSpace(SelectedVideoInputId))
            {
                _currentVideoSourceIdx = SourceIndexFromId();
            }
        }

        private string SelectedVideoInputId => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.SelectedVideoInputId ?? string.Empty
            : _cppReader?.SelectedVideoInputId ?? string.Empty;

        private IReadOnlyList<(string Id, string Label)> VideoInputDeviceList => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.VideoInputDevices.Select(d => (d.DeviceId, d.Label)).ToList() ?? []
            : _cppReader?.VideoInputDevices.Select(d => (d.DeviceId, d.Label)).ToList() ?? [];

        private int VideoInputCount => VideoInputDeviceList.Count;

        private int SourceIndexFromId()
        {
            var index = VideoInputDeviceList.Select(d => d.Id).ToList().IndexOf(SelectedVideoInputId);
            return index < 0 ? 0 : index;
        }

        private Task StartDecoding() => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.StartDecoding() ?? Task.CompletedTask
            : _cppReader?.StartDecoding() ?? Task.CompletedTask;

        private Task StopDecoding() => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.StopDecoding() ?? Task.CompletedTask
            : _cppReader?.StopDecoding() ?? Task.CompletedTask;

        private async Task ToggleScanning()
        {
            try
            {
                if (IsScanning)
                {
                    await StopDecoding();
                }
                else
                {
                    _lastError = string.Empty;
                    await StartDecoding();
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }
        }

        private Task UpdateResolution() => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.UpdateResolution() ?? Task.CompletedTask
            : _cppReader?.UpdateResolution() ?? Task.CompletedTask;

        private Task ToggleTorch() => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.ToggleTorch() ?? Task.CompletedTask
            : _cppReader?.ToggleTorch() ?? Task.CompletedTask;

        private Task LocalReceivedBarcodeText(JsScanner.BarcodeReceivedEventArgs args) => ReceivedBarcodeText(args.BarcodeText);

        private Task LocalReceivedBarcodeText(CppScanner.BarcodeReceivedEventArgs args) => ReceivedBarcodeText(args.BarcodeText);

        private async Task ReceivedBarcodeText(string barcodeText)
        {
            LocalBarcodeText = barcodeText;
            _copied = false;
            await StopDecoding();
        }

        private void LocalReceivedError(JsScanner.ErrorReceivedEventArgs args) => _lastError = args.Message;

        private void LocalReceivedError(CppScanner.ErrorReceivedEventArgs args) => _lastError = args.Message;

        private void LocalDecodingChanged(JsScanner.DecodingChangedArgs args) => DecodingChanged(args.IsDecoding);

        private void LocalDecodingChanged(CppScanner.DecodingChangedArgs args) => DecodingChanged(args.IsDecoding);

        private void DecodingChanged(bool isDecoding)
        {
            IsScanning = isDecoding;
            StateHasChanged();
        }

        private async Task CopyResult()
        {
            try
            {
                await JS.InvokeVoidAsync("navigator.clipboard.writeText", LocalBarcodeText);
                _copied = true;
            }
            catch (JSException)
            {
                /* Clipboard access needs a secure context and user permission - not worth an error banner. */
                _copied = false;
            }
        }

        private async Task CapturePicture()
        {
            _imgSrc = Engine == ScannerEngine.ZXingJs
                ? await (_jsReader?.Capture() ?? Task.FromResult(string.Empty))
                : await (_cppReader?.Capture() ?? Task.FromResult(string.Empty));
        }

        private async Task OnCameraChanged(ChangeEventArgs args)
        {
            var deviceId = args.Value?.ToString();
            if (string.IsNullOrEmpty(deviceId))
            {
                return;
            }

            await SelectVideoInput(deviceId);
            _currentVideoSourceIdx = SourceIndexFromId();
        }

        private async Task OnVideoSourceNext()
        {
            var devices = VideoInputDeviceList;
            if (devices.Count == 0)
            {
                return;
            }

            _currentVideoSourceIdx = (_currentVideoSourceIdx + 1) % devices.Count;

            await SelectVideoInput(devices[_currentVideoSourceIdx].Id);
        }

        private async Task SelectVideoInput(string deviceId)
        {
            try
            {
                if (Engine == ScannerEngine.ZXingJs && _jsReader is not null)
                {
                    await _jsReader.SelectVideoInput(new JsScanner.VideoInputDevice { DeviceId = deviceId });
                }
                else if (_cppReader is not null)
                {
                    await _cppReader.SelectVideoInput(new CppScanner.VideoInputDevice { DeviceId = deviceId });
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }
        }
    }
}
