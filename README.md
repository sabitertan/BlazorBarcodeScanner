![Nuget](https://img.shields.io/nuget/v/BlazorBarcodeScanner.ZXing.JS?style=flat-square)
# BlazorBarcodeScanner
Barcode Scanner component for Blazor using [zxing-js](https://github.com/zxing-js/library) Interop

## Prerequisites

Before you continue, please make sure you have the latest version of Visual Studio and .NET 5 installed. Visit official [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/client) site to learn more.

## Installation

### 1. NuGet packages

```
Install-Package BlazorBarcodeScanner.ZXing.JS
```

or

```
dotnet add package BlazorBarcodeScanner.ZXing.JS
```

### 2. Refence to JS libraries

Add following lines to `wwwroot\index.html` (for server side `_Host.cshtml`) before `</body>` tag.

```html
    <script src="_content/BlazorBarcodeScanner.ZXing.JS/zxingjs.index.min.js"></script>
    <script src="_content/BlazorBarcodeScanner.ZXing.JS/BlazorBarcodeScanner.js"></script>
```

## Usage

Add reference to your `.razor` page/component for this library

```cs
@using BlazorBarcodeScanner.ZXing.JS
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
#### OnCodeReceived
The library raises a custom event, whenever the barcode scanner sucessfully decoded a value from video stream. You can attach to that event using the component's Blazor `EventCallback` named `OnCodeReceived`.

**Note**: Accessing `BlazorBarcodeScanner.ZXing.JS.JsInteropClass.BarcodeReceived` directly is discuraged and will be removed in the future. See the corresponding fragments in the code blocks below:
```html
<BlazorBarcodeScanner.ZXing.JS.BarcodeReader 
    ...
    OnCodeReceived="LocalReceivedBarcodeText"
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

### Capturing a picture from the stream
#### Direct capture
In some application it might be useful if a picture can be useful to take a still image of the video stream while decoding.
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
In some application it might be useful if a picture can be useful to take a still image of the frame that just decoded the last barcode. 
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
This library uses auto-detect feature of zxing-js library. It supports variety of barcode types. For more information: [zxing-js supported types](https://github.com/zxing-js/library#supported-formats)
