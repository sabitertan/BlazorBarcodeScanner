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
        if (mediaStreamIsTorchCompatibleTrack(track)) {
            return true;
        }
    }

    return false;
}

/**
 * Checks if the stream has torch support and return track has torch capability.
 */
function mediaStreamGetTorchCompatibleTrack(stream) {

    const tracks = stream.getVideoTracks();

    for (const track of tracks) {
        if (mediaStreamIsTorchCompatibleTrack(track)) {
            return track;
        }
    }

    return null;
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
    selectedDeviceId: undefined,
    setSelectedDeviceId: function (deviceId) {
        this.selectedDeviceId = deviceId;
    },
    streamWidth: 640,
    streamHeight: 480,
    setVideoResolution: function (width, height) {
        this.streamWidth = width;
        this.streamHeight = height;
    },
    getVideoConstraints: function () {
        var videoConstraints = {};

        if (!this.selectedDeviceId) {
            videoConstraints["facingMode"] = 'environment';
        }
        else {
            videoConstraints["deviceId"] = { exact: this.selectedDeviceId };
        }

        if (this.streamWidth) videoConstraints["width"] = { ideal: this.streamWidth };
        if (this.streamHeight) videoConstraints["height"] = { ideal: this.streamHeight };

        return videoConstraints;
    },
    startDecoding: function (videoElementId) {
        var videoConstraints = this.getVideoConstraints();

        console.log("Starting decoding with " + videoConstraints);
        this.codeReader.decodeFromConstraints({ video: videoConstraints }, videoElementId, (result, err) => {
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
         
      /*  this.codeReader.stream.getVideoTracks()[0].applyConstraints({
            advanced: [{ torch: true }] // or false to turn off the torch
        }); */
        console.log(`Started continous decode from camera with id ${selectedDeviceId}`);
    },
    stopDecoding: function () {
        this.codeReader.reset();
        DotNet.invokeMethodAsync('BlazorBarcodeScanner.ZXing.JS', 'ReceiveBarcode', '')
            .then(message => {
                console.log(message);
            });
        console.log('Reset camera stream.');
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
    },
    toggleTorch() {
        let track = mediaStreamGetTorchCompatibleTrack(this.codeReader.stream);
        if (track !== null) {
            let torchStatus = track.getConstraints().torch ? false: true;
            mediaStreamSetTorch(track, torchStatus);
        }
    }
};