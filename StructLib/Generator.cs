using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using StructLib.Internal;

namespace StructLib
{
	public class Generator
	{
		readonly List<FieldCreationInfo> fieldInfos;

		public Dictionary<string, FieldCreationInfo> FieldInfos => fieldInfos.ToDictionary(f => f.FieldName);


		readonly Type type;
		readonly string format;

		private Generator(string format, List<FieldCreationInfo> fieldInfos, Type type)
		{
			this.fieldInfos = fieldInfos;
			this.type = type;
			this.format = format;
		}

		public Structure CreateInstance(byte[] data = null)
		{
			var instance = new Structure(type, fieldInfos, Activator.CreateInstance(type));

			if (data != null)
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
			var dictionary = new Dictionary<string, string>();

			for (int i = 0; i < formula.Length; i++)
				dictionary[FieldName(i)] = formula[i].ToString();

			return GenerateOrReturn(JsonConvert.SerializeObject(dictionary), dictionary);
		}

		private static Generator GenerateOrReturn(string jsonFormula, Dictionary<string, string> dictionary)
		{
			if (generators.Keys.Contains(jsonFormula))
				return generators[jsonFormula];
			else
				return CreateGeneratorInternal(jsonFormula, dictionary);
		}

		public static Generator CreateGeneratorJson(string jsonformula)
		{
			// deserialize then serialize to avoid recreating Generators for the same formulas that have a slightly diffent json
			var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonformula);
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

		private static Generator CreateGeneratorInternal(string jsonFormula, Dictionary<string, string> fieldTypeDictionary)
		{
			var typeBuilder = Module.DefineType(jsonFormula,
				TypeAttributes.Public |
				//TypeAttributes.ExplicitLayout |
				TypeAttributes.Sealed |
				TypeAttributes.Serializable,
				typeof(System.ValueType));

			var fieldInfos = ExtractTypes(fieldTypeDictionary);

			foreach (var fieldInfo in fieldInfos)
			{
				var field = typeBuilder.DefineField(fieldInfo.FieldName, fieldInfo.PackedType.FieldFinalType, FieldAttributes.Public | FieldAttributes.HasFieldRVA);
				//field.SetOffset(fieldInfo.Offset);
				fieldInfo.Field = field;
			}
			return generators[jsonFormula] = new Generator(jsonFormula, fieldInfos, typeBuilder.CreateType());

		}

		private static string FieldName(int counter)
		{
			return $"Field{counter}";
		}

		private static List<FieldCreationInfo> ExtractTypes(Dictionary<string, string> fieldTypeDictionary)
		{
			var extracted = new List<FieldCreationInfo>();
			int offset = 0;
			foreach (var fieldName in fieldTypeDictionary.Keys)
			{
				IPackedType packedType = PackedType.ParseFormat(fieldTypeDictionary[fieldName]);
				if (packedType != null)
				{
					extracted.Add(new FieldCreationInfo(fieldName, packedType, offset));
					offset += Marshal.SizeOf(packedType.FieldBaseType);
				}
				else
				{
					throw new Exception($"Can't parse {fieldName}:{fieldTypeDictionary}");
				}
			}
			return extracted;
		}
		#endregion
	}
}
