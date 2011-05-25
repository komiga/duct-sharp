
using System;
using System.IO;
using System.Collections;
using System.Text;

public class MyArchive : duct.Archive {
	private ArrayList _list=new ArrayList();
	public ArrayList List {
		get { return _list; }
	}
	
	public override string Identifier {
		get { return "TEST"; }
	}
	
	public override ulong MetadataSize {
		get { return base.MetadataSize+4; } // +entry_count
	}
	
	public override ulong HeaderSize {
		get {
			ulong size=MetadataSize;
			foreach (MyEntry e in _list) {
				size+=e.MetadataSize;
			}
			return size;
		}
	}
	
	public override uint Count {
		get { return (uint)_list.Count; }
	}
	
	public MyArchive(string path) : base(path) {
	}
	
	public override void Clear() {
		_list.Clear();
	}
	
	public override bool DeserializeUserspace(BinaryReader stream) {
		// no special data
		uint entrycount=stream.ReadUInt32();
		for (uint i=0; i<entrycount; ++i) {
			MyEntry e=new MyEntry();
			e.Deserialize(stream);
			_list.Add(e);
		}
		return true;
	}
	
	public override bool SerializeUserspace(BinaryWriter stream) {
		// no special data
		stream.Write(Count);
		foreach (MyEntry e in _list) {
			e.Serialize(stream);
		}
		return true;
	}
	
	public override bool ReadEntries() {
		foreach (MyEntry e in _list) {
			e.Read(_stream); // we don't need to seek to the dataoffset - Entry.Read is supposed to
		}
		return true;
	}
	
	public override bool WriteEntries() {
		_stream.Seek((long)HeaderSize, SeekOrigin.Begin);
		foreach (MyEntry e in _list) {
			e.Write(_stream);
		}
		return true;
	}
	
	public void Add(MyEntry e) {
		_list.Add(e);
	}
}

// This'll handle its own data
public class MyEntry : duct.Entry {
	private byte[] _data;
	public byte[] Data {
		get { return _data; }
	}
	
	private string _path;
	public string Path {
		get { return _path; }
	}
	
	public override uint MetadataSize {
		get {
			return base.MetadataSize+(uint)Encoding.UTF8.GetByteCount(_path)+2u;
		}
	}
	
	public MyEntry() {
	}
	
	public MyEntry(string path) {
		Load(path);
	}
	
	public override Stream Open(Stream stream) {
		throw new NotImplementedException();
	}
	
	public override void Close() {
		throw new NotImplementedException();
	}
	
	public override bool DeserializeUserspace(BinaryReader stream) {
		ushort len=stream.ReadUInt16();
		byte[] buf=stream.ReadBytes(len);
		_path=Encoding.UTF8.GetString(buf);
		Console.WriteLine("{0}", _path);
		return true;
	}
	
	public override bool SerializeUserspace(BinaryWriter stream) {
		byte[] buf=Encoding.UTF8.GetBytes(_path);
		stream.Write((ushort)buf.Length);
		stream.Write(buf);
		return true;
	}
	
	public override bool Read(Stream stream) {
		stream.Seek((long)_dataoffset, SeekOrigin.Begin);
		_data=new byte[_datasize];
		stream.Read(_data, 0, (int)_datasize);
		return true;
	}
	
	public override bool Write(Stream stream) {
		_dataoffset=(ulong)stream.Position;
		if (_data!=null) {
			stream.Write(_data, 0, _data.Length);
		}
		return true;
	}
	
	// load the entry's data from a file
	public void Load(string path) {
		_path=path;
		using (BinaryReader bs=new BinaryReader(new FileStream(_path, FileMode.Open, FileAccess.Read))) {
			_datasize=(uint)bs.BaseStream.Length;
			_data=bs.ReadBytes((int)_datasize);
		}
	}
	
	public void Save() {
		using (BinaryWriter bs=new BinaryWriter(new FileStream(_path+".out", FileMode.Create, FileAccess.Write))) {
			if (_data!=null) {
				bs.Write(_data);
			}
		}
	}
}

public class MainClass {
	public static void Main(string[] args) {
		// create an archive
		MyArchive archout=new MyArchive("data/test.arc");
		archout.Add(new MyEntry("data/test.txt"));
		if (!archout.Save()) {
			Console.WriteLine("Failed to write {0}", archout.Path);
			return;
		}
		// read it in
		MyArchive archin=new MyArchive("data/test.arc");
		if (!archin.Open(true, true, false)) {
			Console.WriteLine("Failed to read {0}", archin.Path);
			return;
		}
		if (!archin.ReadEntries()) {
			Console.WriteLine("Failed to read entry data");
			return;
		}
		foreach (MyEntry e in archin.List) {
			Console.WriteLine("\"{0}\" offset={1} size={2} metadatasize={3}", e.Path, e.DataOffset, e.DataSize, e.MetadataSize);
			e.Save();
		}
	}
} // class MainClass

