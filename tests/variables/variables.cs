using System;
using duct;

class MainClass {
	public static void PrintValues(Identifier iden) {
		Console.WriteLine("Variable count: {0}", iden.Count);
		foreach (ValueVariable v in iden.Children) {
			Console.WriteLine("{0}, {1}", v.GetValueFormatted(ValueFormat.ALL_DEFAULT), v.GetNameFormatted(ValueFormat.NAME_DEFAULT));
		}
	}

	public static void Main(string[] args) {
		Identifier iden = new Identifier();
		iden.Add(new IntVariable(100, "integer"));
		iden.Add(new FloatVariable(100, "float"));
		iden.Add(new StringVariable("borkbork", "string"));
		iden.Add(new BoolVariable(true, "bool"));
		PrintValues(iden);
		Console.WriteLine("removing VariableType.BOOL: {0}", iden.Remove(VariableType.BOOL));
		Console.WriteLine("removing \"string\": {0}\n", iden.Remove("string"));
		PrintValues(iden);
	}
}
