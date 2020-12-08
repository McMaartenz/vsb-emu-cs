using System;

namespace Maartanic
{
	internal static class GraphicsInstructions
	{
		private static T Parse<T>(string input)
		{
			return Program.Parse<T>(input);
		}

		internal static string Instructions(Engine e, ref string[] lineInfo, ref string[] args)
		{
			switch (lineInfo[0].ToUpper())
			{
				case "SCREENLN": // VSB compat
				case "PLINE": // PLINE [x] [y] [x 1] [y 1] r-r-r-r
					Program.graphics.Line(Parse<float>(args[0]), Parse<float>(args[1]), Parse<float>(args[2]), Parse<float>(args[3]));
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

						Program.graphics.Rectangle(x, y, w, h);
					}
					break;

				case "SCREENFILL": // VSB compat
				case "PFILL": // PFILL [color] r
					Program.graphics.Fill(Program.HexHTML(args[0]));
					break;

				case "SCREENPX": // VSB compat
				case "PPX": // PPX [x] [y] r-r
					Program.graphics.Pixel(Parse<float>(args[0]), Parse<float>(args[1]));
					break;

				default:
					e.SendMessage(Engine.Level.ERR, $"Unrecognized instruction \"{lineInfo[0]}\". (EXT.)");
					break;
			}
			return null;
		}
	}
}