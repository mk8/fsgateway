using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;


namespace FsGateway
{
	public class Field
	{

		private string _name="";
		private string _type="";
		
		public Field() {}
		
		public Field(string name, string type) {
			_name=name;
			_type=type;
		}
		
		public String Name {
			get {
				return _name;
			}
			set {
				_name=value;
			}
		}
		
		public String Type {
			get {
				return _type;
			}
			set {
				_type=value;
			}
		}
	}
}
