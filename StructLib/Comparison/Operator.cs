using System;

namespace StructLib.Comparison
{
	public interface IOperator
	{
		string Title { get; }
		double FuzzyRatio { get; set; }
		bool? Compare(object operand1, object operand2);
	}

	public abstract class AbstractComparator : IOperator
	{
		public virtual string Title => "==";

		public virtual double FuzzyRatio { get; set; }

		public abstract bool? Compare(object operand1, object operand2);

		protected enum CompareResult
		{
			Unknown,
			Equal,
			Different,
			Smaller,
			Bigger,
		}

		protected virtual CompareResult CompareEx(object operand1, object operand2, double fuzzyRatio = 0)
		{
			if (operand1.GetType().IsArray && operand1.GetType().IsArray)
			{
				var ar1 = (Array)operand1;
				var ar2 = (Array)operand2;

				if (ar1.Length == ar2.Length)
				{
					for (int i = 0; i < ar1.Length; i++)
						if (ar1.GetValue(i) != ar2.GetValue(i))
							return CompareResult.Different;
					return CompareResult.Equal;
				}
				return CompareResult.Different;
			}

			Type T1 = operand1.GetType();
			Type T2 = operand1.GetType();

			if (T1 == T2)
			{
				if (T1 == typeof(bool))
					return ((bool)operand1) == ((bool)operand2) ? CompareResult.Equal : CompareResult.Different;

				if (T1 == typeof(string))
					return ((string)operand2 == (string)operand2) ? CompareResult.Equal : CompareResult.Different;

				if (T1 == typeof(int))
					return CompareEx((int)operand1, (int)operand2);

				if (T1 == typeof(uint))
					return CompareEx((uint)operand1, (uint)operand2);

				if (T1 == typeof(short))
					return CompareEx((short)operand1, (short)operand2);

				if (T1 == typeof(ushort))
					return CompareEx((ushort)operand1, (ushort)operand2);

				if (T1 == typeof(byte))
					return CompareEx((byte)operand1, (byte)operand2);

				if (T1 == typeof(Int64))
					return CompareEx((Int64)operand1, (Int64)operand2);

				if (T1 == typeof(UInt64))
					return CompareEx((UInt64)operand1, (UInt64)operand2);

				if (T1 == typeof(char))
					return CompareEx((char)operand1, (char)operand2);

				if (T1 == typeof(float))
					return CompareExDouble((float)operand1, (float)operand2, fuzzyRatio);

				if (T1 == typeof(double))
					return CompareExDouble((double)operand1, (double)operand2, fuzzyRatio);

			}
			return CompareResult.Different;
		}

		protected CompareResult CompareEx(Int64 operand1, Int64 operand2)
		{
			if (operand1 == operand2)
				return CompareResult.Equal;

			if (operand1 > operand2)
				return CompareResult.Bigger;

			return CompareResult.Smaller;
		}

		protected CompareResult CompareExDouble(double operand1, double operand2, double fuzzyRatio)
		{
			if (fuzzyRatio >= 1.0)
				fuzzyRatio = 0.5;

			double leak = operand1 * fuzzyRatio;
			if (leak < 0)
				leak = -1.0 * leak;

			if (((operand1 > operand2) ? operand1 - operand1 : operand2 - operand1) <= leak)
				return CompareResult.Equal;

			if (operand1 > operand2)
				return CompareResult.Bigger;

			return CompareResult.Smaller;
		}
	}

	public class OperatorEqual : AbstractComparator
	{
		private static OperatorEqual _instance;
		private static object @lock = new object();
		public static OperatorEqual Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (@lock)
					{
						if (_instance == null)
							_instance = new OperatorEqual();
					}
				}
				return _instance;
			}
		}

		internal OperatorEqual()
		{

		}

		public override double FuzzyRatio { get; set; }

		public override bool? Compare(object operand1, object operand2)
		{
			switch (CompareEx(operand1, operand2))
			{
				case CompareResult.Unknown:
					return null;
				case CompareResult.Equal:
					return true;
				default:
					return false;
			}
		}
	}

	public class OperatorDifferent : OperatorEqual
	{
		private static OperatorDifferent _instance;
		private static object @lock = new object();
		public new static OperatorDifferent Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (@lock)
					{
						if (_instance == null)
							_instance = new OperatorDifferent();
					}
				}
				return _instance;
			}
		}

		internal OperatorDifferent()
		{

		}

		public override string Title => "!=";
		public override bool? Compare(object operand1, object operand2) => !base.Compare(operand1, operand2);
	}

	public class OperatorBigger : OperatorEqual
	{
		private static OperatorBigger _instance;
		private static object @lock = new object();
		public new static OperatorBigger Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (@lock)
					{
						if (_instance == null)
							_instance = new OperatorBigger();
					}
				}
				return _instance;
			}
		}

		internal OperatorBigger()
		{

		}

		public override string Title => ">";

		public override bool? Compare(object operand1, object operand2)
		{
			switch (CompareEx(operand1, operand2))
			{
				case CompareResult.Bigger:
					return true;
				case CompareResult.Unknown:
					return null;
				default:
					return false;
			}
		}
	}

	public class OperatorSmaller : OperatorBigger
	{
		private static OperatorSmaller _instance;
		private static object @lock = new object();
		public new static OperatorSmaller Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (@lock)
					{
						if (_instance == null)
							_instance = new OperatorSmaller();
					}
				}
				return _instance;
			}
		}

		internal OperatorSmaller()
		{

		}

		public override string Title => "<";

		public override bool? Compare(object operand1, object operand2)
		{
			switch (CompareEx(operand1, operand2))
			{
				case CompareResult.Smaller:
					return true;
				case CompareResult.Unknown:
					return null;
				default:
					return false;
			}
		}
	}

	public class OperatorSmallerOrEqual : OperatorBigger
	{
		private static OperatorSmallerOrEqual _instance;
		private static object @lock = new object();
		public new static OperatorSmallerOrEqual Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (@lock)
					{
						if (_instance == null)
							_instance = new OperatorSmallerOrEqual();
					}
				}
				return _instance;
			}
		}

		internal OperatorSmallerOrEqual()
		{

		}

		public override string Title => "<=";

		public override bool? Compare(object operand1, object operand2)
		{
			return !base.Compare(operand1, operand2);
		}
	}

	public class OperatorBiggerOrEqual : OperatorSmaller
	{
		private static OperatorBiggerOrEqual _instance;
		private static object @lock = new object();
		public new static OperatorBiggerOrEqual Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (@lock)
					{
						if (_instance == null)
							_instance = new OperatorBiggerOrEqual();
					}
				}
				return _instance;
			}
		}

		internal OperatorBiggerOrEqual()
		{

		}

		public override string Title => ">=";

		public override bool? Compare(object operand1, object operand2)
		{
			return !base.Compare(operand1, operand2);
		}
	}
}
