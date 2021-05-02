using BlazorBarcodeScanner.ZXing.JS;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorZXingJSApp.Client.Pages
{
    public partial class Index
    {
        private BarcodeReader _reader;
        private int StreamWidth = 640;
        private int StreamHeight = 480;

        private string LocalBarcodeText;
        private int _currentVideoSourceIdx = 0;

        private string _imgSrc = string.Empty;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                if (!string.IsNullOrWhiteSpace(_reader.SelectedVideoInputId))
                {
                    _currentVideoSourceIdx = SourceIndexFromId();
                }
            }
        }

        private int SourceIndexFromId()
        {
            int result = 0;
            var inputs = _reader.VideoInputDevices.ToList();
            for (result = 0; result < inputs.Count; result++)
            {
                if (inputs[result].DeviceId.Equals(_reader.SelectedVideoInputId))
                {
                    break;
                }
            }
            return result;
        }

        private void LocalReceivedBarcodeText(BarcodeReceivedEventArgs args)
        {
            this.LocalBarcodeText = args.BarcodeText;
            StateHasChanged();
        }

        private async void CapturePicture()
        {
            _imgSrc = await _reader.Capture();
            // 'data:image/jpeg;base64,/9j/4LfqNdCD3Du....
            var stringSub = _imgSrc.Split(",")[1];
            byte[] data = Convert.FromBase64String(stringSub);
           // var base64Decoded = Encoding.ASCII.GetString(data);
           // var rawString = Encoding.ASCII.GetBytes(stringSub);
            using var stream = new MemoryStream(data);
            var zxText = await RenderZXingNet(stream);
            stream.Dispose();
            StateHasChanged();
        }

        public async Task<string> RenderZXingNet(Stream stream) {

            try
            {
                using var image = await SixLabors.ImageSharp.Image.LoadAsync<SixLabors.ImageSharp.PixelFormats.Rgba32>(stream);
                var reader = new ZXing.ImageSharp.BarcodeReader<SixLabors.ImageSharp.PixelFormats.Rgba32>();
                var result = reader.Decode(image);
                return result?.Text;
            }
            catch (Exception exc)
            {
                return exc.Message;
            }
        }

        private void OnVideoSourceNext(MouseEventArgs args)
        {
            var inputs = _reader.VideoInputDevices.ToList();

            if (inputs.Count == 0)
            {
                return;
            }

            _currentVideoSourceIdx++;
            if (_currentVideoSourceIdx >= inputs.Count)
            {
                _currentVideoSourceIdx = 0;
            }

            _reader.SelectVideoInput(inputs[_currentVideoSourceIdx]);
        }
    }
}