using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace StructLib
{
	public class Generator
	{
		readonly Type type;
		readonly string format;

		public Generator(string format, Type type)
		{
			this.type = type;
			this.format = format;
		}

		public Structure CreateInstance(byte[] data = null)
		{

			var instance = new Structure(type, Activator.CreateInstance(type));
			if (data!=null)
			{
				instance.Unpack(data);
			}
			return instance;
		}

		//private unsafe object CreateInstanceWithData(byte[] data)
		//{
		//	object instance = Activator.CreateInstance(type);

		//	/// doesn't work with valuetypes
		//	//if (data != null)
		//	//{
		//	//	fixed (byte* ptr = &data[0])
		//	//	{
		//	//		Marshal.PtrToStructure((IntPtr)ptr, instance);
		//	//	}
		//	//}


		//	return instance;
		//}

		#region static

		static readonly Dictionary<string, Generator> generators = new Dictionary<string, Generator>();

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

		public static Generator CreateGenerator(string formula)
		{
			var dictionary = new Dictionary<string, char>();

			for (int i = 0; i < formula.Length; i++)
				dictionary[FieldName(i)] = formula[i];

			return GenerateOrReturn(JsonConvert.SerializeObject(dictionary), dictionary);
		}

		private static Generator GenerateOrReturn(string jsonFormula, Dictionary<string, char> dictionary)
		{
			if (generators.Keys.Contains(jsonFormula))
				return generators[jsonFormula];
			else
				return CreateGeneratorInternal(jsonFormula, dictionary);
		}

		public static Generator CreateGeneratorJson(string jsonformula)
		{
			// deserialize then serialize to avoid recreating Generators for the same formulas that have a slightly diffent json
			var dictionary = JsonConvert.DeserializeObject<Dictionary<string, char>>(jsonformula);
			return GenerateOrReturn(JsonConvert.SerializeObject(dictionary), dictionary);
		}

		public static Structure Unpack(string formula, object[] values)
		{
			return Unpack(CreateGenerator(formula), values);
		}

		public static Structure Unpack(string formula, byte[] data)
		{
			return Unpack(CreateGenerator(formula), data);
		}

		public static Structure UnpackJSon(string jsonFormula, object[] values)
		{
			return Unpack(CreateGeneratorJson(jsonFormula), values);
		}

		public static Structure UnpackJSon(string jsonFormula, byte[] data)
		{
			return Unpack(CreateGeneratorJson(jsonFormula), data);
		}

		private static Structure Unpack(Generator generator, object[] values)
		{
			var instance = generator.CreateInstance();
			instance.Unpack(values);
			return instance;
		}

		private static Structure Unpack(Generator generator, byte[] values)
		{
			return generator.CreateInstance(values);
		}

		private static Generator CreateGeneratorInternal(string jsonFormula, Dictionary<string, char> fieldTypeDictionary)
		{
			foreach (char c in fieldTypeDictionary.Values)
				if (!types.ContainsKey(c))
					throw new Exception($"Unknown format {c}");

			var typeBuilder = Module.DefineType(jsonFormula,
				TypeAttributes.Public |
				TypeAttributes.ExplicitLayout |
				TypeAttributes.Sealed |
				TypeAttributes.Serializable,
				typeof(System.ValueType));

			List<Tuple<string, Type, int>> extracted = ExtractTypes(fieldTypeDictionary);

			foreach (var tuple in extracted)
			{
				var field = typeBuilder.DefineField(tuple.Item1, tuple.Item2, FieldAttributes.Public | FieldAttributes.HasFieldRVA);
				field.SetOffset(tuple.Item3);
			}
			return generators[jsonFormula] = new Generator(jsonFormula, typeBuilder.CreateType());

		}

		private static string FieldName(int counter)
		{
			return $"Field{counter}";
		}

		private static List<Tuple<string, Type, int>> ExtractTypes(Dictionary<string, char> fieldTypeDictionary)
		{
			List<Tuple<string, Type, int>> extracted = new List<Tuple<string, Type, int>>();

			int offset = 0;
			foreach (var fieldName in fieldTypeDictionary.Keys)
			{
				Type type = types[fieldTypeDictionary[fieldName]];
				extracted.Add(new Tuple<string, Type, int>(fieldName, type, offset));
				offset += Marshal.SizeOf(type);
			}
			return extracted;
		}
		#endregion
	}
}
