using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Xunit;
using CppScanner = BlazorBarcodeScanner.ZXing.Cpp;

namespace BlazorBarcodeScanner.Tests
{
    public class ZXingCppBarcodeReaderContractTests : BarcodeReaderContractTests<CppScanner.BarcodeReader>
    {
        protected override string ListVideoInputDevicesIdentifier => "BlazorBarcodeScannerZXingCpp.listVideoInputDevices";

        protected override void SetupVideoInputDevices(params (string DeviceId, string Label)[] devices)
        {
            JSInterop
                .Setup<List<CppScanner.VideoInputDevice>>(ListVideoInputDevicesIdentifier)
                .SetResult(devices
                    .Select(d => new CppScanner.VideoInputDevice { DeviceId = d.DeviceId, Label = d.Label, Kind = "videoinput" })
                    .ToList());
        }

        protected override object CreateVideoInputDevicesChangedCallback(Action onInvoked)
            => EventCallback.Factory.Create<IEnumerable<CppScanner.VideoInputDevice>>(this, _ => onInvoked());

        [Fact]
        public void Creates_one_JS_reader_per_component()
        {
            Render<CppScanner.BarcodeReader>();
            Render<CppScanner.BarcodeReader>();

            Assert.Equal(2, JSInterop.Invocations.Count(i => i.Identifier == "BlazorBarcodeScannerZXingCpp.createReader"));
        }

        [Fact]
        public void Renders_the_detection_overlay_on_top_of_the_video()
        {
            var cut = Render<CppScanner.BarcodeReader>();

            Assert.NotNull(cut.Find("div.zxing-video-container canvas.zxing-overlay"));
        }
    }

    public class ZXingCppBarcodeReaderInteropTests : BunitContext
    {
        private async Task<CppScanner.BarcodeReaderInterop> CreateInteropAsync()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            var interop = new CppScanner.BarcodeReaderInterop(JSInterop.JSRuntime);
            await interop.InitializeAsync();
            return interop;
        }

        [Fact]
        public async Task Creates_the_JS_reader_once()
        {
            var interop = await CreateInteropAsync();

            await interop.InitializeAsync();

            Assert.Equal(1, JSInterop.Invocations.Count(i => i.Identifier == "BlazorBarcodeScannerZXingCpp.createReader"));
        }

        [Fact]
        public async Task Throws_when_used_before_initialization()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            var interop = new CppScanner.BarcodeReaderInterop(JSInterop.JSRuntime);

            await Assert.ThrowsAsync<InvalidOperationException>(() => interop.StopDecoding());
        }

        [Fact]
        public async Task Raises_BarcodeReceived_for_a_new_code()
        {
            var interop = await CreateInteropAsync();
            var received = new List<string>();
            interop.BarcodeReceived += args => { received.Add(args.BarcodeText); return Task.CompletedTask; };

            await interop.OnBarcodeReceived("4006381333931");

            Assert.Equal(["4006381333931"], received);
        }

        [Fact]
        public async Task Debounces_a_repeated_code()
        {
            var interop = await CreateInteropAsync();
            var received = new List<string>();
            interop.BarcodeReceived += args => { received.Add(args.BarcodeText); return Task.CompletedTask; };

            await interop.OnBarcodeReceived("same");
            await interop.OnBarcodeReceived("same");
            await interop.OnBarcodeReceived("other");

            Assert.Equal(["same", "other"], received);
        }

        [Fact]
        public async Task Ignores_an_empty_code()
        {
            var interop = await CreateInteropAsync();
            var received = 0;
            interop.BarcodeReceived += _ => { received++; return Task.CompletedTask; };

            await interop.OnBarcodeReceived(string.Empty);

            Assert.Equal(0, received);
        }

        [Fact]
        public async Task Raises_BarcodeNotFound_only_after_a_code_was_seen()
        {
            var interop = await CreateInteropAsync();
            var notFound = 0;
            interop.BarcodeNotFound += () => notFound++;

            interop.OnNotFoundReceived();
            Assert.Equal(0, notFound);

            await interop.OnBarcodeReceived("something");
            interop.OnNotFoundReceived();
            interop.OnNotFoundReceived();

            Assert.Equal(1, notFound);
        }

        [Fact]
        public async Task Forgets_the_last_code_when_decoding_stops()
        {
            var interop = await CreateInteropAsync();
            var received = new List<string>();
            interop.BarcodeReceived += args => { received.Add(args.BarcodeText); return Task.CompletedTask; };

            await interop.OnBarcodeReceived("same");
            await interop.StopDecoding();
            await interop.OnBarcodeReceived("same");

            Assert.Equal(["same", "same"], received);
        }

        [Fact]
        public async Task Reports_errors_from_the_JS_layer()
        {
            var interop = await CreateInteropAsync();
            var messages = new List<string>();
            interop.ErrorReceived += args => { messages.Add(args.Message); return Task.CompletedTask; };

            await interop.OnErrorReceived("checksum failed");
            await interop.OnErrorReceived(string.Empty);

            Assert.Equal(["checksum failed"], messages);
        }

        [Fact]
        public async Task Reports_decoding_start_and_stop()
        {
            var interop = await CreateInteropAsync();
            var started = new List<string>();
            var stopped = new List<string>();
            interop.DecodingStarted += args => { started.Add(args.DeviceId); return Task.CompletedTask; };
            interop.DecodingStopped += args => { stopped.Add(args.DeviceId); return Task.CompletedTask; };

            await interop.OnDecodingStarted("camera-1");
            await interop.OnDecodingStopped("camera-1");

            Assert.Equal(["camera-1"], started);
            Assert.Equal(["camera-1"], stopped);
        }

        [Fact]
        public async Task StartDecoding_sets_the_resolution_before_starting()
        {
            var interop = await CreateInteropAsync();

            await interop.StartDecoding(default, default, 1280, 720);

            var identifiers = ReaderInvocations().ToList();
            var resolution = identifiers.IndexOf("setVideoResolution");
            var start = identifiers.IndexOf("startDecoding");

            Assert.True(resolution >= 0 && start > resolution, string.Join(", ", identifiers));
        }

        [Fact]
        public async Task Pushes_the_decode_options_to_the_JS_layer()
        {
            var interop = await CreateInteropAsync();

            await interop.SetDecodeOptions("QRCode|EAN-13", tryHarder: false, scanIntervalMilliseconds: 250);

            var arguments = ReaderInvocation("setDecodeOptions").Arguments;

            Assert.Equal(["QRCode|EAN-13", false, 250], arguments);
        }

        [Fact]
        public async Task Torch_helpers_map_to_their_JS_counterparts()
        {
            var interop = await CreateInteropAsync();

            await interop.SetTorchOn();
            await interop.SetTorchOff();
            await interop.ToggleTorch();

            Assert.Contains("setTorchOn", ReaderInvocations());
            Assert.Contains("setTorchOff", ReaderInvocations());
            Assert.Contains("toggleTorch", ReaderInvocations());
        }

        [Fact]
        public async Task Disposing_releases_the_JS_reader()
        {
            var interop = await CreateInteropAsync();

            await interop.DisposeAsync();

            Assert.Contains("dispose", ReaderInvocations());
        }

        /// <summary>
        /// Calls the interop routes through the IJSObjectReference returned by createReader
        /// are recorded under their bare method name.
        /// </summary>
        private IEnumerable<string> ReaderInvocations()
            => JSInterop.Invocations.Select(i => i.Identifier);

        private JSRuntimeInvocation ReaderInvocation(string identifier)
            => JSInterop.Invocations.Single(i => i.Identifier == identifier);
    }
}
