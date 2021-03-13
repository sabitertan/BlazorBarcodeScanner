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
    <script src="_content/BlazorBarcodeScanner.ZXing.JS/zxingjs-0.17.1.index.min.js"></script>
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
    ShowVideoDeviceList="true"
    VideoWidth="300"
    VideoHeigth="200"
 />

```

Library raises a custom event when barcode scanner reads a value from video stream, you can attach to that event using example below in `@code` block.

```cs
    private string LocalBarcodeText;
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        BlazorBarcodeScanner.ZXing.JS.JsInteropClass.BarcodeReceived += LocalReceivedBarcodeText; // attach to Barcodereceived event
    }

    private void LocalReceivedBarcodeText(BarcodeReceivedEventArgs args)
    {
        this.LocalBarcodeText = args.BarcodeText;
        StateHasChanged();
    }
```

### Supported Formats
This library uses auto-detect feature of zxing-js library. It supports variety of barcode types. For more information: [zxing-js supported types](https://github.com/zxing-js/library#supported-formats)