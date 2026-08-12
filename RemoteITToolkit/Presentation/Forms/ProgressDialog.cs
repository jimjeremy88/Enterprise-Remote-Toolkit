using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RemoteITToolkit.Presentation.Helpers;

namespace RemoteITToolkit.Presentation.Forms
{
    public class ProgressDialog : Form
    {
        private Label _lblMessage;

        public ProgressDialog(string initialMessage)
        {
            InitializeComponent(initialMessage);
        }

        private void InitializeComponent(string msg)
        {
            this.Size = new Size(350, 150);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            _lblMessage = new Label
            {
                Text = msg,
                AutoSize = false,
                Size = new Size(330, 30),
                Location = new Point(10, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F)
            };

            var panelBorder = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            panelBorder.Controls.Add(_lblMessage);

            this.Controls.Add(panelBorder);
        }

        public void UpdateMessage(string msg)
        {
            this.SafeInvoke(() => _lblMessage.Text = msg);
        }

        public static async Task ShowAsync(Form parent, string message, Func<ProgressDialog, Task> backgroundTask)
        {
            using (var dialog = new ProgressDialog(message))
            {
                var tcs = new TaskCompletionSource<bool>();

                dialog.Load += async (s, e) =>
                {
                    try
                    {
                        await backgroundTask(dialog);
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                    finally
                    {
                        dialog.Close();
                    }
                };

                dialog.ShowDialog(parent);
                await tcs.Task;
            }
        }
    }
}