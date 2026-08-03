using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using JsScanner = BlazorBarcodeScanner.ZXing.JS;

namespace BlazorBarcodeScanner.Tests
{
    public class ZXingJsBarcodeReaderContractTests : BarcodeReaderContractTests<JsScanner.BarcodeReader>
    {
        protected override string ListVideoInputDevicesIdentifier => "BlazorBarcodeScanner.listVideoInputDevices";

        protected override void SetupVideoInputDevices(params (string DeviceId, string Label)[] devices)
        {
            /* The zxing-js interop passes a (otherwise unused) "get" argument along. */
            JSInterop
                .Setup<List<JsScanner.VideoInputDevice>>(ListVideoInputDevicesIdentifier, "get")
                .SetResult(devices
                    .Select(d => new JsScanner.VideoInputDevice { DeviceId = d.DeviceId, Label = d.Label, Kind = "videoinput" })
                    .ToList());
        }

        protected override object CreateVideoInputDevicesChangedCallback(Action onInvoked)
            => EventCallback.Factory.Create<IEnumerable<JsScanner.VideoInputDevice>>(this, _ => onInvoked());
    }

    public class ZXingJsBarcodeReaderInteropTests : BunitContext
    {
        private JsScanner.BarcodeReaderInterop CreateInterop()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            return new JsScanner.BarcodeReaderInterop(JSInterop.JSRuntime);
        }

        [Fact]
        public void Raises_BarcodeReceived_for_a_new_code()
        {
            var interop = CreateInterop();
            var received = new List<string>();
            interop.BarcodeReceived += args => { received.Add(args.BarcodeText); return Task.CompletedTask; };

            interop.OnBarcodeReceived("4006381333931");

            Assert.Equal(["4006381333931"], received);
        }

        [Fact]
        public void Debounces_a_repeated_code()
        {
            var interop = CreateInterop();
            var received = new List<string>();
            interop.BarcodeReceived += args => { received.Add(args.BarcodeText); return Task.CompletedTask; };

            interop.OnBarcodeReceived("same");
            interop.OnBarcodeReceived("same");
            interop.OnBarcodeReceived("other");

            Assert.Equal(["same", "other"], received);
        }

        [Fact]
        public void Ignores_an_empty_code()
        {
            var interop = CreateInterop();
            var received = 0;
            interop.BarcodeReceived += _ => { received++; return Task.CompletedTask; };

            interop.OnBarcodeReceived(string.Empty);

            Assert.Equal(0, received);
        }

        [Fact]
        public void Raises_BarcodeNotFound_only_after_a_code_was_seen()
        {
            var interop = CreateInterop();
            var notFound = 0;
            interop.BarcodeNotFound += () => notFound++;

            interop.OnNotFoundReceived();
            Assert.Equal(0, notFound);

            interop.OnBarcodeReceived("something");
            interop.OnNotFoundReceived();
            interop.OnNotFoundReceived();

            Assert.Equal(1, notFound);
        }

        [Fact]
        public async Task StartDecoding_sets_the_resolution_before_starting()
        {
            var interop = CreateInterop();

            await interop.StartDecoding(default, 1280, 720);

            var identifiers = JSInterop.Invocations.Select(i => i.Identifier).ToList();
            var resolution = identifiers.IndexOf("BlazorBarcodeScanner.setVideoResolution");
            var start = identifiers.IndexOf("BlazorBarcodeScanner.startDecoding");

            Assert.True(resolution >= 0 && start > resolution, string.Join(", ", identifiers));
            Assert.Equal([1280, 720], JSInterop.Invocations["BlazorBarcodeScanner.setVideoResolution"].Single().Arguments);
        }

        [Fact]
        public async Task StopDecoding_calls_into_the_JS_layer()
        {
            var interop = CreateInterop();

            await interop.StopDecoding();

            JSInterop.VerifyInvoke("BlazorBarcodeScanner.stopDecoding");
        }

        [Fact]
        public async Task Torch_helpers_map_to_their_JS_counterparts()
        {
            var interop = CreateInterop();

            await interop.SetTorchOn();
            await interop.SetTorchOff();
            await interop.ToggleTorch();

            JSInterop.VerifyInvoke("BlazorBarcodeScanner.setTorchOn");
            JSInterop.VerifyInvoke("BlazorBarcodeScanner.setTorchOff");
            JSInterop.VerifyInvoke("BlazorBarcodeScanner.toggleTorch");
        }
    }
}
