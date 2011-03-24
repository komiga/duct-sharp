
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace duct {

public class CSVMap {
	SortedDictionary<int, CSVRow> _rows;

	public int RowCount {
		get { return _rows.Count; }
	}

	public int HeaderCount {
		get {
			CSVRow header=GetRow(-1, false);
			if (header!=null)
				return header.Count;
			return 0;
		}
	}

	public int ValueCount {
		get {
			int count=0;
			foreach (CSVRow row in _rows.Values)
				count+=row.Count;
			return count;
		}
	}

	public SortedDictionary<int, CSVRow>.ValueCollection Rows {
		get { return _rows.Values; }
	}

	public SortedDictionary<int, CSVRow>.KeyCollection RowIndices {
		get { return _rows.Keys; }
	}

	public CSVMap() {
		_rows=new SortedDictionary<int, CSVRow>();
	}

	public bool AddRow(CSVRow row) {
		if (row!=null) {
			_rows[row.Index]=row;
			return true;
		}
		return false;
	}

	public bool RemoveRow(int index) {
		return _rows.Remove(index);
	}

	public bool RemoveRow(CSVRow row) {
		return _rows.Remove(row.Index);
	}

	public CSVRow GetRow(int index) {
		return GetRow(index, false);
	}

	public CSVRow GetRow(int index, bool autocreate) {
		CSVRow row=null;
		if (!_rows.TryGetValue(index, out row) && autocreate)
			row=new CSVRow(index);
		return row;
	}

	public ValueVariable GetValue(int row, int column) {
		CSVRow crow=GetRow(row);
		if (crow!=null)
			return crow.Get(column);
		return null;
	}

	public void Clear() {
		_rows.Clear();
	}

	public SortedDictionary<int, CSVRow>.Enumerator GetEnumerator() {
		return _rows.GetEnumerator();
	}
}

public class CSVRow {
	SortedDictionary<int, ValueVariable> _columns;

	public int Count {
		get { return _columns.Count; }
	}

	/*public int LastIndex {
		get { return _columns.Keys.Last(); }
	}*/

	int _index;
	public int Index {
		set { _index=value; }
		get { return _index; }
	}

	public SortedDictionary<int, ValueVariable>.KeyCollection ColumnIndices {
		get { return _columns.Keys; }
	}

	public SortedDictionary<int, ValueVariable>.ValueCollection Values {
		get { return _columns.Values; }
	}

	public CSVRow(int index) {
		_columns=new SortedDictionary<int, ValueVariable>();
		_index=index;
	}

	public bool Add(int index, ValueVariable val) {
		if (val!=null) {
			_columns[index]=val;
			return true;
		}
		return false;
	}

	public bool Remove(int index) {
		return _columns.Remove(index);
	}

	public ValueVariable Get(int index) {
		ValueVariable val=null;
		if (_columns.TryGetValue(index, out val))
			return val;
		return null;
	}

	public bool Has(int index) {
		return _columns.ContainsKey(index);
	}

	public SortedDictionary<int, ValueVariable>.Enumerator GetEnumerator() {
		return _columns.GetEnumerator();
	}
}

public enum CSVToken : int {
	None=0,
	String,
	QuotedString,
	Number,
	Double,
	Separator,
	EOF,
	EOL
}

public class CSVParser {
	const char CHAR_EOF='\xFFFF';
	const char CHAR_NEWLINE='\n';
	const char CHAR_CARRIAGERETURN='\r';
	const char CHAR_DECIMALPOINT='.';
	const char CHAR_QUOTE='\"';
	const char CHAR_BACKSLASH='\\';

	static CharacterSet _numberset=new CharacterSet("0-9\\-+");
	static CharacterSet _numeralset=new CharacterSet("0-9");
	static CharacterSet _signset=new CharacterSet("\\-+");

	CharacterSet _whitespaceset=new CharacterSet();

	char _curchar;

	int _line;
	public int Line {
		get { return _line; }
	}

	int _col;
	public int Column {
		get { return _col; }
	}

	CSVParserHandler _handler;
	public CSVParserHandler Handler {
		set { _handler=value; }
		get { return _handler; }
	}

	Token<CSVToken> _token=new Token<CSVToken>(CSVToken.None);
	public Token<CSVToken> Token {
		get { return _token; }
	}

	StreamReader _stream;
	public StreamReader Stream {
		get { return _stream; }
	}

	char _separator;
	public char Separator {
		set {
			_separator=value;
			_whitespaceset.Clear();
			if (_separator!=' ')
				_whitespaceset.AddRange(' ');
			if (_separator!='\t')
				_whitespaceset.AddRange('\t');
		}
		get { return _separator; }
	}

	public CSVParser() {
		Clean();
	}

	public CSVParser(StreamReader stream) {
		InitWithStream(stream);
	}

	public void InitWithStream(StreamReader stream) {
		Clean();
		_stream=stream;
		NextChar(); // Get the first character
	}

	public void Clean() {
		_token.Reset(CSVToken.None);
		_line=1;
		_col=0;
		_stream=null;
		_curchar=CHAR_EOF;
	}

	public bool Parse() {
		//NextChar();
		SkipWhitespace();
		NextToken();
		ReadToken();
		if (_curchar==CHAR_EOF) {
			_token.Reset(CSVToken.EOF);
			_handler.HandleToken(_token); // Just to make sure the EOF gets handled (data might not end with a newline, causing an EOF token)
			return false;
		} else if (_token.Type==CSVToken.EOF) {
			return false;
		}
		return true;
	}

	char NextChar() {
		if (_curchar==CHAR_NEWLINE) {
			_line++;
			_col=0;
		}
		if (_stream.Peek()!=-1)
			_curchar=(char)_stream.Read();
		else
			_curchar=CHAR_EOF;
		if (_curchar==CHAR_CARRIAGERETURN) // Skip \r -- IT WAS NEVER THERE
			NextChar();
		else if (_curchar!=CHAR_EOF)
			_col++;
		return _curchar;
	}
	
	void SkipWhitespace() {
		while (_curchar!=CHAR_EOF && _whitespaceset.Contains(_curchar))
			NextChar();
	}

	Token<CSVToken> NextToken() {
		//if (_curchar=='\n')
		//	Console.WriteLine("CSVParser.NextToken() char: \\n");
		//else
		//	Console.WriteLine("CSVParser.NextToken() char: {0}", _curchar);
		_token.Reset(CSVToken.None);
		_token.SetPosition(_line, _col);
		switch (_curchar) {
			case CHAR_QUOTE:
				_token.Type=CSVToken.QuotedString;
				break;
			case CHAR_EOF:
				_token.Type=CSVToken.EOF;
				break;
			case CHAR_NEWLINE:
				_token.Type=CSVToken.EOL;
				break;
			case CHAR_DECIMALPOINT:
				_token.Type=CSVToken.Double;
				_token.Add(_curchar); // Add the decimal
				break;
			default:
				if (_curchar==_separator) {
					_token.Type=CSVToken.Separator;
				} else if (_numberset.Contains(_curchar)) {
					_token.Type=CSVToken.Number;
					_token.Add(_curchar); // Add the number/sign
				} else {
					_token.Type=CSVToken.String;
				}
				break;
		}
		return _token;
	}
	
	void ReadToken() {
		//Console.WriteLine("CSVParser.ReadToken() [in] {0}:{1} token.Type: {2}", _token.Line, _token.Column, _token.Type.ToString());
		switch (_token.Type) {
			case CSVToken.QuotedString:
				ReadQuotedStringToken();
				NextChar();
				break;
			case CSVToken.String:
				ReadStringToken();
				break;
			case CSVToken.Number:
				NextChar();
				ReadNumberToken();
				break;
			case CSVToken.Double:
				NextChar();
				ReadDoubleToken();
				break;
			case CSVToken.Separator:
				NextChar();
				break;
			case CSVToken.EOL:
				NextChar();
				break;
			case CSVToken.EOF:
				// Do nothing
				break;
			//default:
			//	throw new CSVParserException(CSVParserError.PARSER, "ReadToken", _token, this, "Unhandled token");
			//	break;
		}
		//Console.WriteLine("CSVParser.ReadToken() [out] {0}:{1} token.Type: {2}", _token.Line, _token.Column, _token.Type.ToString());
		// Special resolve when Number and Double tokens only contain signs or periods
		switch (_token.Type) {
			case CSVToken.Number:
				if (_token.Compare(_signset))
					_token.Type=CSVToken.String;
				break;
			case CSVToken.Double:
				if (_token.Compare(_signset) || _token.Compare(CHAR_DECIMALPOINT))
					_token.Type=CSVToken.String;
				break;
			default:
				break;
		}
		//Console.WriteLine("CSVParser.ReadToken() [resolve] {0}:{1} token.Type: {2}", _token.Line, _token.Column, _token.Type.ToString());
		_handler.HandleToken(_token);
	}
	
	void ReadNumberToken() {
		while (_curchar!=CHAR_EOF) {
			if (_curchar==CHAR_QUOTE) {
				throw new CSVParserException(CSVParserError.PARSER, "ReadNumberToken", _token, this, "Unexpected quote");
			} else if (_curchar==_separator || _curchar==CHAR_NEWLINE || _whitespaceset.Contains(_curchar)) {
				break;
			} else if (_numeralset.Contains(_curchar)) {
				_token.Add(_curchar);
			} else if (_curchar==CHAR_DECIMALPOINT) {
				_token.Add(_curchar);
				NextChar();
				_token.Type=CSVToken.Double;
				ReadDoubleToken();
				break;
			} else {
				_token.Type=CSVToken.String;
				ReadStringToken();
				break;
			}
			NextChar();
		}
	}
	
	void ReadDoubleToken() {
		while (_curchar!=CHAR_EOF) {
			if (_curchar==CHAR_QUOTE) {
				throw new CSVParserException(CSVParserError.PARSER, "ReadDoubleToken", _token, this, "Unexpected quote");
			} else if (_curchar==_separator || _curchar==CHAR_NEWLINE || _whitespaceset.Contains(_curchar)) {
				break;
			} else if (_numeralset.Contains(_curchar)) {
				_token.Add(_curchar);
			} else { // (_curchar==CHAR_DECIMALPOINT)
				// The token should've already contained a decimal point, so it must be a string.
				_token.Type=CSVToken.String;
				ReadStringToken();
				break;
			}
			NextChar();
		}
	}

	void ReadStringToken() {
		while (_curchar!=CHAR_EOF) {
			if (_curchar==CHAR_QUOTE) {
				throw new CSVParserException(CSVParserError.PARSER, "ReadStringToken", _token, this, "Unexpected quote");
			} else if (_curchar==CHAR_BACKSLASH) {
				char c=Variable.GetEscapeChar(NextChar());
				if (c!=CHAR_EOF)
					_token.Add(c);
				else
					throw new CSVParserException(CSVParserError.PARSER, "ReadStringToken", _token, this, String.Format("Unknown escape sequence: {0}", _curchar));
			} else if (_curchar==_separator || _curchar==CHAR_NEWLINE /*|| _whitespaceset.Contains(_curchar)*/) {
				break;
			} else {
				_token.Add(_curchar);
			}
			NextChar();
		}
	}

	void ReadQuotedStringToken() {
		bool eolreached=false;
		NextChar(); // Skip the first character (it will be the initial quote)
		while (_curchar!=CHAR_QUOTE) {
			if (_curchar==CHAR_EOF) {
				throw new CSVParserException(CSVParserError.PARSER, "ReadQuotedStringToken", _token, this, "Encountered EOF whilst reading quoted string");
			} else if (_curchar==CHAR_BACKSLASH) {
				char c=Variable.GetEscapeChar(NextChar());
				if (c!=CHAR_EOF)
					_token.Add(c);
				else
					throw new CSVParserException(CSVParserError.PARSER, "ReadQuotedStringToken", _token, this, String.Format("Unknown escape sequence: {0}", _curchar));
			} else {
				if (!eolreached)
					_token.Add(_curchar);
				if (_curchar==CHAR_NEWLINE) {
					//throw new CSVParserException(CSVParserError.PARSER, "ReadQuotedStringToken", _token, this, "Unclosed quote (met EOL character)");
					eolreached=true;
				} else if (eolreached && !_whitespaceset.Contains(_curchar)) {
					eolreached=false;
					_token.Add(_curchar);
				}
			}
			NextChar();
		}
	}
}

public abstract class CSVParserHandler {
	protected CSVParser _parser=new CSVParser();
	protected CSVMap _map;
	protected CSVRow _currentrow;
	protected int _rowindex, _beginningrow;
	protected int _columnindex;

	protected virtual void Init() {
		_parser.Handler=this;
	}

	public virtual void SetSeparator(char separator) {
		_parser.Separator=separator;
	}

	public virtual void SetBeginningRow(int beginningrow) {
		_beginningrow=beginningrow;
	}

	protected virtual void Clean() {
		_map=null;
		_currentrow=null;
		_rowindex=_beginningrow;
		_columnindex=0;
	}

	protected virtual void Process() {
		_map=new CSVMap();
		while (_parser.Parse()) {
		}
		Finish();
	}

	public CSVMap ProcessFromStream(StreamReader stream) {
		_parser.InitWithStream(stream);
		Clean();
		Process();
		CSVMap map=_map; // Store before cleaning
		Clean();
		_parser.Clean();
		return map;
	}

	public abstract void HandleToken(Token<CSVToken> token);
	protected abstract void Finish();
}

public enum CSVParserError {
	/** Parser error. */
	UNKNOWN=0,
	/** Parser error. */
	PARSER,
	/** Memory allocation error (e.g. out of memory). */
	MEMALLOC
}

public class CSVParserException : Exception {
	CSVParserError _error;
	string _reporter, _message;
	Token<CSVToken> _token;
	CSVParser _parser;

	/**
		Constructor with values.
	*/
	public CSVParserException(CSVParserError error, string reporter, Token<CSVToken> token, CSVParser parser, string message) {
		_error=error;
		_reporter=reporter;
		_token=token;
		_parser=parser;
		_message=message;
		if (_parser!=null && _token==null)
			_token=_parser.Token;
	}

	public override string ToString() {
		if (_token!=null && _parser!=null)
			return String.Format("({0}) [{1}] from line: {2}, col: {3} to line: {4}, col: {5}: {6}", _reporter, ErrorName(_error), _token.Line, _token.Column, _parser.Line, _parser.Column, _message);
		else if (_token!=null)
			return String.Format("({0}) [{1}] at line: {2}, col: {3}: {4}", _reporter, ErrorName(_error), _token.Line, _token.Column, _message);
		else if (_parser!=null)
			return String.Format("({0}) [{1}] at line: {2}, col: {3}: {4}", _reporter, ErrorName(_error), _parser.Line, _parser.Column, _message);
		else
			return String.Format("({0}) [{1}]: {2}", _reporter, ErrorName(_error), _message);
	}

	public static string ErrorName(CSVParserError error) {
		switch (error) {
			case CSVParserError.PARSER:
				return "ERROR_PARSER";
			case CSVParserError.MEMALLOC:
				return "ERROR_MEMALLOC";
			default:
				return "ERROR_UNKNOWN";
		}
	}
}

class StandardCSVParserHandler : CSVParserHandler {
	bool _lastempty;

	public StandardCSVParserHandler() {
		Init();
	}

	protected override void Clean() {
		base.Clean();
		_lastempty=true;
	}

	public override void HandleToken(Token<CSVToken> token) {
		switch (token.Type) {
			case CSVToken.String:
				string str=token.ToString().Trim();
				int bv=Variable.StringToBool(str);
				if (bv!=-1)
					AddToRow(new BoolVariable(bv==1 ? true : false));
				else
					AddToRow(new StringVariable(str));
				break;
			case CSVToken.QuotedString:
				AddToRow(new StringVariable(token.ToString()));
				break;
			case CSVToken.Number:
				AddToRow(new IntVariable(token.ToInt()));
				break;
			case CSVToken.Double:
				AddToRow(new FloatVariable(token.ToFloat()));
				break;
			case CSVToken.Separator:
				if (_lastempty)
					AddToRow(new StringVariable());
				_columnindex++;
				_lastempty=true;
				break;
			case CSVToken.EOL:
			case CSVToken.EOF:
				if (_lastempty && _columnindex!=0)
					AddToRow(new StringVariable());
				NewRow();
				break;
			//default:
			//	//DebugLog("(StandardCSVParserHandler.handleToken) Unhandled token of type "+token.typeAsString())
			//	break;
		}
	}

	protected override void Finish() {
		//if (_lastempty)
		//	AddToRow(new StringVariable());
	}

	void AddToRow(ValueVariable val) {
		if (_currentrow==null)
			NewRow();
		//_currentrow.Add(new CSVRecord(_columnindex, val));
		_currentrow.Add(_columnindex, val);
		_lastempty=false;
	}

	void NewRow() {
		if (_currentrow!=null)
			_map.AddRow(_currentrow);
		_currentrow=new CSVRow(_rowindex);
		_rowindex++;
		_columnindex=0;
		_lastempty=true;
	}
}

public class CSVFormatter {
	static StandardCSVParserHandler _handler=new StandardCSVParserHandler();

	public static bool FormatRow(CSVRow row, out string result) {
		return FormatRow(row, out result, ',');
	}

	public static bool FormatRow(CSVRow row, out string result, char separator) {
		return FormatRow(row, out result, separator, ValueFormat.ALL_DEFAULT);
	}

	public static bool FormatRow(CSVRow row, out string result, char separator, ValueFormat varformat) {
		if (row!=null) {
			StringBuilder builder=new StringBuilder(256);
			int lastcolumn=0;
			foreach (KeyValuePair<int, ValueVariable> pair in row) {
				for (; lastcolumn<pair.Key; ++lastcolumn)
					builder.Append(separator);
				builder.Append(pair.Value.GetValueFormatted(varformat));
			}
			result=builder.ToString();
			return true;
		}
		result=String.Empty; // clear the result string
		return false;
	}

	public static CSVMap LoadFromFile(string path) {
		return LoadFromFile(path, ',');
	}

	public static CSVMap LoadFromFile(string path, char separator) {
		return LoadFromFile(path, separator, false);
	}

	public static CSVMap LoadFromFile(string path, char separator, bool header) {
		return LoadFromFile(path, separator, header, Encoding.UTF8);
	}

	public static CSVMap LoadFromFile(string path, char separator, bool header, Encoding encoding) {
		//try {
			StreamReader stream=new StreamReader(path, encoding, false);
			CSVMap map=LoadFromStream(stream, separator, header);
			stream.Close();
			return map;
		//} catch (FileNotFoundException e) {
		//	return null;
		//}
		//return null;
	}

	public static CSVMap LoadFromStream(StreamReader stream) {
		return LoadFromStream(stream, ',');
	}

	public static CSVMap LoadFromStream(StreamReader stream, char separator) {
		return LoadFromStream(stream, separator, false);
	}

	public static CSVMap LoadFromStream(StreamReader stream, char separator, bool header) {
		if (stream!=null) {
			_handler.SetBeginningRow(header ? -1 : 0);
			_handler.SetSeparator(separator);
			return _handler.ProcessFromStream(stream);
		}
		return null;
	}

	public static bool WriteToFile(CSVMap map, string path) {
		return WriteToFile(map, path, ',');
	}

	public static bool WriteToFile(CSVMap map, string path, char separator) {
		return WriteToFile(map, path, separator, ValueFormat.ALL_DEFAULT);
	}

	public static bool WriteToFile(CSVMap map, string path, char separator, ValueFormat varformat) {
		return WriteToFile(map, path, separator, varformat, Encoding.UTF8);
	}

	public static bool WriteToFile(CSVMap map, string path, char separator, ValueFormat varformat, Encoding encoding) {
		//try {
			StreamWriter stream=new StreamWriter(path, false, encoding);
			bool ret=WriteToStream(map, stream, separator, varformat);
			stream.Close();
			return ret;
		//} catch {
		//}
		//return false;
	}

	public static bool WriteToStream(CSVMap map, StreamWriter stream, char separator, ValueFormat varformat) {
		if (map!=null && stream!=null) {
			string temp;
			bool first=false;
			int lastrow=0;
			foreach (KeyValuePair<int, CSVRow> pair in map) {
				if (!first) {
					lastrow=pair.Key;
					first=true;
				}
				for (; lastrow!=pair.Key; ++lastrow)
					stream.WriteLine();
				if (FormatRow(pair.Value, out temp, separator, varformat)) {
					stream.Write(temp);
				}
			}
			return true;
		}
		return false;
	}
}

} // namespace duct
