using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;

namespace FsGateway
{
	public class View
	{
		private string _schema="";
		private string _name="";
		private string _script="";
		private Byte[] _buffer=null;
		
		public View()
		{
		}
		
		public View(string schema, string name, string script) {
			_schema=schema;
			_name=name;
			_script=script;
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
			return (_schema != null ? _schema + "." : "")+_name;
		}
	}
}
