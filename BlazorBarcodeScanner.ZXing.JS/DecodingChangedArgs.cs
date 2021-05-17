using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBarcodeScanner.ZXing.JS
{
    public class DecodingChangedArgs
    {
        public BarcodeReader Sender;
        public bool IsDecoding;
    }
}
