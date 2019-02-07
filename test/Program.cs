using StructLib;
using System;

namespace test
{

	class Program
	{
		static void Main(string[] args)
		{

			string jsonFormula = "{'A' : 'I'," +  // A is an uint
				"'B' :'b'," + // B is a byte
				"'C' : 'c'," + // C is a char 
				"'D':'b[10]'," + // D is a byte[] with fixed size
				"'E' : 'b[]'}"; // E is a byte[] with variable size

			#region Defining struct
			var gen = Generator.CreateGeneratorJson(jsonFormula);
			// initial values are applied to all isntances and should be applied before instantiation
			gen.FieldInfos["D"].InitialValue = new byte[] { 0, 1, 2, 3 }; // (initial)values to fixed size arrays will be trunkated/padded to fill the size
			gen.FieldInfos["E"].InitialValue = new byte[] { 4, 5, 6, 7 }; // variable size array get (inital)values as they are
			#endregion

			#region Creating inst and updating variables
			var inst = gen.CreateInstance();

			inst["A"].Value = 1U;
			inst["B"].Value = (byte)2;
			inst["C"].Value = 'C';

			Console.Write("inst.Pack() = ");
			PrintHex(inst.Pack());
			Console.WriteLine($"inst.D.Length = {((byte[])inst["D"].Value).Length}");
			Console.WriteLine($"inst.E.Length = {((byte[])inst["E"].Value).Length}");
			#endregion

			#region Creating inst2 based on bytes from inst and comparing them
			/////////////////////////////////////////////////////////////////////////////////
			// when a Structure is populated with unpack, variable size fields are ignored //
			/////////////////////////////////////////////////////////////////////////////////
			var inst2 = Generator.UnpackJSon(jsonFormula, inst.Pack());
			Console.WriteLine("");
			Console.Write("inst2.Pack() = ");
			PrintHex(inst2.Pack());
			Console.WriteLine($"inst {(inst.Equals(inst2) ? "eqauls" : "different from")} inst2");
			#endregion

			#region Modifying inst2 then compare again
			inst2["E"].Value = new byte[] { 4, 5, 6, 7 };
			Console.WriteLine("");
			Console.Write("inst2.E = ");
			PrintHex((byte[])inst2["E"].Value);
			Console.WriteLine($"inst2.E.Length = {((byte[])inst2["E"].Value).Length}");
			Console.Write("inst2.Pack() = ");
			PrintHex(inst2.Pack());
			Console.WriteLine($"inst {(inst.Equals(inst2) ? "eqauls" : "different from")} inst2");
			#endregion
				
			Console.ReadLine();
		}

		private static void PrintHex(byte[] data)
		{
			foreach (byte b in data)
			{
				Console.Write($"{b:x02} ");
			}
			Console.WriteLine("");
		}
	}
}
