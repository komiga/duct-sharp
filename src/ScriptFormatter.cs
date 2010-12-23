
using System;
using System.IO;
using System.Text;
using duct;

namespace duct {

public enum ScriptToken : int {
	None = 0,

	String,
	QuotedString,
	Number,
	Double,

	Equals,

	OpenBrace,
	CloseBrace,

	Comment,
	CommentBlock,
	EOF,
	EOL
}

public class ScriptParser {

	const char CHAR_EOF = '\xFFFF';
	const char CHAR_NEWLINE = '\n';
	const char CHAR_CARRIAGERETURN = '\r';

	const char CHAR_DECIMALPOINT = '.';

	const char CHAR_QUOTE = '\"';
	const char CHAR_SLASH = '/';
	const char CHAR_BACKSLASH = '\\';
	const char CHAR_ASTERISK = '*';

	const char CHAR_OPENBRACE = '{';
	const char CHAR_CLOSEBRACE = '}';
	const char CHAR_EQUALSIGN = '=';

	static CharacterSet _whitespaceset = new CharacterSet("\t ");
	static CharacterSet _numberset = new CharacterSet("0-9\\-+");
	static CharacterSet _numeralset = new CharacterSet("0-9");
	static CharacterSet _signset = new CharacterSet("\\-+");

	char _curchar, _peekchar;

	int _line;
	public int Line {
		get { return _line; }
	}

	int _col;
	public int Column {
		get { return _col; }
	}

	ScriptParserHandler _handler;
	public ScriptParserHandler Handler {
		set { _handler = value; }
		get { return _handler; }
	}

	Token<ScriptToken> _token = new Token<ScriptToken>(ScriptToken.None);
	public Token<ScriptToken> Token {
		get { return _token; }
	}

	StreamReader _stream;
	public StreamReader Stream {
		get { return _stream; }
	}

	public ScriptParser() {
		Clean();
	}

	public ScriptParser(StreamReader stream) {
		InitWithStream(stream);
	}

	~ScriptParser() {
		Clean();
	}

	public void InitWithStream(StreamReader stream) {
		Clean();
		_stream = stream;
		NextChar(); // Get the first character
	}

	public void Clean() {
		_token.Reset(ScriptToken.None);
		_line = 1;
		_col = 0;
		_stream = null;
		_curchar = CHAR_EOF;
		_peekchar = CHAR_EOF;
	}

	public bool Parse() {
		//NextChar();
		SkipWhitespace();
		NextToken();
		ReadToken();
		if (_curchar == CHAR_EOF) {
			_token.Reset(ScriptToken.EOF);
			_handler.HandleToken(_token); // Just to make sure the EOF gets handled (data might not end with a newline, causing an EOF token)
			return false;
		} else if (_token.Type == ScriptToken.EOF) {
			return false;
		}
		return true;
	}

	char NextChar() {
		if (_curchar == CHAR_NEWLINE) {
			_line++;
			_col = 0;
		}
		if (_stream.Peek() != -1)
			_curchar = (char)_stream.Read();
		else
			_curchar = CHAR_EOF;
		if (_curchar == CHAR_CARRIAGERETURN) // Skip \r -- IT WAS NEVER THERE
			NextChar();
		else if (_curchar != CHAR_EOF)
			_col++;
		return _curchar;
	}

	char PeekChar() {
		_peekchar = (char)_stream.Peek();
		return _peekchar;
	}

	void SkipWhitespace() {
		while (_curchar != CHAR_EOF && _whitespaceset.Contains(_curchar))
			NextChar();
	}

	void SkipToEOL() {
		while (_curchar != CHAR_EOF && _curchar != CHAR_NEWLINE)
			NextChar();
	}
	
	bool SkipToChar(char c) {
		while (_curchar != CHAR_EOF && _curchar != c)
			NextChar();
		return _curchar == c;
	}

	Token<ScriptToken> NextToken() {
		//if (_curchar == '\n')
		//	Console.WriteLine("ScriptParser.NextToken() char: \\n");
		//else
		//	Console.WriteLine("ScriptParser.NextToken() char: {0}", _curchar);
		_token.Reset(ScriptToken.None);
		_token.SetPosition(_line, _col);
		switch (_curchar) {
			case CHAR_QUOTE:
				_token.Type = ScriptToken.QuotedString;
				break;
			case CHAR_ASTERISK:
				if (PeekChar() == CHAR_SLASH)
					throw new ScriptParserException(ScriptParserError.PARSER, "NextToken", _token, this, "Encountered unexpected end of block comment");
				_token.Type = ScriptToken.String;
				break;
			case CHAR_SLASH:
				if (PeekChar() == CHAR_SLASH)
					_token.Type = ScriptToken.Comment;
				else if (_peekchar == CHAR_ASTERISK)
					_token.Type = ScriptToken.CommentBlock;
				else
					_token.Type = ScriptToken.String;
				break;
			case CHAR_EOF:
				_token.Type = ScriptToken.EOF;
				break;
			case CHAR_NEWLINE:
				_token.Type = ScriptToken.EOL;
				break;
			case CHAR_DECIMALPOINT:
				_token.Type = ScriptToken.Double;
				_token.Add(_curchar); // Add the decimal
				break;
			case CHAR_EQUALSIGN:
				_token.Type = ScriptToken.Equals;
				break;
			case CHAR_OPENBRACE:
				_token.Type = ScriptToken.OpenBrace;
				break;
			case CHAR_CLOSEBRACE:
				_token.Type = ScriptToken.CloseBrace;
				break;
			default:
				if (_numberset.Contains(_curchar)) {
					_token.Type = ScriptToken.Number;
					_token.Add(_curchar); // Add the number/sign
				} else {
					_token.Type = ScriptToken.String;
				}
				break;
		}
		return _token;
	}

	void ReadToken() {
		//Console.WriteLine("ScriptParser.ReadToken() [in] {0}:{1} token.Type: {2}", _token.Line, _token.Column, _token.Type.ToString());
		switch (_token.Type) {
			case ScriptToken.QuotedString:
				ReadQuotedStringToken();
				NextChar();
				break;
			case ScriptToken.String:
				ReadStringToken();
				break;
			case ScriptToken.Number:
				NextChar();
				ReadNumberToken();
				break;
			case ScriptToken.Double:
				NextChar();
				ReadDoubleToken();
				break;
			case ScriptToken.Equals:
				NextChar();
				break;
			case ScriptToken.Comment:
				SkipToEOL();
				//NextChar(); // Bad to get the next char, as it could be the EOL needed to terminate the current identifier
				break;
			case ScriptToken.CommentBlock:
				ReadCommentBlockToken();
				break;
			case ScriptToken.OpenBrace:
			case ScriptToken.CloseBrace:
				NextChar();
				break;
			case ScriptToken.EOL:
				NextChar();
				break;
			case ScriptToken.EOF:
				// Do nothing
				break;
			//default:
			//	throw new ScriptParserException(ScriptParserError.PARSER, "ReadToken", _token, this, "Unhandled token");
			//	break;
		}
		//Console.WriteLine("ScriptParser.ReadToken() [out] {0}:{1} token.Type: {2}", _token.Line, _token.Column, _token.Type.ToString());
		// Special resolve when Number and Double tokens only contain signs or periods
		switch (_token.Type) {
			case ScriptToken.Number:
				if (_token.Compare(_signset))
					_token.Type = ScriptToken.String;
				break;
			case ScriptToken.Double:
				if (_token.Compare(_signset) || _token.Compare(CHAR_DECIMALPOINT))
					_token.Type = ScriptToken.String;
				break;
			default:
				break;
		}
		//Console.WriteLine("ScriptParser.ReadToken() [resolve] {0}:{1} token.Type: {2}", _token.Line, _token.Column, _token.Type.ToString());
		_handler.HandleToken(_token);
	}
	
	void ReadNumberToken() {
		while (_curchar != CHAR_EOF) {
			if (_curchar == CHAR_QUOTE) {
				throw new ScriptParserException(ScriptParserError.PARSER, "ReadNumberToken", _token, this, "Unexpected quote");
			} else if (_curchar == CHAR_SLASH) {
				if (PeekChar() == CHAR_SLASH || _peekchar == CHAR_ASTERISK) {
					break;
				} else {
					_token.Type = ScriptToken.String;
					ReadStringToken();
					break;
				}
			} else if (_curchar == CHAR_NEWLINE || _whitespaceset.Contains(_curchar) || _curchar == CHAR_CLOSEBRACE || _curchar == CHAR_EQUALSIGN) {
				break;
			} else if (_numeralset.Contains(_curchar)) {
				_token.Add(_curchar);
			} else if (_curchar == CHAR_DECIMALPOINT) {
				_token.Add(_curchar);
				NextChar();
				_token.Type = ScriptToken.Double;
				ReadDoubleToken();
				break;
			} else {
				_token.Type = ScriptToken.String;
				ReadStringToken();
				break;
			}
			NextChar();
		}
	}
	
	void ReadDoubleToken() {
		while (_curchar != CHAR_EOF) {
			if (_curchar == CHAR_QUOTE) {
				throw new ScriptParserException(ScriptParserError.PARSER, "ReadDoubleToken", _token, this, "Unexpected quote");
			} else if (_curchar == CHAR_SLASH) {
				if (PeekChar() == CHAR_SLASH || _peekchar == CHAR_ASTERISK) {
					break;
				} else {
					_token.Type = ScriptToken.String;
					ReadStringToken();
					break;
				}
			} else if (_curchar == CHAR_NEWLINE || _whitespaceset.Contains(_curchar) || _curchar == CHAR_CLOSEBRACE || _curchar == CHAR_EQUALSIGN) {
				break;
			} else {
				if (_numeralset.Contains(_curchar)) {
					_token.Add(_curchar);
				} else { // (_curchar == CHAR_DECIMALPOINT)
					// The token should've already contained a decimal point, so it must be a string.
					_token.Type = ScriptToken.String;
					ReadStringToken();
					break;
				}
			}
			NextChar();
		}
	}
	
	void ReadStringToken() {
		while (_curchar != CHAR_EOF) {
			if (_curchar == CHAR_QUOTE) {
				throw new ScriptParserException(ScriptParserError.PARSER, "ReadStringToken", _token, this, "Unexpected quote");
			} else if (_curchar == CHAR_BACKSLASH) {
				char c = Variable.GetEscapeChar(NextChar());
				if (c != CHAR_EOF)
					_token.Add(c);
				else
					throw new ScriptParserException(ScriptParserError.PARSER, "ReadStringToken", _token, this, String.Format("Unknown escape sequence: {0}", _curchar));
			} else if (_curchar == CHAR_NEWLINE || _whitespaceset.Contains(_curchar)
					|| (_curchar == CHAR_SLASH && (PeekChar() == CHAR_SLASH || _peekchar == CHAR_ASTERISK))
					|| _curchar == CHAR_CLOSEBRACE || _curchar == CHAR_EQUALSIGN) {
				break;
			} else {
				_token.Add(_curchar);
			}
			NextChar();
		}
	}

	void ReadQuotedStringToken() {
		bool eolreached = false;
		NextChar(); // Skip the first character (it will be the initial quote)
		while (_curchar != CHAR_QUOTE) {
			if (_curchar == CHAR_EOF) {
				throw new ScriptParserException(ScriptParserError.PARSER, "ReadQuotedStringToken", _token, this, "Encountered EOF whilst reading quoted string");
			} else if (_curchar == CHAR_BACKSLASH) {
				char c = Variable.GetEscapeChar(NextChar());
				if (c != CHAR_EOF)
					_token.Add(c);
				else
					throw new ScriptParserException(ScriptParserError.PARSER, "ReadQuotedStringToken", _token, this, String.Format("Unknown escape sequence: {0}", _curchar));
			} else {
				if (!eolreached)
					_token.Add(_curchar);
				if (_curchar == CHAR_NEWLINE) {
					//throw new ScriptParserException(ScriptParserError.PARSER, "ReadQuotedStringToken", _token, this, "Unclosed quote (met EOL character)");
					eolreached = true;
				} else if (eolreached && !_whitespaceset.Contains(_curchar)) {
					eolreached = false;
					_token.Add(_curchar);
				}
			}
			NextChar();
		}
	}
	
	void ReadCommentBlockToken() {
		NextChar(); // Skip the first character (it will be an asterisk)
		if (_curchar != CHAR_EOF) {
			while (SkipToChar(CHAR_ASTERISK)) {
				if (NextChar() == CHAR_SLASH) {
					NextChar(); // Get the next character, otherwise the NextToken() call will try to handle the slash
					return;
				}
			}
		}
		throw new ScriptParserException(ScriptParserError.PARSER, "ReadCommentBlock", _token, this, "Unexpected EOF");
	}

}

public abstract class ScriptParserHandler {

	protected ScriptParser _parser = new ScriptParser();
	protected Node _rootnode;
	protected Node _currentnode;

	protected virtual void Init() {
		_parser.Handler = this;
	}

	protected virtual void Clean() {
		_currentnode = null;
		_rootnode = null;
	}

	protected virtual void Process() {
		_rootnode = new Node((CollectionVariable)null);
		_currentnode = _rootnode;
		while (_parser.Parse()) {
		}
		Finish();
		if (_currentnode != _rootnode)
			throw new ScriptParserException(ScriptParserError.HIERARCHY, "Process", null, null, "The current node does not match the root node");
	}

	public Node ProcessFromStream(StreamReader stream) {
		_parser.InitWithStream(stream);
		Process();
		Node node = _rootnode; // Store before cleaning
		Clean();
		_parser.Clean();
		return node;
	}

	public abstract void HandleToken(Token<ScriptToken> token);
	protected abstract void Finish();

}

public enum ScriptParserError {
	/** Parser error. */
	UNKNOWN = 0,
	/** Parser error. */
	PARSER,
	/** Hierarchy error (e.g. node opened but not closed). */
	HIERARCHY,
	/** Memory allocation error (e.g. out of memory). */
	MEMALLOC
}

public class ScriptParserException : Exception {

	ScriptParserError _error;
	string _reporter, _message;
	Token<ScriptToken> _token;
	ScriptParser _parser;

	/**
		Constructor with values.
	*/
	public ScriptParserException(ScriptParserError error, string reporter, Token<ScriptToken> token, ScriptParser parser, string message) {
		_error = error;
		_reporter = reporter;
		_token = token;
		_parser = parser;
		_message = message;
		if (_parser != null && _token == null)
			_token = _parser.Token;
	}

	public override string ToString() {
		if (_token != null && _parser != null)
			return String.Format("({0}) [{1}] from line: {2}, col: {3} to line: {4}, col: {5}: {6}", _reporter, ErrorName(_error), _token.Line, _token.Column, _parser.Line, _parser.Column, _message);
		else if (_token != null)
			return String.Format("({0}) [{1}] at line: {2}, col: {3}: {4}", _reporter, ErrorName(_error), _token.Line, _token.Column, _message);
		else if (_parser != null)
			return String.Format("({0}) [{1}] at line: {2}, col: {3}: {4}", _reporter, ErrorName(_error), _parser.Line, _parser.Column, _message);
		else
			return String.Format("({0}) [{1}]: {2}", _reporter, ErrorName(_error), _message);
	}

	public static string ErrorName(ScriptParserError error) {
		switch (error) {
			case ScriptParserError.PARSER:
				return "ERROR_PARSER";
			case ScriptParserError.HIERARCHY:
				return "ERROR_HIERARCHY";
			case ScriptParserError.MEMALLOC:
				return "ERROR_MEMALLOC";
			default:
				return "ERROR_UNKNOWN";
		}
	}

}

class StandardScriptParserHandler : ScriptParserHandler {

	string _varname;
	bool _equals;
	Identifier _currentiden;
	ValueVariable _currentvalue;

	public StandardScriptParserHandler() {
		Init();
	}

	protected override void Clean() {
		base.Clean();
		_varname = String.Empty;
		_equals = false;
		_currentvalue = null;
		_currentiden = null;
	}

	public override void HandleToken(Token<ScriptToken> token) {
		switch (token.Type) {
			case ScriptToken.String:
				if (!String.IsNullOrEmpty(_varname) && _equals) {
					int bv = Variable.StringToBool(token.ToString());
					if (bv != -1) {
						AddVariableAndReset(_currentnode, new BoolVariable(bv == 1 ? true : false, _varname), false, false);
						break;
					} else {
						AddVariableAndReset(_currentnode, new StringVariable(token.ToString(), _varname), false, false);
					}
				} else if ((!String.IsNullOrEmpty(_varname) || _currentiden != null) && !_equals) {
					MakeIdentifier(token);
					int bv = Variable.StringToBool(token.ToString());
					if (bv != -1) {
						AddVariableAndReset(_currentiden, new BoolVariable(bv == 1 ? true : false), false, false);
						break;
					}
					AddVariableAndReset(_currentiden, new StringVariable(token.ToString()), false, false);
				} else {
					_varname = token.ToString();
				}
				break;
			case ScriptToken.QuotedString:
				if (!String.IsNullOrEmpty(_varname) && _equals) {
					AddVariableAndReset(_currentnode, new StringVariable(token.ToString(), _varname), false, false);
				} else if ((!String.IsNullOrEmpty(_varname) || _currentiden != null) && !_equals) {
					MakeIdentifier(token);
					AddVariableAndReset(_currentiden, new StringVariable(token.ToString()), false, false);
				} else {
					_varname = token.ToString();
				}
				break;
			case ScriptToken.Number:
				if (!String.IsNullOrEmpty(_varname) && _equals) {
					_currentvalue = new IntVariable(token.ToInt(), _varname);
					AddVariableAndReset(_currentnode, _currentvalue, false, false);
				} else if ((!String.IsNullOrEmpty(_varname) || _currentiden != null) && !_equals) {
					MakeIdentifier(token);
					AddVariableAndReset(_currentiden, new IntVariable(token.ToInt()), false, false);
				} else {
					//new ScriptParserException(ScriptParserError.PARSER, "StandardScriptParserHandler.HandleToken", token, _parser, "A number cannot be an identifier");
					_varname = token.ToString();
				}
				break;
			case ScriptToken.Double:
				if (!String.IsNullOrEmpty(_varname) && _equals) {
					_currentvalue = new FloatVariable((float)token.ToDouble(), _varname);
					AddVariableAndReset(_currentnode, _currentvalue, false, false);
				} else if ((!String.IsNullOrEmpty(_varname) || _currentiden != null) && !_equals) {
					MakeIdentifier(token);
					AddVariableAndReset(_currentiden, new FloatVariable((float)token.ToDouble()), false, false);
				} else {
					new ScriptParserException(ScriptParserError.PARSER, "StandardScriptParserHandler.HandleToken", token, _parser, "A number cannot be an identifier");
					_varname = token.ToString();
				}
				break;
			case ScriptToken.Equals:
				if (_currentiden != null)
					new ScriptParserException(ScriptParserError.PARSER, "StandardScriptParserHandler.HandleToken", token, _parser, "Unexpected equality sign after identifier declaration");
				else if (_varname.Length == 0)
					new ScriptParserException(ScriptParserError.PARSER, "StandardScriptParserHandler.HandleToken", token, _parser, "Expected string, got equality sign");
				else if (_equals)
					new ScriptParserException(ScriptParserError.PARSER, "StandardScriptParserHandler.HandleToken", token, _parser, "Expected value, got equality sign");
				else
					_equals = true;
				break;
			case ScriptToken.OpenBrace:
				if (_currentiden != null)
					new ScriptParserException(ScriptParserError.PARSER, "StandardScriptParserHandler.HandleToken", token, _parser, "Node cannot contain values (possible openbrace typo)");
				Node tempnode = new Node(_varname, _currentnode);
				AddVariableAndReset(_currentnode, tempnode, false, false);
				_currentnode = tempnode;
				break;
			case ScriptToken.CloseBrace:
				if (_currentnode.Parent == null) {
					new ScriptParserException(ScriptParserError.PARSER, "StandardScriptParserHandler.HandleToken", token, _parser, "Mismatched node brace");
				} else if (_equals) {
					new ScriptParserException(ScriptParserError.PARSER, "StandardScriptParserHandler.HandleToken", token, _parser, "Expected value, got close-brace");
				} else {
					if (_currentiden != null)
						Reset(true, true);
					_currentnode = (Node)_currentnode.Parent;
				}
				break;
			case ScriptToken.Comment:
			case ScriptToken.CommentBlock:
				// Do nothing
				break;
			case ScriptToken.EOL:
			case ScriptToken.EOF:
				Finish();
				break;
			//default:
			//	//DebugLog("(StandardScriptParserHandler.handleToken) Unhandled token of type " + token.typeAsString())
			//	break;
		}
	}

	protected override void Finish() {
		if ((_parser.Token.Type == ScriptToken.EOL || _parser.Token.Type == ScriptToken.EOF) && _equals) {
			throw new ScriptParserException(ScriptParserError.PARSER, "StandardScriptParserHandler.Finish", _parser.Token, _parser, "Expected value, got EOL/EOF");
		} else if (!String.IsNullOrEmpty(_varname)) { // no-value identifier
			MakeIdentifier(null, true, true, true);
		} else {
			Reset(true, true);
		}
	}

	void Reset(bool iden, bool val) {
		_varname = String.Empty;
		_equals = false;
		if (val)
			_currentvalue = null;
		if (iden)
			_currentiden = null;
	}

	void AddVariableAndReset(CollectionVariable collection, Variable variable, bool iden, bool val) {
		collection.Add(variable);
		Reset(iden, val);
	}

	void MakeIdentifier(Token<ScriptToken> token) {
		MakeIdentifier(token, false);
	}

	void MakeIdentifier(Token<ScriptToken> token, bool resetiden) {
		MakeIdentifier(token, resetiden, false);
	}

	void MakeIdentifier(Token<ScriptToken> token, bool resetiden, bool resetvalue) {
		MakeIdentifier(token, resetiden, resetvalue, false);
	}

	void MakeIdentifier(Token<ScriptToken> token, bool resetiden, bool resetvalue, bool force) {
		if (_currentiden == null || force) {
			_currentiden = new Identifier(_varname);
			AddVariableAndReset(_currentnode, _currentiden, resetiden, resetvalue);
		}
	}
	
}

public class ScriptFormatter {
	static StandardScriptParserHandler _handler = new StandardScriptParserHandler();

	public static string FormatIdentifier(Identifier iden) {
		return FormatIdentifier(iden, ValueFormat.NAME_DEFAULT, ValueFormat.ALL_DEFAULT);
	}

	public static string FormatIdentifier(Identifier iden, ValueFormat nameformat) {
		return FormatIdentifier(iden, nameformat, ValueFormat.ALL_DEFAULT);
	}

	public static string FormatIdentifier(Identifier iden, ValueFormat nameformat, ValueFormat varformat) {
		string formatted;
		FormatIdentifier(iden, out formatted, nameformat, varformat);
		return formatted;
	}

	public static bool FormatIdentifier(Identifier iden, out string result) {
		return FormatIdentifier(iden, out result, ValueFormat.NAME_DEFAULT, ValueFormat.ALL_DEFAULT);
	}

	public static bool FormatIdentifier(Identifier iden, out string result, ValueFormat nameformat) {
		return FormatIdentifier(iden, out result, nameformat, ValueFormat.ALL_DEFAULT);
	}

	public static bool FormatIdentifier(Identifier iden, out string result, ValueFormat nameformat, ValueFormat varformat) {
		if (!String.IsNullOrEmpty(iden.Name)) {
			StringBuilder builder = new StringBuilder(128);
			builder.Append(iden.GetNameFormatted(nameformat));
			foreach (ValueVariable val in iden.Children) {
				builder.Append(' ');
				builder.Append(val.GetValueFormatted(varformat));
				//builder.Append(String.Format("<{0}>", val.TypeName));
			}
			result = builder.ToString();
			return true;
		} else {
			result = String.Empty; // clear the result string
		}
		return false;
	}

	public static string FormatValue(ValueVariable val) {
		return FormatValue(val, ValueFormat.NAME_DEFAULT, ValueFormat.ALL_DEFAULT);
	}

	public static string FormatValue(ValueVariable val, ValueFormat nameformat) {
		return FormatValue(val, nameformat, ValueFormat.ALL_DEFAULT);
	}

	public static string FormatValue(ValueVariable val, ValueFormat nameformat, ValueFormat varformat) {
		string formatted;
		FormatValue(val, out formatted, nameformat, varformat);
		return formatted;
	}

	public static bool FormatValue(ValueVariable val, out string result) {
		return FormatValue(val, out result, ValueFormat.NAME_DEFAULT, ValueFormat.ALL_DEFAULT);
	}

	public static bool FormatValue(ValueVariable val, out string result, ValueFormat nameformat) {
		return FormatValue(val, out result, nameformat, ValueFormat.ALL_DEFAULT);
	}

	public static bool FormatValue(ValueVariable val, out string result, ValueFormat nameformat, ValueFormat varformat) {
		if (!String.IsNullOrEmpty(val.Name)) {
			StringBuilder builder = new StringBuilder(64);
			builder.Append(val.GetNameFormatted(nameformat));
			builder.Append("=");
			builder.Append(val.GetValueFormatted(varformat));
			//builder.Append(String.Format("<{0}>", val.TypeName));
			result = builder.ToString();
			return true;
		} else {
			result = String.Empty; // clear the result string
		}
		return false;
	}

	public static Node LoadFromFile(string path) {
		return LoadFromFile(path, Encoding.UTF8);
	}

	public static Node LoadFromFile(string path, Encoding encoding) {
		//try {
			StreamReader stream = new StreamReader(path, encoding, false);
			Node root = _handler.ProcessFromStream(stream);
			stream.Close();
			return root;
		//} catch (FileNotFoundException e) {
		//	return null;
		//}
		//return null;
	}
	
	public static Node LoadFromStream(StreamReader stream) {
		if (stream != null)
			return _handler.ProcessFromStream(stream);
		return null;
	}

	public static bool WriteToFile(Node root, string path) {
		return WriteToFile(root, path, ValueFormat.NAME_DEFAULT);
	}

	public static bool WriteToFile(Node root, string path, ValueFormat nameformat) {
		return WriteToFile(root, path, nameformat, ValueFormat.ALL_DEFAULT);
	}

	public static bool WriteToFile(Node root, string path, ValueFormat nameformat, ValueFormat varformat) {
		return WriteToFile(root, path, nameformat, varformat, Encoding.UTF8);
	}

	public static bool WriteToFile(Node root, string path, ValueFormat nameformat, ValueFormat varformat, Encoding encoding) {
		//try {
			StreamWriter stream = new StreamWriter(path, false, encoding);
			bool ret = WriteToStream(root, stream, nameformat, varformat, 0);
			stream.Close();
			return ret;
		//} catch {
		//}
		//return false;
	}

	public static bool WriteToStream(Node root, StreamWriter stream) {
		return WriteToStream(root, stream, ValueFormat.NAME_DEFAULT);
	}

	public static bool WriteToStream(Node root, StreamWriter stream, ValueFormat nameformat) {
		return WriteToStream(root, stream, nameformat, ValueFormat.ALL_DEFAULT);
	}

	public static bool WriteToStream(Node root, StreamWriter stream, ValueFormat nameformat, ValueFormat varformat) {
		return WriteToStream(root, stream, nameformat, varformat, 0);
	}

	public static bool WriteToStream(Node root, StreamWriter stream, ValueFormat nameformat, ValueFormat varformat, int tcount) {
		if (root != null && stream != null) {
			string temp;
			int tcountd = tcount;
			if (root.Parent != null) {
				if (!String.IsNullOrEmpty(root.Name)) {
					temp = root.GetNameFormatted(nameformat);
					temp += " {";
					stream.WriteLine(temp.PadLeft(temp.Length + tcount, '\t'));
				} else {
					stream.Write(new String('\t', tcount));
					stream.WriteLine('{');
				}
				tcountd++;
			} else {
				//stream.WriteLine();
			}
			bool writtenvariable = false;
			foreach (Variable variable in root.Children) {
				if (variable is ValueVariable) {
					if (FormatValue((ValueVariable)variable, out temp, nameformat, varformat)) {
						stream.WriteLine(temp.PadLeft(temp.Length + tcountd, '\t'));
						writtenvariable = true;
					}
				} else if (variable is Identifier) {
					if (FormatIdentifier((Identifier)variable, out temp, nameformat, varformat)) {
						stream.WriteLine(temp.PadLeft(temp.Length + tcountd, '\t'));
						writtenvariable = true;
					}
				} else if (variable is Node) {
					if (root.Parent == null && writtenvariable)
						stream.WriteLine(new String('\t', tcountd));
					WriteToStream((Node)variable, stream, nameformat, varformat, tcountd);
					if (root.Parent == null)
						stream.WriteLine(new String('\t', tcountd));
					writtenvariable = false;
				}
			}
			if (root.Parent != null) {
				stream.Write(new String('\t', tcount));
				stream.WriteLine('}');
			}
			return true;
		}
		return false;
	}
}

} // namespace duct
