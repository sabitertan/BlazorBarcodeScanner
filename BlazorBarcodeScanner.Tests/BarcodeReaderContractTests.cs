using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorBarcodeScanner.Tests
{
    /// <summary>
    /// Markup and behaviour both reader components must agree on. The suite is run once per
    /// implementation so the zxing-js and zxing-cpp packages stay drop-in replacements for
    /// each other.
    /// </summary>
    public abstract class BarcodeReaderContractTests<TReader> : BunitContext where TReader : IComponent
    {
        /// <summary>The JS identifier the implementation enumerates cameras with.</summary>
        protected abstract string ListVideoInputDevicesIdentifier { get; }

        /// <summary>Registers a camera list to be returned by the JS layer.</summary>
        protected abstract void SetupVideoInputDevices(params (string DeviceId, string Label)[] devices);

        /// <summary>Builds an <c>EventCallback</c> for the implementation's VideoInputDevicesChanged parameter.</summary>
        protected abstract object CreateVideoInputDevicesChangedCallback(Action onInvoked);

        protected BarcodeReaderContractTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private IRenderedComponent<TReader> Render(params (string Name, object? Value)[] parameters)
            => Render<TReader>(builder =>
            {
                foreach (var (name, value) in parameters)
                {
                    Assert.True(builder.TryAdd(name, value), $"{typeof(TReader)} has no parameter named '{name}'.");
                }
            });

        [Fact]
        public void Renders_the_container_section()
        {
            var cut = Render();

            Assert.NotNull(cut.Find("section.zxing-container"));
        }

        [Fact]
        public void Renders_the_title_when_one_is_given()
        {
            var cut = Render(("Title", "Scan a barcode"));

            Assert.Equal("Scan a barcode", cut.Find("h3.zxing-title").TextContent.Trim());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Omits_the_title_when_it_is_blank(string title)
        {
            var cut = Render(("Title", title));

            Assert.Empty(cut.FindAll("h3.zxing-title"));
        }

        [Fact]
        public void Renders_all_control_buttons_with_their_captions()
        {
            var cut = Render(
                ("ButtonStartText", "Go"),
                ("ButtonStopText", "Halt"),
                ("ButtonResetText", "Again"),
                ("ButtonToggleTorchText", "Light"));

            var captions = cut.FindAll("button.zxing-button").Select(b => b.TextContent.Trim()).ToArray();

            Assert.Equal(["Go", "Halt", "Again", "Light"], captions);
        }

        [Fact]
        public void Hides_every_button_that_is_switched_off()
        {
            var cut = Render(
                ("ShowStart", false),
                ("ShowStop", false),
                ("ShowReset", false),
                ("ShowToggleTorch", false));

            Assert.Empty(cut.FindAll("button.zxing-button"));
        }

        [Fact]
        public void Shows_the_placeholder_while_no_camera_is_known()
        {
            var cut = Render(("TextWithoutDevices", "no cameras here"));

            Assert.Contains("no cameras here", cut.Markup);
            Assert.Empty(cut.FindAll("select.zxing-video-select"));
        }

        [Fact]
        public void Lists_every_camera_reported_by_the_browser()
        {
            SetupVideoInputDevices(("front-id", "Front camera"), ("back-id", "Back camera"));

            var cut = Render();

            var options = cut.FindAll("select.zxing-video-select option");

            Assert.Equal(2, options.Count);
            Assert.Equal("front-id", options[0].GetAttribute("value"));
            Assert.Equal("Front camera", options[0].TextContent.Trim());
            Assert.Equal("back-id", options[1].GetAttribute("value"));
            Assert.Equal("Back camera", options[1].TextContent.Trim());
        }

        [Fact]
        public void Hides_the_camera_list_when_asked_to()
        {
            SetupVideoInputDevices(("front-id", "Front camera"));

            var cut = Render(("ShowVideoDeviceList", false));

            Assert.Empty(cut.FindAll("select.zxing-video-select"));
        }

        [Fact]
        public void Enumerates_the_cameras_on_first_render()
        {
            Render();

            Assert.Contains(JSInterop.Invocations, i => i.Identifier == ListVideoInputDevicesIdentifier);
        }

        [Fact]
        public void Publishes_the_camera_list_through_VideoInputDevicesChanged()
        {
            SetupVideoInputDevices(("front-id", "Front camera"));
            var published = 0;

            Render(("VideoInputDevicesChanged", CreateVideoInputDevicesChangedCallback(() => published++)));

            Assert.Equal(1, published);
        }

        [Fact]
        public void Renders_the_result_container_by_default()
        {
            var cut = Render();

            Assert.NotNull(cut.Find("div.zxing-result"));
        }

        [Fact]
        public void Hides_the_result_container_when_asked_to()
        {
            var cut = Render(("ShowResult", false));

            Assert.Empty(cut.FindAll("div.zxing-result-container"));
        }

        [Fact]
        public void Sizes_the_video_element_from_the_parameters()
        {
            var cut = Render(("VideoWidth", 640), ("VideoHeight", 480));

            var video = cut.Find("video");

            Assert.Equal("640", video.GetAttribute("width"));
            Assert.Equal("480", video.GetAttribute("height"));
        }

        [Fact]
        public void Drops_the_fixed_size_for_a_full_width_video()
        {
            var cut = Render(("FullWidthVideo", true));

            var video = cut.Find("video");

            Assert.False(video.HasAttribute("width"));
            Assert.False(video.HasAttribute("height"));
        }
    }
}
