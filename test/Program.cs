using StructLib;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace test
{
	
	class Program
	{
		static void Main(string[] args)
		{
			
			string jsonFormula = "{'A' : 'I','B' :'b', 'C' : 'c'}";

			var gen = Generator.CreateGeneratorJson(jsonFormula);
			var inst = gen.CreateInstance();

			inst["A"].Value = 1U;
			inst["B"].Value = (byte)2;
			inst["C"].Value = 'C';

			PrintHex(inst.Pack());

			var inst2 = Generator.UnpackJSon(jsonFormula, inst.Pack());
			PrintHex(inst2.Pack());
			
			Console.WriteLine($"inst {(inst.Equals(inst2) ? "eqauls" : "different from")} inst2");
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
