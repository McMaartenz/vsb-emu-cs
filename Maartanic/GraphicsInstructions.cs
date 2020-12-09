using System.Linq;
using System.Drawing;

namespace Maartanic
{
	internal static class GraphicsInstructions
	{
		private static Color temp;
		private static readonly string[] VSBInstructions = new string[]
			{
				"SCREENLN",
				"SCREENREC",
				"SCREENFILL",
				"SCREENOUT"
			};

		private static T Parse<T>(string input)
		{
			return Program.Parse<T>(input);
		}

		private static void SwitchValues(ref string a, ref string b)
		{
			string tmp = a;
			a = b;
			b = tmp;
		}

		private static void VSBHandle(int s, string instr, ref string[] args)
		{
			if (VSBInstructions.Contains(instr))
			{
				if (instr == "SCREENOUT")
				{
					SwitchValues(ref args[^2], ref args[^1]);
				}
				if (s < 1)
				{
					temp = Program.graphics.GetColor();
					Program.graphics.SetColor(Program.HexHTML(args[^1]));
				}
				else
				{
					Program.graphics.SetColor(temp);
				}
			}
		}

		internal static string Instructions(Engine e, ref string[] lineInfo, ref string[] args)
		{
			switch (lineInfo[0].ToUpper())
			{
				case "SCREENUPD":
					break;

				case "SCREENLN": // VSB compat
				case "PLINE": // PLINE [x] [y] [x 1] [y 1] r-r-r-r
					VSBHandle(0, lineInfo[0].ToUpper(), ref args);
					Program.graphics.Line(Parse<float>(args[0]), Parse<float>(args[1]), Parse<float>(args[2]), Parse<float>(args[3]));
					VSBHandle(1, lineInfo[0].ToUpper(), ref args);
					break;

				case "PCOL": // PCOL [Color] r
					Program.graphics.SetColor(Program.HexHTML(args[0]));
					break;

				case "SCREENREC": // VSB compat
				case "PRECT": // PRECT [x] [y] [w] [h] r-r-r-r
					{
						float x = Parse<float>(args[0]);
						float y = Parse<float>(args[1]);
						float w = Parse<float>(args[2]);
						float h = Parse<float>(args[3]);

						if (lineInfo[0].ToUpper() == "SCREENREC")
						{
							w -= x;
							h -= y;
						}
						VSBHandle(0, lineInfo[0].ToUpper(), ref args);
						Program.graphics.Rectangle(x, y, w, h);
						VSBHandle(1, lineInfo[0].ToUpper(), ref args);
					}
					break;

				case "SCREENFILL": // VSB compat
				case "PFILL": // PFILL [color] r
					Program.graphics.Fill(Program.HexHTML(args[0]));
					break;

				case "SCREENPX": // VSB compat
				case "PPX": // PPX [x] [y] r-r
					VSBHandle(0, lineInfo[0].ToUpper(), ref args);
					Program.graphics.Pixel(Parse<float>(args[0]), Parse<float>(args[1]));
					VSBHandle(1, lineInfo[0].ToUpper(), ref args);
					break;

				case "SCREENOUT": // VSB compat
				case "PWRITE": // PWRITE [x] [y] [text] r-r-r
					VSBHandle(0, lineInfo[0].ToUpper(), ref args);
					Program.graphics.Write(Parse<float>(args[0]), Parse<float>(args[1]), args[2]);
					VSBHandle(1, lineInfo[0].ToUpper(), ref args);
					break;

				default:
					e.SendMessage(Engine.Level.ERR, $"Unrecognized instruction \"{lineInfo[0]}\". (GPU.)");
					break;
			}
			return null;
		}
	}
}