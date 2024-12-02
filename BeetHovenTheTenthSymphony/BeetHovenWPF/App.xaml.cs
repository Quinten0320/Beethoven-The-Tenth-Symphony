using System.Windows;
using BeethovenBusiness;

namespace BeetHovenWPF
{
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            //// Dispose the shared PianoInputHandler instance
            //PianoInputHandlerService.DisposeInstance();

            //base.OnExit(e); 
        }
    }
}
