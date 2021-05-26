# Project maintenance
## Upgrading ZXingJS
The latest minified version of ZXingJS is available from (unpkg.com)[https://unpkg.com/@zxing/library/umd/index.min.js] . In order to upgrade the version used within this library, simply download this file and overwrite `BlazorBarcodeScanner.ZXing.JS\wwwroot\zxing.index.min.js` with it.

As ZXingJS does not provide any facility to retrieve the version from the library itself, simply add the download URL (including the concrete version) as a comment line right above the copyright notice.