using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Maartanic
{
	internal class OutputForm : Form
	{
		internal static OutputForm app;
		internal static Graphics windowGraphics;

		// P/Invoke user32.dll to show window with uint 0x09
		[DllImport("user32.dll")]
		private static extern int ShowWindow(IntPtr hWnd, uint Msg);
		private const uint SW_RESTORE = 0x09;
		internal static void Restore(Form form)
		{
			if (form.WindowState == FormWindowState.Minimized)
			{
				ShowWindow(form.Handle, SW_RESTORE);
			}
		}

		internal OutputForm() {}

		internal void StartTimeout()
		{
			bool exitCase = false;
			lock(Program.internalShared.SyncRoot)
			{
				Program.internalShared[3] = "TRUE";
			}
			while (!exitCase)
			{
				try
				{
					Thread.Sleep(Timeout.Infinite);
				}
				catch (ThreadInterruptedException)
				{
					Thread.Sleep(80);
					lock (Program.internalShared.SyncRoot)
					{
						if (Program.internalShared[2] == "TRUE")
						{
							exitCase = true;
						}
					}
				}
			}

			// RESET
			lock (Program.internalShared.SyncRoot)
			{
				Program.internalShared[2] = "FALSE";
			}
			Restore(this);
			Program.EN.ThrowEvent += (sender, args) => { DoSomething(); };
		}

		private void DoSomething() // Excited moment: event works and can minimize a window again!
		{
			BeginInvoke(new Action(() =>// Invoke code onto the windowProcess thread
			{
				SuspendLayout();
				WindowState = FormWindowState.Minimized;
				ResumeLayout(false);
				StartTimeout();
			}));
		}

		[STAThread]
		internal static void Main()
		{
			Application.EnableVisualStyles();
			app = new OutputForm();
			app.InitializeComponent();

			app.SuspendLayout(); // Suspend, change title, resume
			app.Text = app.Text.Insert(17, Program.VERSION + " ");
			app.ResumeLayout(false);
			
			Application.Run(app);
			GC.Collect();
		}

		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// OutputForm
			// 
			this.BackColor = System.Drawing.Color.Black;
			this.ClientSize = new System.Drawing.Size(480, 360);
			this.ForeColor = System.Drawing.Color.Black;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "OutputForm";
			this.Text = "Maartanic Engine Display";
			this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OutputForm_FormClosing);
			this.Shown += new System.EventHandler(this.Form1_Shown);
			this.ResumeLayout(false);

		}

		private void OutputForm_FormClosing(object sender, FormClosedEventArgs e)
		{
			lock(Program.internalShared.SyncRoot)
			{
				Program.internalShared[0] = "FALSE";
				Program.internalShared[1] = "the internal window thread being closed";
			}
			Program.consoleProcess.Interrupt(); // Wake up if sleeping
		}

		private void Form1_Shown(Object sender, EventArgs e)
		{
			StartTimeout(); // Start waiting for a signal
		}

	}
}