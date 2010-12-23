using System;
using duct;

class MainClass {
	public static void Main(string[] args) {
		Template tpl_test01 = new Template(new string[]{"test01", "testalt01"}, new VariableType[]{VariableType.INTEGER, VariableType.STRING, VariableType.FLOAT});
		Template tpl_test02 = new Template(new string[]{"Test02", "TestAlt02"}, new VariableType[]{VariableType.INTEGER | VariableType.STRING | VariableType.FLOAT}, true);
		Template tpl_test03 = new Template(new string[]{"test03"}, new VariableType[]{VariableType.STRING}, false, VariableType.ANY);
		Template tpl_test04 = new Template(new string[]{"test04"}, new VariableType[]{VariableType.STRING, VariableType.INTEGER}, false, VariableType.INTEGER);
		Template tpl_test05 = new Template(new string[]{"test05"}, new VariableType[]{VariableType.BOOL}, false, VariableType.INTEGER | VariableType.STRING | VariableType.FLOAT);
		Template tpl_test06 = new Template(new string[]{"test06"}, new VariableType[]{VariableType.FLOAT}, false, VariableType.INTEGER | VariableType.STRING | VariableType.FLOAT);
		Template tpl_test07 = new Template(null, new VariableType[]{VariableType.INTEGER, VariableType.STRING});
		Template tpl_bools = new Template(new string[]{"bools"}, new VariableType[]{VariableType.BOOL, VariableType.BOOL, VariableType.BOOL, VariableType.BOOL});

		try {
			Node root = ScriptFormatter.LoadFromFile("templates.script");
			if (root != null) {
				string temp;
				bool pass = false;
				foreach (Identifier identifier in root) {
					temp = identifier.Name.ToLower();
					if (temp == "test01" || temp == "testalt01") // Select the identifiers to test by-name. This isn't necessary in practice, but we're just going to do this to illustrate validation
						pass = tpl_test01.ValidateIdentifier(identifier);
					else if (temp == "test02" || temp == "testalt02")
						pass = tpl_test02.ValidateIdentifier(identifier);
					else if (temp == "test03")
						pass = tpl_test03.ValidateIdentifier(identifier);
					else if (temp == "test04")
						pass = tpl_test04.ValidateIdentifier(identifier);
					else if (temp == "test05")
						pass = tpl_test05.ValidateIdentifier(identifier);
					else if (temp == "test06")
						pass = tpl_test06.ValidateIdentifier(identifier);
					else if (temp == "bools")
						pass = tpl_bools.ValidateIdentifier(identifier);
					else
						pass = tpl_test07.ValidateIdentifier(identifier);
					ScriptFormatter.FormatIdentifier(identifier, out temp);
					Console.WriteLine("Identifier: {0} {{{1}}}", (pass) ? "passed" : "failed", temp);
				}
			} else {
				Console.WriteLine("Root node is null");
			}
		} catch (ScriptParserException e) {
			Console.WriteLine("Caught exception: {0}", e.ToString());
		}
	}
}
