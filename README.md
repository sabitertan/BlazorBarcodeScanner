[![Nuget ZXing.JS](https://img.shields.io/nuget/v/BlazorBarcodeScanner.ZXing.JS?style=flat-square&label=ZXing.JS)](https://www.nuget.org/packages/BlazorBarcodeScanner.ZXing.JS)
[![Nuget ZXing.Cpp](https://img.shields.io/nuget/v/BlazorBarcodeScanner.ZXing.Cpp?style=flat-square&label=ZXing.Cpp)](https://www.nuget.org/packages/BlazorBarcodeScanner.ZXing.Cpp)
![build](https://github.com/sabitertan/BlazorBarcodeScanner/actions/workflows/main.yml/badge.svg)
# BlazorBarcodeScanner
Barcode Scanner component for Blazor, available with two interchangeable reader engines.

| Package | Engine | Notes |
| --- | --- | --- |
| `BlazorBarcodeScanner.ZXing.JS` | [zxing-js](https://github.com/zxing-js/library) | Pure JavaScript, the original implementation. |
| `BlazorBarcodeScanner.ZXing.Cpp` | [zxing-cpp](https://github.com/zxing-cpp/zxing-cpp) compiled to WebAssembly | Faster decoding, format filtering, detection overlay. Ships a ~1.3 MB `.wasm` payload. |

Both expose the same `BarcodeReader` component surface, so switching engines is a matter of
changing the namespace and the script tags. They can also be referenced side by side in one
app - the demo does exactly that.

## Demo
[https://sabitertan.github.io/BlazorBarcodeScanner/](https://sabitertan.github.io/BlazorBarcodeScanner/)

## Prerequisites

.NET 8, 9 or 10. Visit the official [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/client) site to learn more.

> .NET 6 and .NET 7 support was dropped after both went out of support; use version 1.0.4 of
> `BlazorBarcodeScanner.ZXing.JS` if you still need them.

## Installation

### 1. NuGet packages

```
dotnet add package BlazorBarcodeScanner.ZXing.JS
```

or, for the WebAssembly reader

```
dotnet add package BlazorBarcodeScanner.ZXing.Cpp
```

### 2. Reference the JS libraries

Add the following lines to `wwwroot\index.html` (for server side `_Host.cshtml`) before the `</body>` tag.

For `BlazorBarcodeScanner.ZXing.JS`:

```html
    <script src="_content/BlazorBarcodeScanner.ZXing.JS/zxingjs.index.min.js"></script>
    <script src="_content/BlazorBarcodeScanner.ZXing.JS/BlazorBarcodeScanner.js"></script>
```

For `BlazorBarcodeScanner.ZXing.Cpp`:

```html
    <script src="_content/BlazorBarcodeScanner.ZXing.Cpp/zxing-cpp.js"></script>
    <script src="_content/BlazorBarcodeScanner.ZXing.Cpp/BlazorBarcodeScanner.js"></script>
```

## Usage

Add reference to your `.razor` page/component for this library

```cs
@using BlazorBarcodeScanner.ZXing.JS
```

or

```cs
@using BlazorBarcodeScanner.ZXing.Cpp
```

Add following component ( with `default parameters `) to anywhere you want in your page/component

```html
<BlazorBarcodeScanner.ZXing.JS.BarcodeReader />
```

or with `custom parameters` ( below shows default values of parameters)

```html
<BlazorBarcodeScanner.ZXing.JS.BarcodeReader
    Title="Scan Barcode from Camera"
    StartCameraAutomatically="false"
    ShowStart="true"
    ShowReset="true"
    ShowToggleTorch = "true"
    ShowVideoDeviceList="true"
    VideoWidth="300"
    VideoHeight="200"
 />

```

Note that `ShowToggleTorch` is an experimental feature.

### Receiving callbacks
#### OnBarcodeReceived
The library raises a custom event, whenever the barcode scanner sucessfully decoded a value from video stream. You can attach to that event using the component's Blazor `EventCallback` named `OnBarcodeReceived`.

See the corresponding fragments in the code blocks below:

```html
<BlazorBarcodeScanner.ZXing.JS.BarcodeReader
    ...
    OnBarcodeReceived="LocalReceivedBarcodeText"
 />
```

```cs
    private string LocalBarcodeText;

    private void LocalReceivedBarcodeText(BarcodeReceivedEventArgs args)
    {
        this.LocalBarcodeText = args.BarcodeText;
        StateHasChanged();
    }
```

#### OnDecodingChanged
In case you need to react on changed decoding states (e.g. hide and display the camera view in your page), you can hook up to this callback.
#### OnErrorReceived
Library raises this event when there is a generic error happens, for example no video source available or user didn't give permissions.
#### OnNotFoundReceived
Library raises this event when an error happens while decoding.
#### VideoInputDevicesChanged
Library raises this event when the list of available input devices changes.
#### SelectedVideoInputIdChanged
Library raises this event when the selected video device changes.

### Capturing a picture from the stream
#### Direct capture
In some applications it might be useful to take a still image of the video stream while decoding.
Therefor the component features an API call to capture such an image as base64 encoded JPEG image.
```html
    <BlazorBarcodeScanner.ZXing.JS.BarcodeReader @ref="_reader"
        ...
    />
    <button @onclick="OnGrabFrame">Grab image</button>
    <!-- If there is no source URL, we hide the image to avoid he "broken image" icons... -->
    <img src="@_img"  style="@(string.IsNullOrWhiteSpace(_imgSrc) ? "display:none;" : "")" />
```

```cs
    ...
    private BarcodeReader _reader;
    private string _img = string.Empty;

    private void OnGrabFrame(MouseEventArgs args)
    {
        _imgSrc = await _reader.Capture();
        StateHasChanged();
    }
```

##### Retrieving the picture for the last code decoded
In some applications it might be useful to take a still image of the frame that just decoded the last barcode.
This functionality can be enabled by setting the `DecodedPictureCapture` attribute to `true`. This will cause the component to store last image successfully decoded.
Upon sucessful deciding (e.g. reception of `OnCodeReceived`), the picture can be accessed by invoking `CaptureLastDecodedPicture`.

**Warning**: Bear in mind that capturing those pictures might impair performance, CPU load or battery life.

### Setting stream quality
While keeping resolution low speeds up image processing, it might yield poor detection performance due to the limited image quality.

In order to allow the application to trade speed for quality, the stream resolution can be set by the application through the following `custom parameters`:
  - StreamWidth
  - StreamHeight

If set to `null` or `0`, a default (browser dependent?) resolution is applied (e.g. 640px by 480px). If set to any number `>0`, the camera stream is requested with the given setting. The settings are used as `ideal` constraint for `getUserMedia` (see [constraints doc](https://developer.mozilla.org/en-US/docs/Web/API/Media_Streams_API/Constraints#specifying_a_range_of_values). Doing so allows for achieving highest resolution by requesting rediculous high numbers for either dimension, causing  the browser to fall back to the maximum feasable for the device of choice.

**Warning**: While increasing the stream resolution might improve your application's code reading performance, it might greatly affect the over all user experience (e.g. through a drop of the frame rate, increased CPU usage, bad battery life, ...)

### Supported Formats
Both engines auto-detect a wide variety of barcode types. For more information see
[zxing-js supported types](https://github.com/zxing-js/library#supported-formats) and
[zxing-cpp supported types](https://github.com/zxing-cpp/zxing-cpp#supported-formats).

### ImageCapture Support
This library uses Media API's ImageCapture, this is an experimental feauture on Firefox. You may want to implement [ImageCapture Polyfill](https://github.com/GoogleChromeLabs/imagecapture-polyfill) in order to use image capturing feature.

`BlazorBarcodeScanner.ZXing.Cpp` falls back to grabbing the current video frame when
`ImageCapture` is unavailable, so `Capture()` also works on Firefox and Safari - at video
resolution rather than full sensor resolution.

## ZXing.Cpp specific parameters

On top of the shared parameters, the WebAssembly reader accepts:

| Parameter | Default | Description |
| --- | --- | --- |
| `Formats` | `""` (all) | Pipe separated zxing-cpp format names to look for, e.g. `QRCode\|EAN-13`. Restricting the set speeds decoding up noticeably. |
| `TryHarder` | `true` | Spend more time per frame to also find rotated, blurry or low contrast codes. |
| `ScanIntervalMilliseconds` | `100` | Minimum time between two decode attempts. Lower reacts faster, at the cost of CPU and battery. |

The component also draws the outline of a detected code onto a canvas layered over the video
feed. Style it through the `.zxing-overlay` class.

## Building and testing

```
dotnet build
dotnet test BlazorBarcodeScanner.Tests/BlazorBarcodeScanner.Tests.csproj
```

The test suite runs a shared contract suite against both `BarcodeReader` implementations so
they cannot drift apart, plus per-engine tests for the JS interop layer.
