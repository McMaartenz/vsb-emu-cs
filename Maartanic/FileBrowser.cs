using System;
using System.Windows.Forms;

namespace Maartanic
{
	internal class FileBrowser
	{
		internal static string returnedFile;

		[STAThread]
		internal static void Main()
		{
			MessageBox.Show("No arguments were specified as start script, therefore one must now be selected in order to continue.", "No file specified in arguments", MessageBoxButtons.OK, MessageBoxIcon.Information);
			using OpenFileDialog chooseFile	= new OpenFileDialog
			{
				InitialDirectory = Application.StartupPath,
				Filter = "Maartanic Engine files (*.mrt)|*.mrt|VSB Engine Files (*.vsb)|*.vsb|All files (*.*)|*.*",
				RestoreDirectory = true
			};
			if (chooseFile.ShowDialog() == DialogResult.OK)
			{
				returnedFile = chooseFile.FileName;
			}
			else
			{
				returnedFile = null;
			}
		}
	}
}