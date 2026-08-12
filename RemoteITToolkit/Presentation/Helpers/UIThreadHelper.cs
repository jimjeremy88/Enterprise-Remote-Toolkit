using System;
using System.Windows.Forms;

namespace RemoteITToolkit.Presentation.Helpers
{
    public static class UIThreadHelper
    {
        public static void SafeInvoke(this Control control, Action action)
        {
            if (control != null && !control.IsDisposed && control.IsHandleCreated)
            {
                if (control.InvokeRequired)
                {
                    control.Invoke(action);
                }
                else
                {
                    action();
                }
            }
        }
    }
}