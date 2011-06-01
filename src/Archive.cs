
using System;
using System.IO;
using System.Text;

namespace duct {

public abstract class Archive {
	public abstract string Identifier {get;}
	
	protected FileStream _stream;
	public Stream Stream {
		get { return (Stream)_stream; }
	}
	
	protected string _path;
	public string Path {
		get { return _path; }
		set { _path=value; }
	}
	
	public bool Opened {
		get { return _readable || _writeable; }
	}
	
	protected bool _writeable;
	public bool Writeable {
		get { return _writeable; }
	}
	
	protected bool _readable;
	public bool Readable {
		get { return _readable; }
	}
	
	public virtual ulong MetadataSize {
		get { return 4u; }
	}
	
	public abstract ulong HeaderSize {get;}
	public abstract uint Count {get;}
	
	public Archive(string path) {
		_path=path;
	}
	
	public virtual bool Open(bool deserialize=true, bool readable=true, bool writeable=false) {
		Close();
		if ((!readable && !writeable) || String.IsNullOrEmpty(Path)) {
			return false;
		}
		FileMode mode=(readable && writeable) ? FileMode.OpenOrCreate : (readable ? FileMode.Open : FileMode.Create);
		FileAccess access=(readable && writeable) ? FileAccess.ReadWrite : (readable ? FileAccess.Read : FileAccess.Write);
		try {
			_stream=new FileStream(Path, mode, access);
		} catch {
			return false;
		}
		_readable=readable;
		_writeable=writeable;
		if (deserialize) {
			if (!Deserialize()) {
				return false;
			}
		}
		return true;
	}
	
	public virtual void Close() {
		if (Opened && _stream!=null) {
			_stream.Close();
			_stream=null;
		}
		_readable=false;
		_writeable=false;
	}
	
	public virtual bool Save() {
		if (!_readable && _writeable) {
			return Save(true);
		} else if (Opened) {
			bool readable=_readable, writeable=_writeable;
			bool rc=Save(false);
			if (rc) {
				return Open(false, readable, writeable);
			}
		} else {
			return Save(false);
		}
		return false;
	}
	
	public virtual bool Save(bool keepopen) {
		if (!Open(false, false, true)) { // reopen with write-access
			return false;
		}
		if (!WriteEntries()) {
			return false;
		}
		if (!Serialize()) {
			return false;
		}
		if (!keepopen) {
			Close();
		}
		return true;
	}
	
	public abstract void Clear();
	
	public virtual bool Deserialize() {
		if (_readable) {
			Clear();
			_stream.Seek(0, SeekOrigin.Begin);
			if (_stream.Length<4) { // lowest possible size for a header
				return false;
			}
			byte[] check=new byte[4];
			_stream.Read(check, 0, 4);
			string a=new String(Encoding.ASCII.GetChars(check));
			if (a.CompareTo(Identifier)!=0) {
				return false;
			}
			BinaryReader bs=new BinaryReader(_stream); // DON'T KILL, DON'T KILL, DON'T KILL
			if (!DeserializeUserspace(bs)) {
				return false;
			}
			return true;
		}
		return false;
	}
	
	public virtual bool Serialize() {
		if (_writeable) {
			_stream.Seek(0, SeekOrigin.Begin);
			byte[] iden=new byte[4];
			Encoding.ASCII.GetBytes(Identifier, 0, 4, iden, 0);
			_stream.Write(iden, 0, 4);
			BinaryWriter bs=new BinaryWriter(_stream);
			if (!SerializeUserspace(bs)) {
				return false;
			}
			return true;
		}
		return false;
	}
	
	public abstract bool DeserializeUserspace(BinaryReader stream);
	public abstract bool SerializeUserspace(BinaryWriter stream);
	public abstract bool ReadEntries();
	public abstract bool WriteEntries();
} // class Archive


[Flags]
public enum EntryFlag : ushort {
	NONE=0x00,
	COMPRESSED=0x01,
	_RESERVED0=0x02,
	_RESERVED1=0x04
}

public abstract class Entry {
	protected bool _opened;
	public bool Opened {
		get { return _opened; }
	}
	
	protected EntryFlag _flags;
	public EntryFlag Flags {
		get { return _flags; }
		set { _flags=value; }
	}
	
	public bool Compressed {
		get { return (_flags&EntryFlag.COMPRESSED)!=0; }
		set {
			if (value)
				_flags|=EntryFlag.COMPRESSED;
			else
				_flags&=~EntryFlag.COMPRESSED;
		}
	}
	
	protected ulong _dataoffset;
	public ulong DataOffset {
		get { return _dataoffset; }
	}
	
	protected uint _datasize;
	public uint DataSize {
		get { return _datasize; }
	}
	
	public static uint ConstMetadataSize {
		get { return 14u; }
	}	
	
	public virtual uint MetadataSize {
		get { return 14u; }
	}
	
	public abstract Stream Open(Stream stream);
	public abstract void Close();
	
	public virtual bool Deserialize(BinaryReader stream) {
		_flags=(EntryFlag)stream.ReadUInt16();
		_dataoffset=stream.ReadUInt64();
		_datasize=stream.ReadUInt32();
		return DeserializeUserspace(stream);
	}
	
	public virtual bool Serialize(BinaryWriter stream) {
		stream.Write((UInt16)_flags);
		stream.Write(_dataoffset);
		stream.Write(_datasize);
		return SerializeUserspace(stream);
	}
	
	public abstract bool DeserializeUserspace(BinaryReader stream);
	public abstract bool SerializeUserspace(BinaryWriter stream);
	public abstract bool Read(Stream stream);
	public abstract bool Write(Stream stream);
	
} // class Entry

} // namespace duct

