using System;
using System.Drawing;
using System.Windows.Forms;

namespace Maartanic
{
	public class OutputForm : Form
	{
		public static OutputForm app;
		public OutputForm()
		{
		}

		[STAThread]
		public static void Main()
		{
			Application.EnableVisualStyles();
			app = new OutputForm();
			app.InitializeComponent();

			app.SuspendLayout();
			app.Text = app.Text.Insert(17, Program.VERSION + " ");
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
			this.ForeColor = System.Drawing.Color.Black;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "OutputForm";
			this.Text = "Maartanic Engine Display";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OutputForm_FormClosing);
			this.ResumeLayout(false);

		}

		private void OutputForm_FormClosing(object sender, FormClosedEventArgs e)
		{
			// Gets called when closing
		}
	}
}