using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;

namespace FsGateway
{
	public class Index
	{
		private string _schema="";
		private string _tablename="";
		private string _name="";
		private string _script="";
		private Byte[] _buffer=null;
		
		public Index()
		{
		}
		
		public Index(string schema, string name, string script) {
			_schema=schema;
			_tablename="";
			_name=name;
			_script=script;
		}
			
		public Index(string schema, string tablename, string name, string script) {
			_schema=schema;
			_tablename=tablename;
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
		
		public string TableName {
			get {
				return _tablename;
			}
			set {
				_tablename=value;
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
			return _schema+"."+_name;
		}
	}
}
