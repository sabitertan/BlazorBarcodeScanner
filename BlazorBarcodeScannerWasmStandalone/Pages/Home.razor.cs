using Microsoft.AspNetCore.Components.Web;
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

        private ScannerEngine Engine { get; set; } = ScannerEngine.ZXingJs;

        private JsScanner.BarcodeReader? _jsReader;
        private CppScanner.BarcodeReader? _cppReader;

        private int StreamWidth = 720;
        private int StreamHeight = 540;

        private string LocalBarcodeText = string.Empty;
        private int _currentVideoSourceIdx = 0;

        private string _imgSrc = string.Empty;
        private string _lastError = string.Empty;

        private static string EngineName(ScannerEngine engine) => engine switch
        {
            ScannerEngine.ZXingJs => "zxing-js",
            _ => "zxing-cpp",
        };

        private static string EngineDescription(ScannerEngine engine) => engine switch
        {
            ScannerEngine.ZXingJs => "BlazorBarcodeScanner.ZXing.JS - the pure JavaScript zxing-js/library port.",
            _ => "BlazorBarcodeScanner.ZXing.Cpp - the zxing-cpp reader compiled to WebAssembly.",
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

        private int VideoInputCount => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.VideoInputDevices?.Count() ?? 0
            : _cppReader?.VideoInputDevices?.Count() ?? 0;

        private int SourceIndexFromId()
        {
            var deviceIds = Engine == ScannerEngine.ZXingJs
                ? _jsReader?.VideoInputDevices.Select(d => d.DeviceId).ToList()
                : _cppReader?.VideoInputDevices.Select(d => d.DeviceId).ToList();

            if (deviceIds is null)
            {
                return 0;
            }

            var index = deviceIds.IndexOf(SelectedVideoInputId);
            return index < 0 ? deviceIds.Count : index;
        }

        private Task StartDecoding() => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.StartDecoding() ?? Task.CompletedTask
            : _cppReader?.StartDecoding() ?? Task.CompletedTask;

        private Task StopDecoding() => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.StopDecoding() ?? Task.CompletedTask
            : _cppReader?.StopDecoding() ?? Task.CompletedTask;

        private Task UpdateResolution() => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.UpdateResolution() ?? Task.CompletedTask
            : _cppReader?.UpdateResolution() ?? Task.CompletedTask;

        private Task ToggleTorch() => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.ToggleTorch() ?? Task.CompletedTask
            : _cppReader?.ToggleTorch() ?? Task.CompletedTask;

        private Task TorchOn() => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.TorchOn() ?? Task.CompletedTask
            : _cppReader?.TorchOn() ?? Task.CompletedTask;

        private Task TorchOff() => Engine == ScannerEngine.ZXingJs
            ? _jsReader?.TorchOff() ?? Task.CompletedTask
            : _cppReader?.TorchOff() ?? Task.CompletedTask;

        private Task LocalReceivedBarcodeText(JsScanner.BarcodeReceivedEventArgs args) => ReceivedBarcodeText(args.BarcodeText);

        private Task LocalReceivedBarcodeText(CppScanner.BarcodeReceivedEventArgs args) => ReceivedBarcodeText(args.BarcodeText);

        private async Task ReceivedBarcodeText(string barcodeText)
        {
            LocalBarcodeText = barcodeText;
            await StopDecoding();
        }

        private void LocalReceivedError(JsScanner.ErrorReceivedEventArgs args) => _lastError = args.Message;

        private void LocalReceivedError(CppScanner.ErrorReceivedEventArgs args) => _lastError = args.Message;

        private async Task CapturePicture()
        {
            _imgSrc = Engine == ScannerEngine.ZXingJs
                ? await (_jsReader?.Capture() ?? Task.FromResult(string.Empty))
                : await (_cppReader?.Capture() ?? Task.FromResult(string.Empty));

            StateHasChanged();
        }

        private async Task OnVideoSourceNext(MouseEventArgs args)
        {
            var count = VideoInputCount;
            if (count == 0)
            {
                return;
            }

            _currentVideoSourceIdx++;
            if (_currentVideoSourceIdx >= count)
            {
                _currentVideoSourceIdx = 0;
            }

            if (Engine == ScannerEngine.ZXingJs && _jsReader is not null)
            {
                await _jsReader.SelectVideoInput(_jsReader.VideoInputDevices.ElementAt(_currentVideoSourceIdx));
            }
            else if (_cppReader is not null)
            {
                await _cppReader.SelectVideoInput(_cppReader.VideoInputDevices.ElementAt(_currentVideoSourceIdx));
            }
        }
    }
}
