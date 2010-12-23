using System;
using System.Collections.Generic;
using duct;

class MainClass {
	public static void Main(string[] args) {
		string file = "in.csv";
		CSVMap map = CSVFormatter.LoadFromFile(file, ',', true);
		if (map != null) {
			Console.WriteLine("There are: {0} possible column(s), {1} row(s), and {2} value(s)", map.HeaderCount, map.RowCount, map.ValueCount);
			foreach (CSVRow row in map.Rows) {
				foreach (KeyValuePair<int, ValueVariable> pair in row) {
					Console.Write("({0}, {1})\t", row.Index, pair.Key);
					Console.WriteLine("{0}: `{1}`", pair.Value.TypeName, pair.Value.ValueAsString());
				}
			}
			Console.WriteLine();
			CSVFormatter.WriteToFile(map, "out.csv", ',', ValueFormat.VALUE_QUOTE_ALWAYS);
		} else {
			Console.WriteLine("Failed to open file: \"{0}\"", file);
		}
	}
}
