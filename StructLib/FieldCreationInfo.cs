using System.Reflection;

namespace StructLib
{
	public class FieldCreationInfo
	{
		internal FieldCreationInfo(string fieldName, IPackedType packedType, int offset)
		{
			this.FieldName = fieldName;
			this.PackedType = packedType;
			this.Offset = offset;
		}

		public object InitialValue { get => PackedType.InitialValue; set => PackedType.InitialValue = value; }
		public FieldInfo Field { get; internal set; }
		public string FieldName { get; }
		internal IPackedType PackedType { get; }
		public int Offset { get; }
	}
}
