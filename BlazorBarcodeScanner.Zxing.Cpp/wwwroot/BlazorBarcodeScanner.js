console.log("Init BlazorBarcodeScanner");

class Helpers {
    static dotNetHelper;

    static setDotNetHelper(value) {
        Helpers.dotNetHelper = value;
    }

    static async receiveBarcode(text) {
        await Helpers.dotNetHelper.invokeMethodAsync('OnBarcodeReceived', text);
    }

    static async receiveError(err) {
        await Helpers.dotNetHelper.invokeMethodAsync('OnErrorReceived', err);
    }

    static async receiveNotFound() {
        await Helpers.dotNetHelper.invokeMethodAsync('OnNotFoundReceived');
    }

    static async decodingStarted(deviceId) {
        await Helpers.dotNetHelper.invokeMethodAsync('OnDecodingStarted', deviceId);
    }

    static async decodingStopped(deviceId) {
        await Helpers.dotNetHelper.invokeMethodAsync('OnDecodingStopped', deviceId);
    }
}

window.Helpers = Helpers;

async function mediaStreamSetTorch(track, onOff) {
    await track.applyConstraints({
        advanced: [{
            fillLightMode: onOff ? 'flash' : 'off',
            torch: !!onOff,
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

function initZxing(canvas, constraints, video, callbackFn){
    var zxing = ZXing().then(function (instance) {
        zxing = instance; // this line is supposedly not required but with current emsdk it is :-/
    });

    const cameraSelector = document.getElementById("cameraSelector");
    const format = '';
    const mode = 'true';
    // const canvas = document.getElementById("video-canvas");
    const resultElement = document.getElementById("result");

    const ctx = canvas.getContext("2d", { willReadFrequently: true });
    // const video = document.createElement("video");
    //video.setAttribute("id", "video");
    //video.setAttribute("width", canvas.width);
    //video.setAttribute("height", canvas.height);
    //video.setAttribute("autoplay", "");

    function readBarcodeFromCanvas(canvas, format, mode) {
        let imgWidth = canvas.width;
        let imgHeight = canvas.height;
        let imageData = canvas.getContext('2d').getImageData(0, 0, imgWidth, imgHeight);
        let sourceBuffer = imageData.data;

        if (zxing != null) {
            let buffer = zxing._malloc(sourceBuffer.byteLength);
            zxing.HEAPU8.set(sourceBuffer, buffer);
            let result = zxing.readBarcodeFromPixmap(buffer, imgWidth, imgHeight, true, '');
            zxing._free(buffer);
            return result;
        } else {
            return { error: "ZXing not yet initialized" };
        }
    }

    function drawResult(code) {
        ctx.beginPath();
        ctx.lineWidth = 4;
        ctx.strokeStyle = "red";
        // ctx.textAlign = "center";
        // ctx.fillStyle = "#green"
        // ctx.font = "25px Arial";
        // ctx.fontWeight = "bold";
        with (code.position) {
            ctx.moveTo(topLeft.x, topLeft.y);
            ctx.lineTo(topRight.x, topRight.y);
            ctx.lineTo(bottomRight.x, bottomRight.y);
            ctx.lineTo(bottomLeft.x, bottomLeft.y);
            ctx.lineTo(topLeft.x, topLeft.y);
            ctx.stroke();
            // ctx.fillText(code.text, (topLeft.x + bottomRight.x) / 2, (topLeft.y + bottomRight.y) / 2);
        }
    }

    function escapeTags(htmlStr) {
        return htmlStr.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;");
    }

    const processFrame = function () {
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

        const code = readBarcodeFromCanvas(canvas, format.value, mode.value === 'true');
        if (code.format) {
            resultElement.innerText = code.format + ": " + escapeTags(code.text);
            drawResult(code)
            callbackFn({text : escapeTags(code.text)});
        } else {
            resultElement.innerText = "No barcode found";
        }
        requestAnimationFrame(processFrame);
    };

    const updateVideoStream = function (deviceId) {
        navigator.mediaDevices
            .getUserMedia(constraints)
            .then(function (stream) {
                video.srcObject = stream;
                video.setAttribute("playsinline", true); // required to tell iOS safari we don't want fullscreen
                video.play();
                processFrame();
            })
            .catch(function (error) {
                console.error("Error accessing camera:", error);
            });
    };
/*
    cameraSelector.addEventListener("change", function () {
        updateVideoStream(this.value);
    });
    */

    updateVideoStream();
    }
const codeReader = {
    stopStreams: function () {
        if (this.stream) {
            this.stream.getVideoTracks().forEach(t => t.stop());
            this.stream = undefined;
        }
        // @TODO: stop decoding here
    },
    attachStreamToVideo: async function (stream, videoSource) {
        videoSource.srcObject = stream;
        this.videoElement = videoSource;
        this.stream = stream;

        return videoSource;
    },
    decodeFromConstraints: async function (canvas, constraints, videoSource, callbackFn) {
        const stream = await navigator.mediaDevices.getUserMedia(constraints);
        const video = await this.attachStreamToVideo(stream, videoSource);
        initZxing(canvas, constraints, video,  callbackFn);
        return;
    }
};

async function listVideoInputDevices() {

    const devices = await navigator.mediaDevices.enumerateDevices();

    const videoDevices = [];

    for (const device of devices) {
        const kind = device.kind === 'video' ? 'videoinput' : device.kind;

        if (kind !== 'videoinput') {
            continue;
        }

        const deviceId = device.deviceId || device.id;
        const label = device.label || `Video device ${videoDevices.length + 1}`;
        const groupId = device.groupId;

        const videoDevice = { deviceId, label, kind, groupId };

        videoDevices.push(videoDevice);
    }

    return videoDevices;
}
window.BlazorBarcodeScanner = {
    codeReader: codeReader,
    listVideoInputDevices: async function () { return await listVideoInputDevices(); },
    selectedDeviceId: undefined,
    setSelectedDeviceId: function (deviceId) {
        this.selectedDeviceId = deviceId;
    },
    getSelectedDeviceId: function () {
        return this.selectedDeviceId;
    },
    streamWidth: 640,
    streamHeight: 480,
    setVideoResolution: function (width, height) {
        this.streamWidth = width;
        this.streamHeight = height;
    },
    lastPicture: undefined,
    lastPictureDecoded: undefined,
    lastPictureDecodedFormat: undefined,
    getVideoConstraints: function () {
        let videoConstraints = {};

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
    startDecoding: async function (video) {
        let videoConstraints = this.getVideoConstraints();

        console.log("Starting decoding with " + videoConstraints);
        
        let videoCanvas = document.getElementById("video-canvas");//TODO Expose canvas reference from C# library

        await this.codeReader.decodeFromConstraints(videoCanvas, { video: videoConstraints }, video, (result, err) => {
            if (result) {
                if (this.lastPictureDecodedFormat) {
                    let captureCanvas = document.getElementsById('capture');//TODO Expose canvas reference from C# library
                    this.lastPictureDecoded = this.capture(this.lastPictureDecodedFormat, captureCanvas);
                }
                Helpers.receiveBarcode(result.text)
                    .then(message => {
                        console.log(message);
                    });
            }
            if (err && !(err instanceof ZXing.NotFoundException)) {
                Helpers.receiveError(err)
                    .then(message => {
                        console.log(message);
                    });
            }
            if (err && (err instanceof ZXing.NotFoundException)) {
                this.lastPictureDecoded = undefined;
                Helpers.receiveNotFound();
            }
        });

        // Make sure the actual selectedDeviceId is logged after start decoding.
        //this.selectedDeviceId = this.codeReader.stream.getVideoTracks()[0].getSettings()["deviceId"];

        /*  this.codeReader.stream.getVideoTracks()[0].applyConstraints({
              advanced: [{ torch: true }] // or false to turn off the torch
          }); */
        console.log(`Started continous decode from camera with id ${this.selectedDeviceId}`);
        Helpers.decodingStarted(this.selectedDeviceId)
    },
    stopDecoding: function () {
        this.codeReader.reset();
        Helpers.receiveBarcode('')
            .then(message => {
                console.log(message);
            });
        Helpers.decodingStopped(this.selectedDeviceId)
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
            let torchStatus = !track.getSettings().torch;
            mediaStreamSetTorch(track, torchStatus);
        }
    },
    capture: async function (type, canvas,video) {
        this.lastPicture = "";

        if (!this.codeReader.stream) {
            return "";
        }

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        canvas.getContext('2d').drawImage(video, 0, 0);
        this.lastPicture = canvas.toDataURL('image/png');
    },
    pictureGetBase64Unmarshalled: function (source) {
        let source_str = BINDING.conv_string(source);
        return BINDING.js_string_to_mono_string(this.pictureGetBase64(source_str));
    },
    pictureGetBase64: function (source) {
        let pic = "";
        switch (source) {
            case "capture": {
                pic = this.lastPicture;
                break;
            }

            case "decoded": {
                pic = this.lastPictureDecoded;
                break;
            }

            default: {
                pic = this.lastPicture;
                break;
            }
        }
        return pic;
    },
    setLastDecodedPictureFormat: function (format) {
        this.lastPictureDecoded = undefined;
        this.lastPictureDecodedFormat = format;
    }
};
