using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


namespace StructLib
{
	public class Structure
	{

		#region static
		static readonly Regex validation = new Regex(@"^((i|I|l|L|h|H|b|c|\?|q|Q|f|d|s)([\d]*))+$");
		static readonly Regex extraction = new Regex(@"^((?<type>i|I|l|L|h|H|b|c|\?|q|Q|f|d|s)(?<offset>[\d]*))");

		static readonly Dictionary<string, Type> structs = new Dictionary<string, Type>();

		static readonly Dictionary<char, Type> types = new Dictionary<char, Type>()
		{
			['i'] = typeof(int),
			['I'] = typeof(uint),
			['l'] = typeof(int),
			['L'] = typeof(uint),
			['h'] = typeof(short),
			['H'] = typeof(ushort),
			['b'] = typeof(byte),
			['c'] = typeof(char),
			['?'] = typeof(bool),
			['q'] = typeof(Int64),
			['Q'] = typeof(UInt64),
			['f'] = typeof(float),
			['d'] = typeof(double),
			['s'] = typeof(string)
		};

		private static AssemblyBuilder _builder;
		private static AssemblyBuilder Builder { get => _builder ?? (_builder = GenerateBuilder()); }

		private static ModuleBuilder _module;
		private static ModuleBuilder Module { get => _module ?? (_module = GenerateModuleBuilder()); }

		private static ModuleBuilder GenerateModuleBuilder()
		{
			return Builder.DefineDynamicModule("StructsContainer");
		}

		private static AssemblyBuilder GenerateBuilder()
		{
			var r = new Random();
			string asmName;
			do
			{
				asmName = r.Next().ToString();
			} while ((from asm in AppDomain.CurrentDomain.GetAssemblies() select asm.GetName().Name).Contains(asmName));
			return AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(asmName), AssemblyBuilderAccess.Run);
		}

		public static Type GetStruct(string formula)
		{
			if (structs.Keys.Contains(formula))
				return structs[formula];
			else
				return CreateStruct(formula);
		}

		public static object Unpack(string formula, object[] values)
		{
			return Unpack(GetStruct(formula), values);
		}

		private static object Unpack(Type type, object[] values)
		{
			var instance = Activator.CreateInstance(type);
			int counter = 0;
			foreach (var value in values)
			{
				type.GetField(FieldName(counter++)).SetValue(instance, value);
			}
			return instance;
		}

		private static Type CreateStruct(string formula)
		{
			if (!validation.Match(formula).Success)
				throw new Exception("Invalid formula");

			var typeBuilder = Module.DefineType(formula,
				TypeAttributes.Public |
				TypeAttributes.Class |
				TypeAttributes.ExplicitLayout |
				TypeAttributes.Sealed |
				TypeAttributes.BeforeFieldInit,
				typeof(System.ValueType));

			List<Tuple<Type, int>> extracted = ExtractTypes(formula);

			int counter = 0;
			foreach (var tuple in extracted)
			{
				var field = typeBuilder.DefineField(FieldName(counter++), tuple.Item1, FieldAttributes.Public | FieldAttributes.HasFieldRVA);
				field.SetOffset(tuple.Item2);
			}
			return structs[formula] = typeBuilder.CreateType();
		}

		private static string FieldName(int counter)
		{
			return $"Field{counter}";
		}

		private static List<Tuple<Type, int>> ExtractTypes(string formula)
		{
			List<Tuple<Type, int>> extracted = new List<Tuple<Type, int>>();

			int lastOffset = 0;
			while (formula != "")
			{
				var match = extraction.Match(formula);
				Type type = types[match.Groups["type"].Value[0]];
				int offset = ExtractOffset(match.Groups["offset"].Value, type, ref lastOffset);
				extracted.Add(new Tuple<Type, int>(type, offset));
				formula = formula.Substring(match.Length);
			}
			return extracted;
		}

		private static int ExtractOffset(string strOffset, Type currentType, ref int lastOffset)
		{
			if (strOffset == "")
			{
				int retval = lastOffset;
				lastOffset += Marshal.SizeOf(currentType);
				return retval;
			}
			else
			{
				return int.Parse(strOffset);
			}
		}
		#endregion
	}
}
