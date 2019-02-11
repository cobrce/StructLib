using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace StructLib.Internal
{
	interface IPackedType
	{
		bool IsArray { get; }

		void SetFieldArrayValue(object instance, FieldInfo field, object value);

		void SetFieldValue(object instance, FieldInfo field, object data);

		Type FieldBaseType { get; } // original Type

		Type FieldFinalType { get; } // after making the Type array or not

		object InitialValue { get; set; }

		int ArraySize { get; }

		void UnpackToField(object instance, FieldInfo field, BinaryReader reader);

		DataCollection Pack(object value);

		int FieldLength(Field field);
	}

	static class PackedType
	{
		internal static MethodInfo GetConversionMethod(Type type)
		{
			return typeof(BinaryReader).GetMethod($"Read{type.Name}");
			//foreach (var method in typeof(BinaryReader).GetMethods())
			//	if (method.ReturnType == type && method.GetParameters().Length == 0 && method.Name.Length > 4 && method.Name.StartsWith("Read"))
			//		return method;
			//return null;
		}

		internal static readonly Dictionary<Type, MethodInfo> conversion = new Dictionary<Type, MethodInfo>();

		static Regex extraction = new Regex(@"^((?<type>i|I|l|L|h|H|b|c|\?|q|Q|f|d|s)(?<array>\[(?<size>[\d]*)\])?)");

		public static IPackedType ParseFormat(string singleFormat)
		{
			var match = extraction.Match(singleFormat);
			if (!match.Success)
				return null;
			var packedType = packedTypes[match.Groups["type"].Value[0]];

			int.TryParse(match.Groups["size"].Value, out int arraySize);

			return (IPackedType)Activator.CreateInstance(packedType, new object[] { match.Groups["array"].Length != 0, arraySize });
		}

		static readonly Dictionary<char, Type> packedTypes = new Dictionary<char, Type>()
		{
			['i'] = typeof(PackedType<int>),
			['I'] = typeof(PackedType<uint>),
			['l'] = typeof(PackedType<int>),
			['L'] = typeof(PackedType<uint>),
			['h'] = typeof(PackedType<short>),
			['H'] = typeof(PackedType<ushort>),
			['b'] = typeof(PackedType<byte>),
			['c'] = typeof(PackedType<char>),
			['?'] = typeof(PackedType<bool>),
			['q'] = typeof(PackedType<Int64>),
			['Q'] = typeof(PackedType<UInt64>),
			['f'] = typeof(PackedType<float>),
			['d'] = typeof(PackedType<double>),
			['s'] = typeof(PackedType<string>)
		};
	}

	class PackedType<T> : IPackedType
	{
		public bool IsArray { get; private set; }

		public Type FieldBaseType { get => typeof(T); }

		public int ArraySize { get; private set; }

		public object InitialValue { get; set; }

		public Type FieldFinalType { get; private set; }

		public PackedType(bool isArray, int arraySize = 0)
		{
			ArraySize = arraySize;
			IsArray = isArray;
			FieldFinalType = IsArray ? typeof(T[]) : typeof(T);
		}

		public DataCollection Pack(object value)
		{
			DataCollection collection = new DataCollection();
			if (value is T || (value.GetType().IsArray && value.GetType().GetElementType() == typeof(T)))
			{
				if (value.GetType().IsArray)
				{
					foreach (var v in (Array)value)
					{
						collection.Add(MarshalReadBytes(v, Marshal.SizeOf(typeof(T))));
					}
				}
				else
				{
					collection.Add(MarshalReadBytes(value, Marshal.SizeOf(typeof(T))));
				}
			}
			return collection;
		}

		private static byte[] MarshalReadBytes(object value, int len)
		{
			byte[] data = new byte[len];
			var ptr = Marshal.AllocHGlobal(data.Length);
			Marshal.StructureToPtr(value, ptr, false);
			Marshal.Copy(ptr, data, 0, data.Length);
			return data;
		}

		public void UnpackToField(object instance, FieldInfo field, BinaryReader reader)
		{
			var data = new List<T>();
			if (!PackedType.conversion.ContainsKey(typeof(T)))
				PackedType.conversion[typeof(T)] = PackedType.GetConversionMethod(typeof(T));

			for (int i = 0; i < ArraySize || (!IsArray && i == 0); i++)
				data.Add((T)PackedType.conversion[typeof(T)].Invoke(reader, new object[0]));

			SetFieldValue(instance, field, data.ToArray());
		}

		public void SetFieldValue(object instance, FieldInfo field, object data)
		{
			if (!data?.GetType().IsArray == true)
				data = new object[] { data };

			if (IsArray)
				SetFieldArrayValue(instance, field, data);
			else
				field.SetValue(instance, ((Array)data).GetValue(0));
		}

		public void SetFieldArrayValue(object instance, FieldInfo field, object value)
		{
			var values = ExtractTArray((Array)value);
			var elements = new T[ArraySize == 0 ? values.Length : ArraySize];

			int i = 0;
			for (; i < elements.Length && i < values.Length; i++)
				elements[i] = values[i];

			for (; i < ArraySize; i++)
				elements[i] = (T)Activator.CreateInstance(FieldBaseType);

			field.SetValue(instance, elements);
		}

		private T[] ExtractTArray(Array value)
		{
			var values = new T[value?.Length ?? 0];

			for (int i = 0; i < values.Length; i++)
				values[i] = (T)value.GetValue(i);
			return values;
		}

		public int FieldLength(Field field)
		{
			int multiplier = 1;
			if (IsArray)
				multiplier = ((Array)field.Value).Length;
			return Marshal.SizeOf(FieldBaseType) * multiplier;
		}

	}
}
