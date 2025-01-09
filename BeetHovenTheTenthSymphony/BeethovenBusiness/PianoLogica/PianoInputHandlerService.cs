using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness.PianoLogica
{
    public sealed class PianoInputHandlerService
    {
        private static readonly Lazy<PianoInputHandler> _instance = new(() => new PianoInputHandler());

        public static PianoInputHandler Instance => _instance.Value;

        private PianoInputHandlerService() { }
    }
}
