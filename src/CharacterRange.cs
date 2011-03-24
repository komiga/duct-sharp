
using System;
using System.Collections.Generic;

namespace duct {

public class CharacterRange {
	char _start;
	public char Start {
		set { _start=value; }
		get { return _start; }
	}

	char _end;
	public char End {
		set { _end=value; }
		get { return _end; }
	}

	public CharacterRange(char start, uint length) {
		_start=start;
		_end=Convert.ToChar(_start+length);
	}

	public CharacterRange(char start, char end) {
		_start=start;
		if (end<_start)
			throw new Exception("End of range must be lower than or equal to the start");
		_end=end;
	}

	public CharacterRange(char start) {
		_start=start;
		_end=start;
	}

	public bool Contains(char c) {
		return (_start==c || (c>=_start && c<=_end));
	}

	public int FindInString(string str) {
		return FindInString(str, 0);
	}

	public int FindInString(string str, uint startfrom) {
		if (startfrom>=str.Length)
			return -1;
		for (int i=(int)startfrom; (uint)i<str.Length; ++i) {
			if (Contains(str[i])) {
				return i;
			}
		}
		return -1;
	}

	public int FindLastInString(string str) {
		return FindLastInString(str, -1);
	}

	public int FindLastInString(string str, int startfrom) {
		if (startfrom==-1)
			startfrom=str.Length-1;
		for (int i=startfrom; i>-1; --i) {
			if (Contains(str[i])) {
				return i;
			}
		}
		return -1;
	}

	public int Compare(CharacterRange other) {
		int sd=_end-_start;
		int od=other._end-other._start;
		if (sd<od) {
			return -1;
		} else if (sd>od) {
			return 1;
		}
		if (_start<other._start) {
			return -1;
		} else if (_start>other._start) {
			return 1;
		}
		return 0;
	}

	public bool Intersects(CharacterRange other) {
		if (Compare(other)==0)
			return true;
		if (_end==(other._start-1))
			return true;
		else if ((_start-1)==other._end)
			return true;
		return !(_start>other._end || _end<other._start);
	}
}

public class CharacterSet {
	List<CharacterRange> _ranges=new List<CharacterRange>();

	public CharacterSet() {
	}

	public CharacterSet(string str) {
		AddRangesWithString(str);
	}

	public CharacterSet(char start, uint length) {
		AddRange(start, length);
	}

	public CharacterSet(char c) {
		AddRange(c);
	}

	public CharacterSet(CharacterRange range) {
		AddRange(range);
	}

	public bool Contains(char c) {
		foreach (CharacterRange range in _ranges) {
			if (range.Contains(c))
				return true;
		}
		return false;
	}

	public bool Contains(CharacterRange other) {
		foreach (CharacterRange range in _ranges) {
			if (range==other || range.Compare(other)==0)
				return true;
		}
		return false;
	}

	public int FindInString(string str) {
		return FindInString(str, 0);
	}

	public int FindInString(string str, uint startfrom) {
		if (startfrom>=str.Length)
			return -1;
		int i;
		foreach (CharacterRange range in _ranges) {
			i=range.FindInString(str, startfrom);
			if (i!=-1)
				return i;
		}
		return -1;
	}

	public int FindLastInString(string str) {
		return FindLastInString(str, -1);
	}

	public int FindLastInString(string str, int startfrom) {
		if (startfrom==-1)
			startfrom=str.Length-1;
		int result=-1, i;
		foreach (CharacterRange range in _ranges) {
			i=range.FindLastInString(str, startfrom);
			if (i!=-1 && (i>result || result==-1))
				result=i;
		}
		return result;
	}

	public void Clear() {
		_ranges.Clear();
	}

	public void AddRange(char start, uint length) {
		_ranges.Add(new CharacterRange(start, length));
	}

	public void AddRange(char c) {
		_ranges.Add(new CharacterRange(c));
	}

	public void AddRange(CharacterRange range) {
		_ranges.Add(range);
	}

	public void AddRangesWithString(string str) {
		const char CHAR_DASH='-';
		const char CHAR_ESCAPE='\\';
		char lastchar='\xFFFF', chr;
		bool isrange=false, escape=false;
		for (int i=0; i<str.Length; ++i) {
			chr=str[i];
			if (escape) {
				escape=false;
			} else if (chr==CHAR_ESCAPE) {
				escape=true;
				continue;
			} else if (lastchar!=0xFFFF && chr==CHAR_DASH && !isrange) {
				isrange=true;
				continue;
			}
			if (lastchar!=0xFFFF) {
				if (isrange) {
					if (chr==lastchar)
						AddRange(chr);
					else if (chr<lastchar)
						AddRange(chr, (uint)(lastchar-chr));
					else
						AddRange(lastchar, (uint)(chr-lastchar));
					lastchar='\xFFFF';
					isrange=false;
				} else {
					AddRange(lastchar);
					lastchar=chr;
				}
			} else {
				lastchar=chr;
			}
		}
		if (lastchar!='\xFFFF') {
			if (isrange)
				throw new Exception(String.Format("Invalid range in string: \"{0}\"", str));
			AddRange(lastchar);
		}
	}

	public CharacterSet InitWithWhitespace() {
		AddRange('\t', 1); // \t and \n
		AddRange('\r', 0);
		AddRange(' ', 0);
		return this;
	}
	
	public CharacterSet InitWithAlphanumeric() {
		AddRange('A', 26);
		AddRange('a', 26);
		AddRange('0', 10);
		return this;
	}

	public CharacterSet InitWithLetters() {
		AddRange('A', 26);
		AddRange('a', 26);
		return this;
	}
	
	public CharacterSet InitWithUppercaseLetters() {
		AddRange('A', 26);
		return this;
	}

	public CharacterSet InitWithLowercaseLetters() {
		AddRange('a', 26);
		return this;
	}
	
	public CharacterSet InitWithNumbers() {
		AddRange('0', 10);
		return this;
	}

	public CharacterSet InitWithNewline() {
		AddRange('\n', 0);
		return this;
	}
}

} // namespace duct