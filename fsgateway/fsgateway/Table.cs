using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;

namespace FsGateway
{
	
	public class Table
	{
		private Int64 _id=-1; 
		private string _schema="";
		private string _name="";
		private string _script="";
		private Byte[] _buffer=null;
		private SortedList <string, Field> _fieldList=new SortedList<string,Field>();

		public Table()
		{
		}
		
		public Table (string schema, string name) {
			_schema=schema;
			_name=name;
		}
		
		public Table (Int64 id, string schema, string name) {
			_id=id;
			_schema=schema;
			_name=name;
		}

		public Int64 Id {
			get {
				return _id;
			}
			set {
				_id=value;
			}
		}
		
		public string Schema {
			get {
				return _schema;
			}
			set {
				_schema=value;
			}
		}
		
		public string Name {
			get {
				return _name;
			}
			set {
				_name=value;
			}
		}

		public SortedList<string,Field> Fields {
			get {
				return _fieldList;
			}
		}
		
		public string Script {
			get {
				return _script;
			}
			set {
				_script=value;

				System.Text.UTF8Encoding encoder=new System.Text.UTF8Encoding();
				_buffer=encoder.GetBytes(_script);
			}
		}

		public Byte[] Buffer {
			get {
				if (_buffer==null) {
					System.Text.UTF8Encoding encoder=new System.Text.UTF8Encoding();
					_buffer=encoder.GetBytes(_script);
				}
				return _buffer;
			}
		}
				
		public override string ToString() {
			return (_schema != null ? _schema+"." : "" )+_name;
		}

	}
}
