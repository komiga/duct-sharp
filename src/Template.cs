
using System;
using System.Collections.Generic;

namespace duct {

public class Template {
	string[] _iden;
	public string[] Identity {
		set { _iden=value; }
		get { return _iden; }
	}

	VariableType[] _layout;
	public VariableType[] Layout {
		set { _layout=value; }
		get { return _layout; }
	}

	VariableType _infinitism;
	public VariableType Infinitism {
		set { _infinitism=value; }
		get { return _infinitism; }
	}

	bool _casesens;
	public bool CaseSensitive {
		set { _casesens=value; }
		get { return _casesens; }
	}

	public Template() : this(null, null) {
	}

	public Template(string[] iden, VariableType[] layout) : this(iden, layout, false) {
	}

	public Template(string[] iden, VariableType[] layout, bool casesens) : this(iden, layout, casesens, VariableType.NONE) {
	}

	public Template(string[] iden, VariableType[] layout, bool casesens, VariableType infinitism) {
		_iden=iden;
		_layout=layout;
		_casesens=casesens;
		_infinitism=infinitism;
	}

	bool _checkVariable(VariableType type, Variable variable) {
		if (variable!=null && type!=VariableType.NONE)
			return (type&variable.Type)!=0;
		return false;
	}

	bool _checkIden(string[] iden, string a, bool casesens) {
		return _checkIden(iden, a, casesens, 0);
	}

	bool _checkIden(string[] iden, string a, bool casesens, int i) {
		//printf("Template._checkIden iden:%p\n", (void*)iden);
		if (iden!=null) {
			//printf("Template._checkIden count:%d i:%d\n", iden->count, i);
			string b;
			for (; i<iden.Length; ++i) {
				b=iden[i];
				//printf("Template._checkIden b:%p\n", (void*)b);
				//debug_assert(b, "null identity element");
				if (String.Compare(a, b, !casesens)==0) {
					return true;
				}
			}
			return false;
		} else {
			return true; // NULL identity matches any name
		}
	}
	
	bool _compareNames(string a, string b, bool casesens) {
		return String.Compare(a, b, !casesens)==0;
	}
	
	bool __matchname(int mc, string[] iden, string name, bool casesens, VariableType infinitism) {
		if (iden.Length==0)
			return true;
		else if (mc<iden.Length)
			return _compareNames(iden[mc], name, casesens);
		else if (mc>=iden.Length && (infinitism!=VariableType.NONE))
			return true;
		return false;
	}

	bool __matchvariable(int mc, VariableType[] layout, VariableType infinitism, Variable variable) {
		if (layout!=null && (mc<layout.Length))
			return (layout[mc]&variable.Type)!=0;
		else if (infinitism!=VariableType.NONE)
			return (infinitism&variable.Type)!=0;
		return false;
	}

	struct _IndexVariablePair {
		public int i;
		public Variable variable;

		public _IndexVariablePair(int index, Variable v) {
			i=index;
			variable=v;
		}
	}

	void __add(CollectionVariable collection, List<_IndexVariablePair> pairs, string name, ref int addcount) {
		if (pairs.Count>0) {
			bool first=true;
			Identifier iden=new Identifier(name, collection);
			foreach (_IndexVariablePair pair in pairs) {
				collection.Children.RemoveAt(pair.i);
				if (first) {
					collection.Children.Insert(pair.i, iden);
					first=false;
				}
				iden.Add(pair.variable); // re-own to the new identifier
			}
			addcount++;
		}
	}

	void __reset(out int mc, out bool repeatmatch, bool rep, List<_IndexVariablePair> pairs) {
		mc=0;
		repeatmatch=rep;
		pairs.Clear();
	}

	public bool Validate(Identifier identifier) {
		return ValidateIdentifier(identifier);
	}

	public bool ValidateIdentifier(Identifier identifier) {
		if (identifier!=null) {
			// TODO: _layout==null ?
			if (!(identifier.Count>_layout.Length && (_infinitism==VariableType.NONE)) && !(identifier.Count<_layout.Length) && _checkIden(_iden, identifier.Name, _casesens)) {
				// Compare defined variables in the identifier
				int i=0;
				foreach (Variable variable in identifier.Children.GetRange(0, _layout.Length)) {
					if ((_layout[i]&variable.Type)==0)
						return false;
					i++;
				}
				// Check infinitism
				if (identifier.Count>_layout.Length && _infinitism!=VariableType.NONE) {
					i=_layout.Length;
					foreach (Variable variable in identifier.Children.GetRange(_layout.Length, identifier.Count-_layout.Length)) {
						if (!_checkVariable(_infinitism, variable))
							return false;
					}
					i++;
				}
				return true; // Identifier passed all tests
			}
		}
		return false;
	}

	public bool Validate(ValueVariable val) {
		return ValidateValue(val);
	}

	public bool ValidateValue(ValueVariable val) {
		if (val!=null) {
			if (_checkIden(_iden, val.Name, _casesens)) {
				if (_layout!=null && _layout.Length>0)
					return (_layout[0]&val.Type)!=0;
				// No canon types, check infinitism
				if (_infinitism!=VariableType.NONE)
					return (_infinitism&val.Type)!=0;
			}
		}
		return false;
	}

	public int CompactCollection(CollectionVariable collection, string name) {
		return CompactCollection(collection, name, false);
	}

	public int CompactCollection(CollectionVariable collection, string name, bool sequential) {
		int addcount=0;
		if (collection!=null && collection.Count>0) {
			List<_IndexVariablePair> pairs=new List<_IndexVariablePair>();
			bool matched; //, namematched, varmatched;
			int mc=0;
			int mmax=(_iden!=null) ? _iden.Length : 0;
			mmax=(((_layout!=null) ? _layout.Length : 0)>mmax) ? _layout.Length : mmax;
			//Console.WriteLine("(Template.CompactCollection) mmax:{0}", mmax);
			ValueVariable val=null;
			bool repeatmatch=false;
			int i=0;
			Variable[] array=collection.Children.ToArray();
			while (i<array.Length) {
				//Console.WriteLine("(Template.CompactCollection) [{0}] repeatmatch:{0}", mc, repeatmatch);
				if (repeatmatch) {
					repeatmatch=false;
				} else {
					val=(array[i] is ValueVariable) ? (ValueVariable)array[i] : null;
					i++;
				}
				if (val!=null) {
					//namematched=__matchname(mc, _iden, value.Name, _casesens, _infinitism);
					//varmatched=__matchvariable(mc, _layout, _infinitism, value);
					//matched=namematched && varmatched;
					matched=__matchname(mc, _iden, val.Name, _casesens, _infinitism) && __matchvariable(mc, _layout, _infinitism, val);
					//Console.WriteLine("(Template.CompactCollection) [{0}] type:{1}, name:{2}, namematched:{3}, varmatched:{4}, (mc<mmax):{5}, (_layout && mc<_layout.Length):{6}",
					//	mc, value.TypeName, "" /*value.Name*/, namematched, varmatched, (mc<mmax), (_layout && mc<_layout.Length));
					if (matched) {
						//Console.WriteLine("(Template.CompactCollection) match at mc:{0}", mc);
						pairs.Add(new _IndexVariablePair(i, val));
						mc++;
					} else if (mc>0 && mc!=mmax) {
						//Console.WriteLine("(Template.CompactCollection) unmatched before total; mc:{0}", mc);
						if (mmax==0 || mc>mmax) {
							//Console.WriteLine("(Template.CompactCollection) creating from left over");
							__add(collection, pairs, name, ref addcount);
						}
						__reset(out mc, out repeatmatch, true, pairs);
					}
					if (mmax>0 && ((mc==mmax && (_infinitism==VariableType.NONE)) || (mc>0 && !(i!=array.Length)))) {
						//Console.WriteLine("(Template.CompactCollection) unmatched total mc:{0}, creating identifier", mc);
						__add(collection, pairs, name, ref addcount);
						__reset(out mc, out repeatmatch, matched, pairs);
					}
				} else if (mc>0 && sequential) {
					//Console.WriteLine("(Template.CompactCollection) non-value inbetween match series mc:{0}", mc);
					if (mmax==0 || mc>mmax) {
						//Console.WriteLine("(Template.CompactCollection) creating from left over");
						__add(collection, pairs, name, ref addcount);
					}
					__reset(out mc, out repeatmatch, false, pairs);
				}
			}
		}
		return addcount;
	}

	public int RenameIdentifiers(CollectionVariable collection, string name) {
		int count=0;
		if (collection!=null) {
			foreach (Identifier identifier in collection.Children) {
				if (ValidateIdentifier(identifier)) {
					identifier.Name=name;
					count++;
				}
			}
		}
		return count;
	}

	public int RenameValues(CollectionVariable collection, string name) {
		int count=0;
		if (collection!=null) {
			foreach (ValueVariable val in collection.Children) {
				if (ValidateValue(val)) {
					val.Name=name;
					count++;
				}
			}
		}
		return count;
	}

	public Identifier GetMatchingIdentifier(CollectionVariable collection) {
		if (collection!=null) {
			foreach (Identifier identifier in collection.Children) {
				if (ValidateIdentifier(identifier))
					return identifier;
			}
		}
		return null;
	}

	public ValueVariable GetMatchingValue(CollectionVariable collection) {
		if (collection!=null) {
			foreach (ValueVariable val in collection.Children) {
				if (ValidateValue(val))
					return val;
			}
		}
		return null;
	}
}

} // namespace duct
