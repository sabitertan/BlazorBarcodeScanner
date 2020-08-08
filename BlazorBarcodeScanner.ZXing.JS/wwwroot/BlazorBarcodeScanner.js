console.log("Init BlazorBarcodeScanner");
window.BlazorBarcodeScanner = {
    codeReader: new ZXing.BrowserMultiFormatReader(),
    listVideoInputDevices: async function () { return await this.codeReader.listVideoInputDevices(); },
    selecteDeviceId: undefined,
    setSelectedDeviceId: function (deviceId) {
        this.selecteDeviceId = deviceId;
    },
    startDecoding: function (videoElementId) {
        this.codeReader.decodeFromVideoDevice(this.selectedDeviceId, videoElementId, (result, err) => {
            if (result) {
                console.log(result);
                DotNet.invokeMethodAsync('BlazorBarcodeScanner.ZXing.JS','ReceiveBarcode', result.text)
                    .then(message => {
                        console.log(message);
                    });
            }
            if (err && !(err instanceof ZXing.NotFoundException)) {
                console.error(err);
                DotNet.invokeMethodAsync('BlazorBarcodeScanner.ZXing.JS', 'ReceiveBarcode', err)
                    .then(message => {
                        console.log(message);
                    });
            }
        })
        console.log(`Started continous decode from camera with id ${selectedDeviceId}`);
    },
    stopDecoding: function () {
        this.codeReader.reset();
        DotNet.invokeMethodAsync('BlazorBarcodeScanner.ZXing.JS', 'ReceiveBarcode', '')
            .then(message => {
                console.log(message);
            });
        console.log('Reset.');
    }
};