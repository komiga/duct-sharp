
using System;
using System.Text;
using System.Collections.Generic;

namespace duct {

/**
	Base Variable types.
	0x1 through 0x80 are reserved types.
*/
[Flags]
public enum VariableType : uint {
	/**
		#IntVariable.
	*/
	INTEGER=0x1,
	/**
		#StringVariable.
	*/
	STRING=0x2,
	/**
		#FloatVariable.
	*/
	FLOAT=0x4,
	/**
		#BoolVariable.
	*/
	BOOL=0x8,
	/**
		Reserved type 0x10.
	*/
	_RESERVED0=0x10,
	/**
		Reserved type 0x20.
	*/
	_RESERVED1=0x20,
	/**
		#Identifier.
	*/
	IDENTIFIER=0x40,
	/**
		#Node.
	*/
	NODE=0x80,
	/**
		No variable type.
		Special variable type (means 'no variable type'). Alias for 0x0 (no flags).
		@see ANY.
	*/
	NONE=0x0,
	/**
		Special value for variable searching.
		Means "any variable type".
	*/
	ANY=0xFFFFFFFF,
	/**
		Special type for ValueVariables.
	*/
	VALUE=INTEGER|STRING|FLOAT|BOOL,
	/**
		Special type for CollectionVariables.
	*/
	COLLECTION=IDENTIFIER|NODE
};

/**
	Variable value/name format.
	Formatting flags for Variable names and ValueVariable values.
*/
[Flags]
public enum ValueFormat {
	/**
		Value quote-always format flag.
		This flag is for any variable type. The output will always have quotes around it.
	*/
	VALUE_QUOTE_ALWAYS=0x01,
	
	/**
		String quote-whitespace format flag.
		This format will quote a string containing whitespace or linefeed/carriage-return characters. e.g. "foo bar\\t" -> "\\"foo bar\\t\\"".
	*/
	STRING_QUOTE_WHITESPACE=0x10,
	/**
		String quote-empty format flag.
		This format will quote an empty string. e.g. "" -> "\\"\\"".
	*/
	STRING_QUOTE_EMPTY=0x20,
	/**
		String quote-control format flag.
		This format will quote a string containing the following characters: '{', '}', '='.
	*/
	STRING_QUOTE_CONTROL=0x40,
	/**
		String quote-always format flag.
		This format will always quote any string.
	*/
	STRING_QUOTE_ALWAYS=0x80,
	/**
		String quote-bool format flag.
		This format will quote a string if it equals "true" or "false" as a type safeguard. e.g. "true" -> "\\"true\\"".
	*/
	STRING_SAFE_BOOL=0x0100,
	/**
		String quote-number format flag.
		This format will quote a string if it is a number as a type safeguard. e.g. "1234.5678" -> "\\"1234.5678\\"".
	*/
	STRING_SAFE_NUMBER=0x0200,
	/**
		String escape-newline format flag.
		This format will replace the following characters with escape sequences, if the string is not surrounded in quotes: '\\n' '\\r'
	*/
	STRING_ESCAPE_NEWLINE=0x1000,
	/**
		String escape-control format flag.
		This format will replace the following characters with escape sequences, if the string is not surrounded in quotes: '{', '}', '='.
	*/
	STRING_ESCAPE_CONTROL=0x2000,
	/**
		String escape-other format flag.
		This format will replace the following characters with escape sequences: '\\t', '\\"', '\\'.
	*/
	STRING_ESCAPE_OTHER=0x4000,
	/**
		String escape-all format flag.
		Consists of #STRING_ESCAPE_NEWLINE, #STRING_ESCAPE_CONTROL and #STRING_ESCAPE_OTHER.
	*/
	STRING_ESCAPE_ALL=STRING_ESCAPE_NEWLINE|STRING_ESCAPE_CONTROL|STRING_ESCAPE_OTHER,
	/**
		String safe format flag.
		Consists of #STRING_SAFE_BOOL, #STRING_SAFE_NUMBER, #STRING_ESCAPE_OTHER and #STRING_QUOTE_CONTROL.
	*/
	STRING_SAFE=STRING_SAFE_BOOL|STRING_SAFE_NUMBER|STRING_ESCAPE_OTHER|STRING_QUOTE_CONTROL,
	/**
		Default string format flag.
		Consists of #STRING_SAFE, #STRING_QUOTE_WHITESPACE and #STRING_QUOTE_EMPTY.
	*/
	STRING_DEFAULT=STRING_SAFE|STRING_QUOTE_WHITESPACE|STRING_QUOTE_EMPTY,
	
	/**
		Boolean quote format flag.
		Converts the boolean value to a string ("true", "false"). e.g. false -> "false", true -> "true".
	*/
	BOOL_QUOTE=0x010000,
	/**
		Default boolean format flag.
		Unset flag (no formatting).
	*/
	BOOL_DEFAULT=NONE,

	/**
		Default name format flag.
		Consists of #STRING_SAFE, #STRING_QUOTE_WHITESPACE and #STRING_QUOTE_EMPTY.
	*/
	NAME_DEFAULT=STRING_SAFE|STRING_QUOTE_WHITESPACE|STRING_QUOTE_EMPTY,
	
	/**
		Default int format flag.
		Unset flag (no formatting).
	*/
	INTEGER_DEFAULT=NONE,

	/**
		Default float format flag.
		Unset flag (no formatting).
	*/
	FLOAT_DEFAULT=NONE,

	/**
		Default format flag for any variable.
		Consists of all default format flags: #STRING_DEFAULT, #FLOAT_DEFAULT, #BOOL_DEFAULT and #INTEGER_DEFAULT.
	*/
	ALL_DEFAULT=STRING_DEFAULT|FLOAT_DEFAULT|BOOL_DEFAULT|INTEGER_DEFAULT,
	
	/**
		No-format flag.
	*/
	NONE=0
};

/**
	Variable class.
*/
public abstract class Variable {
	protected string _name=String.Empty;
	public string Name {
		set { _name=value; }
		get { return _name; }
	}

	public string GetNameFormatted() {
		return GetNameFormatted(ValueFormat.NAME_DEFAULT);
	}

	public string GetNameFormatted(ValueFormat format) {
		string str;
		if ((format&ValueFormat.VALUE_QUOTE_ALWAYS)!=0)
			str=String.Format("\"{0}\"", _name);
		else if ((format&ValueFormat.STRING_QUOTE_EMPTY)!=0 && String.IsNullOrEmpty(_name))
			return "\"\"";
		else if ((format&ValueFormat.STRING_QUOTE_WHITESPACE)!=0 && (!String.IsNullOrEmpty(_name) && (_name.IndexOf('\t')>-1 || _name.IndexOf(' ')>-1 || _name.IndexOf('\n')>-1)))
			str=String.Format("\"{0}\"", _name);
		else if ((format&ValueFormat.STRING_QUOTE_CONTROL)!=0 && (_name.IndexOf(CHAR_OPENBRACE)>-1 || _name.IndexOf(CHAR_CLOSEBRACE)>-1 || _name.IndexOf(CHAR_EQUALSIGN)>-1))
			str=String.Format("\"{0}\"", _name);
		else
			str=_name;
		return EscapeString(str, format);
	}

	protected CollectionVariable _parent;
	public CollectionVariable Parent {
		set { _parent=value; }
		get { return _parent; }
	}

	public abstract VariableType Type {get;}
	public abstract string TypeName {get;}

	public abstract Variable Clone();

	public static int VariableToBool(Variable source) {
		if (source!=null) {
			if (source.Type==VariableType.BOOL) {
				return ((BoolVariable)source).Value ? 1 : 0;
			} else if (source.Type==VariableType.STRING) {
				string str=((StringVariable)source).Value;
				if (String.Compare(str, "true", true)==0 || str.CompareTo("1")==0) {
					return 1;
				} else if (String.Compare(str, "false", true)==0 || str.CompareTo("0")==0) {
					return 0;
				}
			} else if (source.Type==VariableType.INTEGER) {
				int val=((IntVariable)source).Value;
				if (val==1)
					return 1;
				else if (val==0)
					return 0;
			}
		}
		return -1;
	}

	public static int StringToBool(string source) {
		if (source!=String.Empty) {
			if (String.Compare(source, "true", true)==0 || source.CompareTo("1")==0) {
				return 1;
			} else if (String.Compare(source, "false", true)==0 || source.CompareTo("0")==0) {
				return 0;
			}
		}
		return -1;
	}

	public static ValueVariable StringToValue(string source, VariableType type) {
		return StringToValue(source, "", type);
	}

	public static ValueVariable StringToValue(string source) {
		return StringToValue(source, "");
	}

	public static ValueVariable StringToValue(string source, string varname) {
		return StringToValue(source, varname, VariableType.NONE);
	}

	public static ValueVariable StringToValue(string source, string varname, VariableType type) {
		if (String.IsNullOrEmpty(source))
			return new StringVariable(source, varname, null);
		if (type==VariableType.NONE) {
			for (int i=0; i<source.Length; ++i) {
				char c=source[i];
				if ((c>='0' && c<='9') || c=='+' || c=='-') {
					if (type==VariableType.NONE) { // Leave float and string alone
						type=VariableType.INTEGER; // Integer so far..
					}
				} else if (c=='.') {
					if (type==VariableType.INTEGER || type==VariableType.NONE) {
						type=VariableType.FLOAT;
					} else if (type==VariableType.FLOAT) {
						type=VariableType.STRING; // Float cannot have more than one decimal point, so the source must be a string
						break;
					}
				} else { // If the character is not numerical there is nothing else to deduce and the value is a string
					type=VariableType.STRING;
					break;
				}
			}
		}
		ValueVariable v;
		switch (type) {
			case VariableType.INTEGER:
				v=new IntVariable(0, varname);
				break;
			case VariableType.FLOAT:
				v=new FloatVariable(0.0f, varname, null);
				break;
			case VariableType.BOOL:
				v=new BoolVariable(false, varname, null);
				break;
			default: // NOTE: VariableType.STRING results the same as an unrecognized variable type
				int b=StringToBool(source);
				if (b>-1)
					return new BoolVariable(b==1, varname, null);
				else
					return new StringVariable(source, varname, null);
		}
		v.SetFromString(source);
		return v;
	}

	public const char CHAR_EOF='\xFFFF';
	public const char CHAR_NEWLINE='\n';
	public const char CHAR_CARRIAGERETURN='\r';
	public const char CHAR_TAB='\t';
	public const char CHAR_N='n';
	public const char CHAR_R='r';
	public const char CHAR_T='t';
	public const char CHAR_APOSTROPHE='\'';
	public const char CHAR_QUOTE='\"';
	public const char CHAR_BACKSLASH='\\';
	public const char CHAR_OPENBRACE='{';
	public const char CHAR_CLOSEBRACE='}';
	public const char CHAR_EQUALSIGN='=';

	public static char GetEscapeChar(char c) {
		switch (c) {
			case CHAR_N:
				return CHAR_NEWLINE;
			case CHAR_R:
				return CHAR_CARRIAGERETURN;
			case CHAR_T:
				return CHAR_TAB;
			case CHAR_APOSTROPHE:
			case CHAR_QUOTE:
			case CHAR_BACKSLASH:
			case CHAR_OPENBRACE:
			case CHAR_CLOSEBRACE:
			case CHAR_EQUALSIGN:
				return c;
			default:
				return CHAR_EOF;
		}
	}

	public static string EscapeString(string str) {
		return EscapeString(str, ValueFormat.STRING_ESCAPE_OTHER);
	}

	public static string EscapeString(string str, ValueFormat format) {
		if (String.IsNullOrEmpty(str)
			|| ((format&ValueFormat.STRING_ESCAPE_OTHER)==0
			&& (format&ValueFormat.STRING_ESCAPE_CONTROL)==0
			&& (format&ValueFormat.STRING_ESCAPE_NEWLINE)==0)) {
			return str;
		}
		bool isquoted=(str.Length>=2 && str[0]=='\"' && str[str.Length-1]=='\"' && !str.EndsWith("\\\""));
		StringBuilder builder=new StringBuilder(str.Length+32);
		for (int i=0; i<str.Length; ++i) {
			char c=str[i];
			if ((format&ValueFormat.STRING_ESCAPE_OTHER)!=0) {
				switch (c) {
				case CHAR_TAB:
					if (!isquoted)
						builder.Append("\\t");
					else
						builder.Append(c);
					continue;
				case CHAR_QUOTE:
					if (/*!isquoted && */(i>0 && i<(str.Length-1)))
						builder.Append("\\\"");
					else
						builder.Append(CHAR_QUOTE);
					continue;
				case CHAR_BACKSLASH:
					if ((i+1)!=str.Length) {
						char c2=GetEscapeChar(str[i+1]);
						switch (c2) {
						case CHAR_BACKSLASH:
							i++; // we don't want to see the slash again; the continue below makes the i-uppage 2
							goto case CHAR_EOF;
						case CHAR_EOF:
							builder.Append("\\\\");
							break;
						default:
							builder.Append('\\'+c2);
							i++; // already a valid escape sequence
							break;
						}
					} else {
						builder.Append("\\\\");
					}
					continue;
				}
			}
			if ((format&ValueFormat.STRING_ESCAPE_CONTROL)!=0 && !isquoted) {
				switch (c) {
				case CHAR_OPENBRACE:
					builder.Append("\\{");
					continue;
				case CHAR_CLOSEBRACE:
					builder.Append("\\}");
					continue;
				case CHAR_EQUALSIGN:
					builder.Append("\\=");
					continue;
				}
			}
			if ((format&ValueFormat.STRING_ESCAPE_NEWLINE)!=0 && !isquoted) {
				switch (c) {
				case CHAR_NEWLINE:
					builder.Append("\\n");
					continue;
				case CHAR_CARRIAGERETURN:
					builder.Append("\\r");
					continue;
				}
			}
			builder.Append(c);
		}
		return builder.ToString();
	}
}

public abstract class ValueVariable : Variable {
	public abstract void SetFromString(string source);
	public virtual string GetValueFormatted() {
		return GetValueFormatted(ValueFormat.ALL_DEFAULT);
	}
	public abstract string GetValueFormatted(ValueFormat format);
	public abstract string ValueAsString();
}

public abstract class CollectionVariable : Variable {
	protected List<Variable> _children=new List<Variable>();
	public List<Variable> Children {
		get { return _children; }
	}

	public int Count {
		get { return _children.Count; }
	}

	public List<Variable>.Enumerator GetEnumerator() {
		return _children.GetEnumerator();
	}

	public void Clear() {
		_children.Clear();
	}

	public bool Add(Variable variable) {
		if (variable!=null) {
			_children.Add(variable);
			variable.Parent=this;
			return true;
		}
		return false;
	}

	public bool Insert(int index, Variable variable) {
		try {
			_children.Insert(index, variable);
			variable.Parent=this;
			return true;
		} catch (ArgumentOutOfRangeException) {
		}
		return false;
	}

	public bool InsertAfter(Variable variable, Variable after) {
		int index=_children.IndexOf(after);
		if (index!=-1) {
			return Insert(index, variable);
		}
		return false;
	}

	public bool Remove(int i) {
		try {
			Variable variable=_children[i];
			return Remove(variable);
		} catch (ArgumentOutOfRangeException) {
			return false;
		}
	}

	public bool Remove(Variable variable) {
		if (_children.Remove(variable)) {
			variable.Parent=null;
			return true;
		}
		return false;
	}

	public bool Remove(VariableType type) {
		foreach (Variable v in _children) {
			if ((v.Type&type)==v.Type) {
				return Remove(v);
			}
		}
		return false;
	}

	public bool Remove(string name) {
		return Remove(name, true);
	}

	public bool Remove(string name, bool casesens) {
		return Remove(name, casesens, VariableType.ANY);
	}

	public bool Remove(string name, bool casesens, VariableType type) {
		foreach (Variable v in _children) {
			if (String.Compare(v.Name, name, !casesens)==0 && (type&v.Type)==v.Type) {
				return Remove(v);
			}
		}
		return false;
	}

	public Variable Get(string name) {
		return Get(name, true);
	}

	public Variable Get(string name, bool casesens) {
		return Get(name, casesens, VariableType.ANY);
	}

	public Variable Get(string name, bool casesens, VariableType type) {
		foreach (Variable v in _children) {
			if (String.Compare(v.Name, name, !casesens)==0 && (type&v.Type)==v.Type)
				return v;
		}
		return null;
	}

	public Variable Get(int index) {
		if (index>-1 && index<_children.Count) {
			try {
				return _children[index];
			} catch {
				return null;
			}
		}
		return null;
	}

	public IntVariable GetInt(string name) {
		return GetInt(name, true);
	}

	public IntVariable GetInt(string name, bool casesens) {
		Variable v=Get(name, casesens, VariableType.INTEGER);
		return (IntVariable)v;
	}

	public IntVariable GetInt(int index) {
		Variable variable=Get(index);
		if (variable is IntVariable)
			return (IntVariable)variable;
		return null;
	}

	public StringVariable GetString(string name) {
		return GetString(name, true);
	}

	public StringVariable GetString(string name, bool casesens) {
		Variable v=Get(name, casesens, VariableType.STRING);
		return (StringVariable)v;
	}

	public StringVariable GetString(int index) {
		Variable variable=Get(index);
		if (variable is StringVariable)
			return (StringVariable)variable;
		return null;
	}

	public FloatVariable GetFloat(string name) {
		return GetFloat(name, true);
	}

	public FloatVariable GetFloat(string name, bool casesens) {
		Variable v=Get(name, casesens, VariableType.FLOAT);
		return (FloatVariable)v;
	}

	public FloatVariable GetFloat(int index) {
		Variable variable=Get(index);
		if (variable is FloatVariable)
			return (FloatVariable)variable;
		return null;
	}

	public BoolVariable GetBool(string name) {
		return GetBool(name, true);
	}

	public BoolVariable GetBool(string name, bool casesens) {
		Variable v=Get(name, casesens, VariableType.BOOL);
		return (BoolVariable)v;
	}

	public BoolVariable GetBool(int index) {
		Variable variable=Get(index);
		if (variable is BoolVariable)
			return (BoolVariable)variable;
		return null;
	}

	public string GetAsString(int index) {
		Variable variable=Get(index);
		if (variable is ValueVariable)
			return ((ValueVariable)variable).ValueAsString();
		return null; // TODO: return null?
	}

	public Identifier GetIdentifier(string name) {
		return GetIdentifier(name, true);
	}

	public Identifier GetIdentifier(string name, bool casesens) {
		Variable v=Get(name, casesens, VariableType.IDENTIFIER);
		return (Identifier)v;
	}

	public Identifier GetIdentifier(int index) {
		Variable variable=Get(index);
		if (variable is Identifier)
			return (Identifier)variable;
		return null;
	}

	public Node GetNode(string name) {
		return GetNode(name, true);
	}

	public Node GetNode(string name, bool casesens) {
		Variable v=Get(name, casesens, VariableType.NODE);
		return (Node)v;
	}

	public Node GetNode(int index) {
		Variable variable=Get(index);
		if (variable is Node)
			return (Node)variable;
		return null;
	}
}

public class IntVariable : ValueVariable {
	int _value;
	public int Value {
		set { _value=value; }
		get { return _value; }
	}

	public IntVariable() : this(0) {
	}

	public IntVariable(int val) : this(val, String.Empty) {
	}

	public IntVariable(int val, string name) : this(val, name, null) {
	}

	public IntVariable(int val, string name, CollectionVariable parent) {
		_value=val;
		_name=name;
		_parent=parent;
	}

	public IntVariable(int val, CollectionVariable parent) : this(val, String.Empty, parent) {
	}

	public override void SetFromString(string source) {
		if (!Int32.TryParse(source, out _value))
			_value=0;
	}

	public override string GetValueFormatted(ValueFormat format) {
		if ((format&ValueFormat.VALUE_QUOTE_ALWAYS)!=0) {
			return String.Format("\"{0}\"", _value);
		}
		return _value.ToString();
	}

	public override string ValueAsString() {
		return _value.ToString();
	}

	public override VariableType Type {
		get { return VariableType.INTEGER; }
	}

	public override string TypeName {
		get { return "int"; }
	}

	public override Variable Clone() {
		return new IntVariable(_value, _name);
	}
}

public class StringVariable : ValueVariable {
	string _value;
	public string Value {
		set { _value=value; }
		get { return _value; }
	}

	public StringVariable() : this(String.Empty) {
	}

	public StringVariable(string val) : this(val, String.Empty) {
	}

	public StringVariable(string val, string name) : this(val, name, null) {
	}

	public StringVariable(string val, string name, CollectionVariable parent) {
		_value=val;
		_name=name;
		_parent=parent;
	}

	public StringVariable(string val, CollectionVariable parent) : this(val, String.Empty, parent) {
	}

	public bool isNumeric() {
		return isNumeric(true);
	}

	public bool isNumeric(bool allowdecimal) {
		if (allowdecimal) {
			double tmp;
			return Double.TryParse(_value, out tmp);
		} else {
			long tmp;
			return Int64.TryParse(_value, out tmp);
		}
	}

	public override void SetFromString(string source) {
		_value=source;
	}

	public override string GetValueFormatted(ValueFormat format) {
		string str;
		if ((format&ValueFormat.VALUE_QUOTE_ALWAYS)!=0 || (format&ValueFormat.STRING_QUOTE_ALWAYS)!=0)
			str=String.Format("\"{0}\"", _value);
		else if ((format&ValueFormat.STRING_QUOTE_EMPTY)!=0 && String.IsNullOrEmpty(_value))
			return "\"\"";
		else if ((format&ValueFormat.STRING_QUOTE_WHITESPACE)!=0 && (!String.IsNullOrEmpty(_value) && (_value.IndexOf('\t')>-1 || _value.IndexOf(' ')>-1 || _value.IndexOf('\n')>-1)))
			str=String.Format("\"{0}\"", _value);
		else if ((format&ValueFormat.STRING_SAFE_BOOL)!=0 && Variable.VariableToBool(this)!=-1)
			str=String.Format("\"{0}\"", _value);
		else if ((format&ValueFormat.STRING_SAFE_NUMBER)!=0 && isNumeric(true))
			str=String.Format("\"{0}\"", _value);
		else if ((format&ValueFormat.STRING_QUOTE_CONTROL)!=0 && (_value.IndexOf(CHAR_OPENBRACE)>-1 || _value.IndexOf(CHAR_CLOSEBRACE)>-1 || _value.IndexOf(CHAR_EQUALSIGN)>-1))
			str=String.Format("\"{0}\"", _value);
		else
			str=_value;
		return Variable.EscapeString(str, format);
	}

	public override string ValueAsString() {
		return _value;
	}

	public override VariableType Type {
		get { return VariableType.STRING; }
	}

	public override string TypeName {
		get { return "string"; }
	}

	public override Variable Clone() {
		return new StringVariable(_value, _name);
	}
}

public class FloatVariable : ValueVariable {
	float _value;
	public float Value {
		set { _value=value; }
		get { return _value; }
	}

	public FloatVariable() : this(0.0f) {
	}

	public FloatVariable(float val) : this(val, String.Empty) {
	}

	public FloatVariable(float val, string name) : this(val, name, null) {
	}

	public FloatVariable(float val, string name, CollectionVariable parent) {
		_value=val;
		_name=name;
		_parent=parent;
	}

	public FloatVariable(float val, CollectionVariable parent) : this(val, String.Empty, parent) {
	}

	public override void SetFromString(string source) {
		if (!Single.TryParse(source, out _value))
			_value=0.0f;
	}

	public override string GetValueFormatted(ValueFormat format) {
		if ((format&ValueFormat.VALUE_QUOTE_ALWAYS)!=0)
			return String.Format("\"{0:0.0###}\"", _value);
		return _value.ToString("0.0###");
	}

	public override string ValueAsString() {
		return _value.ToString("0.0###");
	}

	public override VariableType Type {
		get { return VariableType.FLOAT; }
	}

	public override string TypeName {
		get { return "float"; }
	}

	public override Variable Clone() {
		return new FloatVariable(_value, _name);
	}
}

public class BoolVariable : ValueVariable {
	bool _value;
	public bool Value {
		set { _value=value; }
		get { return _value; }
	}

	public BoolVariable() : this(false) {
	}

	public BoolVariable(bool val) : this(val, String.Empty) {
	}

	public BoolVariable(bool val, string name) : this(val, name, null) {
	}

	public BoolVariable(bool val, string name, CollectionVariable parent) {
		_value=val;
		_name=name;
		_parent=parent;
	}

	public BoolVariable(bool val, CollectionVariable parent) : this(val, String.Empty, parent) {
	}

	public override void SetFromString(string source) {
		if (String.Compare(source, "true", true)==0 || source.CompareTo("1")==0)
			_value=true;
		else
			_value=false;
	}

	public override string GetValueFormatted(ValueFormat format) {
		if ((format&ValueFormat.VALUE_QUOTE_ALWAYS)!=0 || (format&ValueFormat.BOOL_QUOTE)!=0)
			return _value ? "\"true\"" : "\"false\"";
		return _value ? "true" : "false";
	}

	public override string ValueAsString() {
		return _value ? "true" : "false";
	}

	public override VariableType Type {
		get { return VariableType.BOOL; }
	}

	public override string TypeName {
		get { return "bool"; }
	}

	public override Variable Clone() {
		return new BoolVariable(_value, _name);
	}
}

public class Identifier : CollectionVariable {
	public Identifier() : this("") {
	}

	public Identifier(string name) : this(name, null) {
	}

	public Identifier(string name, CollectionVariable parent) {
		_name=name;
		_parent=parent;
	}

	public Identifier(CollectionVariable parent) : this("", parent) {
	}

	public override VariableType Type {
		get { return VariableType.IDENTIFIER; }
	}

	public override string TypeName {
		get { return "identifier"; }
	}

	public override Variable Clone() {
		Identifier clone=new Identifier(_name);
		foreach (Variable v in _children)
			clone.Add(v.Clone());
		return clone;
	}
}

public class Node : CollectionVariable {
	public Node() : this(String.Empty) {
	}

	public Node(string name) : this(name, null) {
	}

	public Node(string name, CollectionVariable parent) {
		_name=name;
		_parent=parent;
	}

	public Node(CollectionVariable parent) : this(String.Empty, parent) {
	}

	public override VariableType Type {
		get { return VariableType.NODE; }
	}

	public override string TypeName {
		get { return "node"; }
	}

	public override Variable Clone() {
		Node clone=new Node(_name);
		foreach (Variable v in _children)
			clone.Add(v.Clone());
		return clone;
	}
}

} // namespace duct
