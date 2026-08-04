# Project maintenance
## Upgrading ZXingJS
The latest minified version of ZXingJS is available from (unpkg.com)[https://unpkg.com/@zxing/library/umd/index.min.js] . In order to upgrade the version used within this library, simply download this file and overwrite `BlazorBarcodeScanner.ZXing.JS\wwwroot\zxing.index.min.js` with it.

As ZXingJS does not provide any facility to retrieve the version from the library itself, simply add the download URL (including the concrete version) as a comment line right above the copyright notice.

## Upgrading zxing-cpp
The WebAssembly reader is built from source. With [emsdk](https://emscripten.org/docs/getting_started/downloads.html) activated, in a checkout of the zxing-cpp tag you want:

```
emcmake cmake -S wrappers/wasm -B build -DCMAKE_BUILD_TYPE=Release -DZXING_READERS=ON -DZXING_WRITERS=OFF
cmake --build build --target zxing_reader
```

Copy `build/zxing_reader.wasm` into `BlazorBarcodeScanner.Zxing.Cpp\wwwroot`, and copy `build/zxing_reader.js` there as `zxing-cpp.js`, renaming the module global `ZXing` to `ZXingCpp` in its three top-level references - zxing-js also defines a `ZXing` global, and both packages have to coexist. Note the version in the comment header, as the wasm carries none.