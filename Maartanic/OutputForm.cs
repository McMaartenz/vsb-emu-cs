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

		internal static IntPtr GetHandle(Form form)
		{
			return (IntPtr) form.Invoke(new Func<IntPtr>(() => { return form.Handle; }));
		}

		internal static void Restore(Form form)
		{
			form.SuspendLayout();
			form.ShowInTaskbar = true;
			form.FormBorderStyle = FormBorderStyle.FixedSingle;
			form.ResumeLayout(false);
			if (form.WindowState == FormWindowState.Minimized)
			{
				ShowWindow(form.Handle, SW_RESTORE);
			}
		}

		// Unnecessary
		//internal OutputForm() { }

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
			Restore(this);
			Program.EN.MinimizeNow += (sender, args) => { Minimize(); };
			lock (Program.internalShared.SyncRoot)
			{
				Program.internalShared[2] = "FALSE";
			}
		}

		internal static bool StartWithGraphics()
		{
			DialogResult result = MessageBox.Show(
				"Starting in VSB/compatibility mode can be done with graphics enabled or disabled accordingly to what a script expects. Do you wish to start with graphics enabled?",
				"Program startup",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question
			);
			return result == DialogResult.Yes;
		}

		internal static bool ErrorMessage(string message)
		{
			DialogResult result = MessageBox.Show(
				message + " Do you wish to continue executing? Hit cancel to ignore further errors.",
				"Application Error",
				MessageBoxButtons.YesNoCancel,
				MessageBoxIcon.Error
			);
			if (result == DialogResult.Cancel)
			{
				Program.stopAsking = true;
				return true;
			}
			return result == DialogResult.Yes;
		}

		internal static void CriticalError(string message)
		{
			MessageBox.Show(
				message,
				"Internal MRT error",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error
			);
		}

		private void Minimize() // Excited moment: event works and can minimize a window again!
		{
			BeginInvoke(new Action(() =>// Invoke code onto the windowProcess thread
			{
				SuspendLayout();
				WindowState = FormWindowState.Minimized;
				ShowInTaskbar = false;
				FormBorderStyle = FormBorderStyle.FixedToolWindow;
				ResumeLayout(false);
				StartTimeout();
			}));
		}

		internal static string output;

		internal string AskInput()
		{
			output = "error";
			Invoke(new Action(() =>
			{
				using Form inputWindow = new Form();

				Label inputWindowLabel = new Label();
				TextBox inputWindowTextBox = new TextBox();
				Button inputWindowOK = new Button();
				Button inputWindowCANCEL = new Button();

				inputWindow.Text = "User input required";
				inputWindowLabel.Text = "This program is asking for user input:";
				inputWindowTextBox.PlaceholderText = "Enter text..";
				inputWindowOK.Text = "OK";
				inputWindowCANCEL.Text = "Cancel";
				inputWindowOK.DialogResult = DialogResult.OK;
				inputWindowCANCEL.DialogResult = DialogResult.Cancel;
				inputWindowLabel.SetBounds(9, 10, 372, 13);
				inputWindowTextBox.SetBounds(12, 36, 372, 20);
				inputWindowOK.SetBounds(228, 72, 75, 23);
				inputWindowCANCEL.SetBounds(309, 72, 75, 23);
				inputWindowLabel.AutoSize = true;
				inputWindowTextBox.Anchor |= AnchorStyles.Right;
				inputWindowOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
				inputWindowCANCEL.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
				inputWindow.ClientSize = new Size(396, 107);
				inputWindow.Controls.AddRange(new Control[] { inputWindowLabel, inputWindowTextBox, inputWindowOK, inputWindowCANCEL });
				inputWindow.ClientSize = new Size(Math.Max(300, inputWindowLabel.Right + 10), inputWindow.ClientSize.Height);
				inputWindow.FormBorderStyle = FormBorderStyle.FixedDialog;
				inputWindow.StartPosition = FormStartPosition.CenterParent;
				inputWindow.MinimizeBox = false;
				inputWindow.MaximizeBox = false;
				inputWindow.AcceptButton = inputWindowOK;
				inputWindow.CancelButton = inputWindowCANCEL;

				if (inputWindow.ShowDialog() == DialogResult.OK)
				{
					output = inputWindowTextBox.Text;
				}
				else
				{
					output = "ERROR";
				}
			}));
			return output;
		}

		private static int MakeInRange(int value, int min, int max)
		{
			if (value > max)
			{
				value = max;
			}
			else if (value < min)
			{
				value = min;
			}
			return value;
		}

		// System.Drawing.Point
		internal int GetMouseX() 
		{
			int ret = 0;
			if (Program.SettingExtendedMode == Engine.Mode.ENABLED)
			{
				Invoke(new Action(() =>
				{
					ret = MakeInRange(PointToClient(Cursor.Position).X, 0, Program.WIN_WIDTH);
				}));
			}
			else
			{
				Program.EN.SendMessage(Engine.Level.ERR, "Attempted to access mouse X position outside of extended mode.");
			}
			return ret;
		}

		internal int GetMouseY()
		{
			int ret = 0;
			if (Program.SettingExtendedMode == Engine.Mode.ENABLED)
			{
				Invoke(new Action(() =>
				{
					ret = MakeInRange(PointToClient(Cursor.Position).Y, 0, Program.WIN_HEIGHT);
				}));
			}
			else
			{
				Program.EN.SendMessage(Engine.Level.ERR, "Attempted to access mouse Y position outside of extended mode.");
			}
			return ret;
		}

		[STAThread]
		internal static void Main()
		{
			Application.EnableVisualStyles();
			app = new OutputForm();
			app.InitializeComponent();

			app.SuspendLayout(); // Suspend, change title, resume
			app.Text = app.Text.Insert(17, Program.VERSION + " ");
			app.ClientSize = new Size(Program.WIN_WIDTH, Program.WIN_HEIGHT);
			app.ResumeLayout(false);
			
			Application.Run(app);
		}

		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// OutputForm
			// 
			this.BackColor = System.Drawing.Color.Black;
			this.ClientSize = new System.Drawing.Size(480, 360);
			this.DoubleBuffered = true;
			this.ForeColor = System.Drawing.Color.Black;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "OutputForm";
			this.ShowInTaskbar = false;
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