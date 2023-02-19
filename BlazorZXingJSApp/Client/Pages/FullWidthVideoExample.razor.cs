using BlazorBarcodeScanner.ZXing.JS;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorZXingJSApp.Client.Pages
{
    public partial class FullWidthVideoExample
    {
        private BarcodeReader _reader;
        private int StreamWidth = 720;
        private int StreamHeight = 540;

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

        private async Task LocalReceivedBarcodeText(BarcodeReceivedEventArgs args)
        {
            await InvokeAsync(() => {
                this.LocalBarcodeText = args.BarcodeText;
                
                StateHasChanged();
                _reader.StopDecoding();
            });
        }
    }
}