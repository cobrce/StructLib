using System.IO;
using System.Reflection;

namespace StructLib.Internal
{
	public class Field
	{

		private readonly object instance;
		private readonly IPackedType packedType;
		private FieldInfo field;
		public FieldInfo FieldInfo { get => field; }

		internal Field(FieldInfo field, object instance, IPackedType packedType)
		{
			this.field = field;
			this.instance = instance;
			this.packedType = packedType;
		}

		public DataCollection Pack()
		{
			return packedType.Pack(Value);
		}

		internal void Unpack(BinaryReader reader)
		{
			packedType.UnpackToField(instance,field,reader);
		}

		public object Value
		{
			get => field.GetValue(instance);
			set =>packedType.SetFieldValue(instance,field,value);
		}
		public int Length { get => packedType.FieldLength(this); }
	}
}
