using BlazorBarcodeScanner.ZXing.JS;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorZXingJSApp.Client.Pages
{
    public partial class Index
    {
        private BarcodeReader _reader;
        private int StreamWidth = 720;
        private int StreamHeight = 540;

        private string LocalBarcodeText;
        private int _currentVideoSourceIdx = 0;

        private string _imgSrc = string.Empty;
        private string _lastError = string.Empty;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender && !string.IsNullOrWhiteSpace(_reader.SelectedVideoInputId))
            {
                _currentVideoSourceIdx = SourceIndexFromId();
            }
        }

        private int SourceIndexFromId()
        {
            var inputs = _reader.VideoInputDevices.ToList();
            int result;
            for (result = 0; result < inputs.Count; result++)
            {
                if (inputs[result].DeviceId.Equals(_reader.SelectedVideoInputId))
                {
                    break;
                }
            }
            return result;
        }

        private async Task LocalReceivedBarcodeText(BarcodeReceivedEventArgs args)
        {
            this.LocalBarcodeText = args.BarcodeText;
            await _reader.StopDecoding();
        }

        private void LocalReceivedError(ErrorReceivedEventArgs args)
        {
            this._lastError = args.Message;
        }

        private async Task CapturePicture()
        {
            _imgSrc = await _reader.Capture();
            StateHasChanged();
        }

        private async Task OnVideoSourceNext(MouseEventArgs args)
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

            await _reader.SelectVideoInput(inputs[_currentVideoSourceIdx]);
        }
    }
}