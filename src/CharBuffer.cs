
using System;

namespace duct {
/*public class CharBuffer {
	char[] _buffer;
	uint _size, _length;
	string _cachestring;
	bool _cached;

	public CharBuffer() : this(0) {
	}

	public CharBuffer(uint size) {
		_buffer=new char[size];
		_size=size;
		_length=0;
		_cached=false;
	}

	public void Reset() {
		_length=0;
		_cached=false;
	}

	public void Add(char c) {
		const uint BUFFER_INITIAL_SIZE=68;
		const double BUFFER_MULTIPLIER=1.75;
		if (_buffer==null) {
			_size=BUFFER_INITIAL_SIZE;
			_buffer=new char[_size];
			_length=0;
		} else if (_length>=_size) {
			uint newsize=Math.Ceiling(_size*BUFFER_MULTIPLIER);
			if (newsize<_length)
				newsize=Math.Ceiling(_length*BUFFER_MULTIPLIER);
			_size=newsize;
			Array.Resize<char>(ref _buffer, newsize);
		}
		_buffer[_length++]=c;
		_cached=false;
	}

	public string CacheString() {
		if (!_cached)
			_cachestring=new string(_buffer, _length);
		return _cachestring;
	}

}*/
}
