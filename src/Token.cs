
using System;
using System.Text;
using duct;

namespace duct {

public class Token<_TokenType> {
	StringBuilder _builder = new StringBuilder();

	_TokenType _type;
	public _TokenType Type {
		set { _type = value; }
		get { return _type; }
	}

	public Token() {
	}

	public Token(_TokenType type) {
		_type = type;
	}

	int _line;
	public int Line {
		set { _line = value; }
		get { return _line; }
	}

	int _col;
	public int Column {
		set { _col = value; }
		get { return _col; }
	}

	public void SetPosition(int line, int col) {
		_line = line;
		_col = col;
	}

	public void Add(char c) {
		if (_builder.Length == 0)
			_builder.Capacity = 68;
		else if (_builder.Length == _builder.Capacity)
			_builder.Capacity = _builder.Capacity + 68;
		_builder.Append(c);
	}

	public void Reset(_TokenType type) {
		_type = type;
		_builder.Remove(0, _builder.Length);
		//Console.WriteLine("DEBUG: Token.Clear() _builder.Capacity == {0}", _builder.Capacity);
	}

	public bool Compare(char c) {
		for (int i = 0; i < _builder.Length; ++i) {
			if (_builder[i] != c)
				return false;
		}
		return true;
	}

	public bool Compare(CharacterSet charset) {
		for (int i = 0; i < _builder.Length; ++i) {
			if (!charset.Contains(_builder[i]))
				return false;
		}
		return true;
	}

	public override string ToString() {
		return _builder.ToString();
	}

	public int ToInt() {
		string str = _builder.ToString();
		return Convert.ToInt32(str);
	}

	public long ToLong() {
		string str = _builder.ToString();
		return Convert.ToInt64(str);
	}

	public float ToFloat() {
		string str = _builder.ToString();
		return Convert.ToSingle(str);
	}

	public double ToDouble() {
		string str = _builder.ToString();
		return Convert.ToDouble(str);
	}
}

} // namespace duct
