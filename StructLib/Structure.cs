using StructLib.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
			{
				foreach (var field in GetFields())
				{
					field.Unpack(br);//.ReadValue(field.FieldInfo.FieldType);
				}
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
				// not sure if I should just compare just the content or the whole Structure
				if (Length == s.Length)
					foreach (var pair in Pack().Zip(s.Pack(), (m, r) => new { m, r }))
						if (pair.m != pair.r)
							return false;
				return true;

				//var remoteFields = s.GetFields().ToArray();
				//var myFields = GetFields().ToArray();

				//if (remoteFields.Length == myFields.Length)
				//{
				//	var zipped = myFields.Zip(remoteFields, (m, r) => new { m, r });
				//	foreach (var pair in zipped)
				//	{
				//		if (pair.m.FieldInfo.FieldType != pair.r.FieldInfo.FieldType)
				//		{
				//			return false;
				//		}
				//		else if (pair.m.FieldInfo.FieldType.IsArray && pair.r.FieldInfo.FieldType.IsArray)
				//		{
				//			var mArray = (Array)pair.m.Value;
				//			var rArray = (Array)pair.r.Value;
				//			if (mArray.Length != rArray.Length)
				//			{
				//				return false;
				//			}
				//			else
				//			{
				//				for (int i = 0; i < mArray.Length; i++)
				//					if (!mArray.GetValue(i).Equals(rArray.GetValue(i)))
				//						return false;
				//			}
				//		}
				//		else if (!pair.m.Value.Equals(pair.r.Value))
				//		{
				//			return false;
				//		}
				//	}
				//	return true;
				//}
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return Instance.GetHashCode();
		}

		internal Structure(Type type, List<FieldCreationInfo> fieldInfos, object instance)
		{
			this.type = type;
			this.Instance = instance;
			EncapsulateFields(type, instance, fieldInfos.ToDictionary(f => f.FieldName));
			SetInitialValuesAndArrays(fieldInfos);
		}

		private void EncapsulateFields(Type type, object instance, Dictionary<string, FieldCreationInfo> dict)
		{
			foreach (var field in type.GetFields())
			{
				fields[field.Name] = new Field(field, instance, dict.ContainsKey(field.Name) ? dict[field.Name].PackedType : null);
			}
		}

		private void SetInitialValuesAndArrays(List<FieldCreationInfo> fieldInfos)
		{
			foreach (var fieldInfo in fieldInfos)
			{
				if (fieldInfo.PackedType.IsArray)
					this[fieldInfo.FieldName].Value = fieldInfo.PackedType.InitialValue;

				else if (fieldInfo.PackedType.InitialValue?.GetType() == fieldInfo.PackedType.FieldBaseType)
					this[fieldInfo.FieldName].Value = fieldInfo.PackedType.InitialValue;
			}
		}

		public int Length { get => Enumerable.Sum(from field in fields.Values select field.Length); }

		public byte[] Pack()
		{
			DataCollection collection = new DataCollection();
			foreach (var field in fields.Values)
			{
				try
				{
					collection.Append(field.Pack());
				}
				catch
				{
					throw new Exception($"Can't pack data for {field.FieldInfo.Name} ");
				}
			}
			return collection.ToArray();
		}

		public IEnumerable<Field> GetFields()
		{
			foreach (var field in fields.Values)
				yield return field;
		}

		public Field this[string fieldName]
		{
			get => fields.ContainsKey(fieldName ?? "") ? fields[fieldName] : null;
		}
	}
}