console.log("Init BlazorBarcodeScanner");
async function mediaStreamSetTorch(track, onOff) {
    await track.applyConstraints({
        advanced: [{
            fillLightMode: onOff ? 'flash' : 'off',
            torch: onOff ? true : false,
        }],
    });
}

  /**
   * Checks if the stream has torch support.
   */
function mediaStreamIsTorchCompatible(stream) {

    const tracks = stream.getVideoTracks();

    for (const track of tracks) {
        if (BrowserCodeReader.mediaStreamIsTorchCompatibleTrack(track)) {
            return true;// @IDEA returning track to caller might  be helpful ?
        }
    }

    return false;
}

  /**
   *
   * @param track The media stream track that will be checked for compatibility.
   */
  function mediaStreamIsTorchCompatibleTrack(track) {
    try {
        const capabilities = track.getCapabilities();
        return 'torch' in capabilities;
    } catch (err) {
        // some browsers may not be compatible with ImageCapture
        // so we are ignoring this for now.
        console.error(err);
        console.warn('Your browser may be not fully compatible with WebRTC and/or ImageCapture specs. Torch will not be available.');
        return false;
    }
}
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
                DotNet.invokeMethodAsync('BlazorBarcodeScanner.ZXing.JS', 'ReceiveBarcode', result.text)
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
        });
        // @TODO: Check if torch or fill light capabilities are available 
        this.codeReader.stream.getVideoTracks()[0].applyConstraints({
            advanced: [{ torch: true }] // or false to turn off the torch
        });
        console.log(`Started continous decode from camera with id ${selectedDeviceId}`);
    },
    stopDecoding: function () {
        this.codeReader.reset();
        DotNet.invokeMethodAsync('BlazorBarcodeScanner.ZXing.JS', 'ReceiveBarcode', '')
            .then(message => {
                console.log(message);
            });
        console.log('Reset.');
    },
    setTorchOn: function () {
        if (mediaStreamIsTorchCompatible(this.codeReader.stream)) {
            mediaStreamSetTorch(this.codeReader.stream.getVideoTracks()[0], true);
        }
    },
    setTorchOff() {
        if (mediaStreamIsTorchCompatible(this.codeReader.stream)) {
            mediaStreamSetTorch(this.codeReader.stream.getVideoTracks()[0], false);
        }
    }
};