using System.Collections.Generic;
using System.IO;

namespace StructLib.Internal
{
	public class DataCollection
	{
		List<byte[]> data = new List<byte[]>();

		internal void Add(byte[] data)
		{
			this.data.Add(data);
		}

		internal void Append(DataCollection dataCollection)
		{
			data.AddRange(dataCollection.data);
		}

		public byte[] ToArray()
		{
			using (var ms = new MemoryStream())
			{
				foreach (var chunk in data)
				{
					ms.Write(chunk, 0, chunk.Length);
				}
				return ms.ToArray();
			}
		}

		internal DataCollection()
		{

		}
	}
}
