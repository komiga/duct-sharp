using System;
using duct;

class MainClass {
	static void OutputNode(Node root, ValueFormat nameformat, ValueFormat varformat, int tcount) {
		string temp;
		foreach (Variable variable in root.Children) {
			if (variable is Node) {
				Node n = (Node)variable;
				Console.WriteLine("{0}Node: \"{1}\"", new String('\t', tcount), n.Name);
				OutputNode(n, nameformat, varformat, tcount + 1);
			} else if (variable is Identifier) {
				ScriptFormatter.FormatIdentifier((Identifier)variable, out temp, nameformat/*ValueFormat.NONE*/, varformat);
				temp = temp.PadLeft(temp.Length + tcount, '\t');
				Console.WriteLine(temp);
			} else if (variable is ValueVariable) {
				ScriptFormatter.FormatValue((ValueVariable)variable, out temp, nameformat, varformat);
				temp = temp.PadLeft(temp.Length + tcount, '\t');
				Console.WriteLine(temp);
			}
		}
	}

	public static void Main(string[] args) {
		//try {
			Node root = ScriptFormatter.LoadFromFile("in.script");
			if (root != null) {
				OutputNode(root, ValueFormat.NAME_DEFAULT/* & ~ValueFormat.STRING_ESCAPE_ALL*/, ValueFormat.ALL_DEFAULT/* & ~ValueFormat.STRING_ESCAPE_ALL*/, 0);
				if (ScriptFormatter.WriteToFile(root, "out.script", ValueFormat.NAME_DEFAULT, ValueFormat.ALL_DEFAULT))
					Console.WriteLine("Wrote out.script");
				else
					Console.WriteLine("Failed to write out.script");
			} else {
				Console.WriteLine("Root node is null");
			}
		//} catch (Exception e) {
		//	Console.WriteLine("Caught exception: {0}", e.ToString());
		//}
	}
}
