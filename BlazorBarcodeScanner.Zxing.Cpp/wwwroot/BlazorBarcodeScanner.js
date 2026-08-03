/*
 * Blazor interop for the zxing-cpp WebAssembly reader.
 *
 * The global is deliberately named differently from the one exported by
 * BlazorBarcodeScanner.ZXing.JS so both packages can be referenced by the same app.
 */
(function () {
    "use strict";

    let zxingPromise = null;

    /** Instantiates the zxing-cpp WebAssembly module once and caches the promise. */
    function getZXing() {
        if (zxingPromise === null) {
            if (typeof ZXingCpp !== "function") {
                return Promise.reject(new Error(
                    'zxing-cpp.js is not loaded. Add <script src="_content/BlazorBarcodeScanner.ZXing.Cpp/zxing-cpp.js"></script> to your host page.'));
            }
            zxingPromise = ZXingCpp();
        }
        return zxingPromise;
    }

    function mediaStreamSetTorch(track, onOff) {
        return track.applyConstraints({
            advanced: [{
                fillLightMode: onOff ? 'flash' : 'off',
                torch: !!onOff,
            }],
        });
    }

    /**
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

    /** Returns the first track of the stream that supports the torch, or null. */
    function mediaStreamGetTorchCompatibleTrack(stream) {
        if (!stream) {
            return null;
        }

        for (const track of stream.getVideoTracks()) {
            if (mediaStreamIsTorchCompatibleTrack(track)) {
                return track;
            }
        }

        return null;
    }

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

            videoDevices.push({ deviceId, label, kind, groupId });
        }

        return videoDevices;
    }

    /**
     * A single scanner instance. One is created per BarcodeReader component and handed
     * to .NET as an IJSObjectReference, so several scanners can coexist on a page.
     */
    class BarcodeReader {
        constructor(dotNetHelper) {
            this.dotNetHelper = dotNetHelper;

            this.selectedDeviceId = undefined;
            this.streamWidth = 640;
            this.streamHeight = 480;

            /* zxing-cpp decode options: '' means "every supported format". */
            this.formats = '';
            this.tryHarder = true;
            this.scanInterval = 100;
            this.overlayColor = 'red';

            this.lastPicture = '';
            this.lastPictureDecoded = undefined;
            this.lastPictureDecodedFormat = undefined;

            this.zxing = undefined;
            this.stream = undefined;
            this.video = undefined;
            this.overlay = undefined;

            /* Off-screen canvas the frames are read back from. */
            this.scanCanvas = document.createElement('canvas');
            this.frameHandle = 0;
            this.lastScan = 0;
        }

        setSelectedDeviceId(deviceId) {
            this.selectedDeviceId = deviceId || undefined;
        }

        getSelectedDeviceId() {
            return this.selectedDeviceId || '';
        }

        setVideoResolution(width, height) {
            this.streamWidth = width;
            this.streamHeight = height;
        }

        setDecodeOptions(formats, tryHarder, scanInterval) {
            this.formats = formats || '';
            this.tryHarder = !!tryHarder;
            this.scanInterval = scanInterval > 0 ? scanInterval : 0;
        }

        setLastDecodedPictureFormat(format) {
            this.lastPictureDecoded = undefined;
            this.lastPictureDecodedFormat = format;
        }

        getVideoConstraints() {
            const videoConstraints = {};

            if (!this.selectedDeviceId) {
                videoConstraints["facingMode"] = 'environment';
            }
            else {
                videoConstraints["deviceId"] = { exact: this.selectedDeviceId };
            }

            if (this.streamWidth) videoConstraints["width"] = { ideal: this.streamWidth };
            if (this.streamHeight) videoConstraints["height"] = { ideal: this.streamHeight };

            return videoConstraints;
        }

        async startDecoding(video, overlay) {
            /* Never leave a previous stream running - it would keep the camera busy. */
            this.teardown();

            const zxing = await getZXing();
            const stream = await navigator.mediaDevices.getUserMedia({ video: this.getVideoConstraints(), audio: false });

            this.zxing = zxing;
            this.stream = stream;
            this.video = video;
            this.overlay = overlay;

            video.srcObject = stream;
            video.setAttribute("playsinline", true); // required to tell iOS safari we don't want fullscreen
            video.muted = true;
            await video.play();

            /* Report back what we actually got - it may differ from what we asked for. */
            const settings = stream.getVideoTracks()[0].getSettings();
            if (settings.deviceId) {
                this.selectedDeviceId = settings.deviceId;
            }

            this.lastScan = 0;
            this.frameHandle = requestAnimationFrame(() => this.scanFrame());

            await this.dotNetHelper.invokeMethodAsync('OnDecodingStarted', this.getSelectedDeviceId());
        }

        async stopDecoding() {
            const wasDecoding = this.stream !== undefined;
            const deviceId = this.getSelectedDeviceId();

            this.teardown();

            if (wasDecoding) {
                await this.dotNetHelper.invokeMethodAsync('OnDecodingStopped', deviceId);
            }
        }

        /** Releases the camera and the render loop without calling back into .NET. */
        teardown() {
            if (this.frameHandle) {
                cancelAnimationFrame(this.frameHandle);
                this.frameHandle = 0;
            }

            if (this.stream) {
                this.stream.getTracks().forEach(track => track.stop());
                this.stream = undefined;
            }

            if (this.video) {
                this.video.pause();
                this.video.srcObject = null;
            }

            this.clearOverlay();
            this.lastPictureDecoded = undefined;
        }

        scanFrame() {
            if (!this.stream) {
                return;
            }

            this.frameHandle = requestAnimationFrame(() => this.scanFrame());

            const now = performance.now();
            if (now - this.lastScan < this.scanInterval) {
                return;
            }
            this.lastScan = now;

            const width = this.video.videoWidth;
            const height = this.video.videoHeight;
            if (!width || !height) {
                /* The stream has not delivered its first frame yet. */
                return;
            }

            if (this.scanCanvas.width !== width || this.scanCanvas.height !== height) {
                this.scanCanvas.width = width;
                this.scanCanvas.height = height;
            }

            const context = this.scanCanvas.getContext('2d', { willReadFrequently: true });
            context.drawImage(this.video, 0, 0, width, height);

            let result;
            try {
                result = this.readBarcode(context, width, height);
            } catch (err) {
                this.report('OnErrorReceived', err && err.message ? err.message : String(err));
                return;
            }

            this.clearOverlay();

            if (result && result.format) {
                if (this.lastPictureDecodedFormat) {
                    this.lastPictureDecoded = this.scanCanvas.toDataURL(this.lastPictureDecodedFormat);
                }
                this.drawResult(result, width, height);
                this.report('OnBarcodeReceived', result.text);
            } else if (result && result.error) {
                this.report('OnErrorReceived', result.error);
            } else {
                this.lastPictureDecoded = undefined;
                this.report('OnNotFoundReceived');
            }
        }

        readBarcode(context, width, height) {
            const zxing = this.zxing;
            if (!zxing) {
                return null;
            }

            const sourceBuffer = context.getImageData(0, 0, width, height).data;
            const buffer = zxing._malloc(sourceBuffer.byteLength);
            try {
                zxing.HEAPU8.set(sourceBuffer, buffer);
                return zxing.readBarcodeFromPixmap(buffer, width, height, this.tryHarder, this.formats);
            } finally {
                zxing._free(buffer);
            }
        }

        /**
         * The overlay canvas uses the intrinsic frame size as its backing store, so the
         * positions reported by zxing-cpp can be drawn without any scaling maths. CSS
         * takes care of stretching it over the video element.
         */
        drawResult(code, width, height) {
            if (!this.overlay || !code.position) {
                return;
            }

            if (this.overlay.width !== width || this.overlay.height !== height) {
                this.overlay.width = width;
                this.overlay.height = height;
            }

            const context = this.overlay.getContext('2d');
            const position = code.position;

            context.beginPath();
            context.lineWidth = Math.max(2, Math.round(width / 200));
            context.strokeStyle = this.overlayColor;
            context.moveTo(position.topLeft.x, position.topLeft.y);
            context.lineTo(position.topRight.x, position.topRight.y);
            context.lineTo(position.bottomRight.x, position.bottomRight.y);
            context.lineTo(position.bottomLeft.x, position.bottomLeft.y);
            context.closePath();
            context.stroke();
        }

        clearOverlay() {
            if (!this.overlay) {
                return;
            }

            const context = this.overlay.getContext('2d');
            if (context) {
                context.clearRect(0, 0, this.overlay.width, this.overlay.height);
            }
        }

        setTorchOn() {
            return this.setTorch(true);
        }

        setTorchOff() {
            return this.setTorch(false);
        }

        setTorch(onOff) {
            const track = mediaStreamGetTorchCompatibleTrack(this.stream);
            return track === null ? Promise.resolve() : mediaStreamSetTorch(track, onOff);
        }

        toggleTorch() {
            const track = mediaStreamGetTorchCompatibleTrack(this.stream);
            return track === null ? Promise.resolve() : mediaStreamSetTorch(track, !track.getSettings().torch);
        }

        async capture(type) {
            this.lastPicture = '';

            if (!this.stream || !this.video) {
                return '';
            }

            let source = this.video;
            let width = this.video.videoWidth;
            let height = this.video.videoHeight;

            /* ImageCapture gives us the full sensor resolution, but Safari and Firefox
             * do not implement it - fall back to the frame shown in the video element. */
            if (typeof ImageCapture !== 'undefined') {
                try {
                    const bitmap = await new ImageCapture(this.stream.getVideoTracks()[0]).grabFrame();
                    source = bitmap;
                    width = bitmap.width;
                    height = bitmap.height;
                } catch (err) {
                    console.warn('ImageCapture failed, capturing the current video frame instead.', err);
                }
            }

            if (!width || !height) {
                return '';
            }

            const canvas = document.createElement('canvas');
            canvas.width = width;
            canvas.height = height;
            canvas.getContext('2d').drawImage(source, 0, 0, width, height);

            this.lastPicture = canvas.toDataURL(type);
            return this.lastPicture;
        }

        pictureGetBase64(source) {
            switch (source) {
                case "decoded":
                    return this.lastPictureDecoded || '';

                case "capture":
                default:
                    return this.lastPicture || '';
            }
        }

        /** Fire and forget - a failing callback must not kill the render loop. */
        report(method, ...args) {
            this.dotNetHelper.invokeMethodAsync(method, ...args)
                .catch(err => console.error(`BlazorBarcodeScanner: ${method} failed`, err));
        }

        dispose() {
            this.teardown();
            this.dotNetHelper = undefined;
        }
    }

    window.BlazorBarcodeScannerZXingCpp = {
        listVideoInputDevices: listVideoInputDevices,
        createReader: function (dotNetHelper) { return new BarcodeReader(dotNetHelper); },
    };
})();
