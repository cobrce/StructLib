using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StructLib
{
	public class Structure
	{
		private readonly Type type;
		public readonly object Instance;
		private Dictionary<string, Field> fields = new Dictionary<string, Field>();

		public void Unpack(byte[] data)
		{
			using (var ms = new MemoryStream(data))
			using (var br = new BinaryReader(ms))
				foreach (var field in GetFields())
				{
					field.Value = br.ReadValue(field.FieldInfo.FieldType);
				}
		}

		public void Unpack(object[] values)
		{
			foreach (var tuple in GetFields().Zip(values, (f, v) => new { f, v }))
				tuple.f.Value = tuple.v;
		}

		public override bool Equals(object obj)
		{
			if (obj is Structure s)
			{
				var remoteFields = s.GetFields().ToArray();
				var myFields = GetFields().ToArray();

				if (remoteFields.Length == myFields.Length)
				{
					var zipped = myFields.Zip(remoteFields, (m, r) => new { m, r });
					foreach (var pair in zipped)
						if (!pair.m.Value.Equals(pair.m.Value))
							return false;
					return true;
				}
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return Instance.GetHashCode();
		}

		internal Structure(Type type, object instance)
		{
			this.type = type;
			this.Instance = instance;

			foreach (var field in type.GetFields())
			{
				Length += Marshal.SizeOf(field.FieldType);
				fields[field.Name] = new Field(field, instance);
			}
		}

		public int Length { get; private set; } = 0;

		public byte[] Pack()
		{
			byte[] data = new byte[this.Length];
			var ptr = Marshal.AllocHGlobal(data.Length);
			Marshal.StructureToPtr(Instance, ptr, false);
			Marshal.Copy(ptr, data, 0, data.Length);
			return data;
		}

		public IEnumerable<Field> GetFields()
		{
			foreach (var field in fields.Values)
				yield return field;
		}

		public Field this[string fieldName]
		{
			get => fields.ContainsKey(fieldName) ? fields[fieldName] : null;
		}

	}
	public class Field
	{

		private readonly object instance;
		private FieldInfo field;
		public FieldInfo FieldInfo { get => field; }


		public Field(FieldInfo field, object instance)
		{
			this.field = field;
			this.instance = instance;
		}

		public object Value
		{
			get => field.GetValue(instance);
			set => field.SetValue(instance, value);
		}
	}

	public static class TypeExtension
	{
		private static Dictionary<Type, MethodInfo> conversion = new Dictionary<Type, MethodInfo>();


		public static object ReadValue(this BinaryReader reader, Type type)
		{
			try
			{
				if (!conversion.ContainsKey(type))
					conversion[type] = GetConversionMethod(type);
				return conversion[type].Invoke(reader, new object[0]);
			}
			catch
			{
				throw new InvalidDataException();
			}
		}

		private static MethodInfo GetConversionMethod(Type type)
		{
			foreach (var method in typeof(BinaryReader).GetMethods())
				if (method.ReturnType == type && method.GetParameters().Length == 0 && method.Name.StartsWith("Read"))
					return method;
			return null;
		}
	}
}