/* 
 * FsGateway - navigate a database structure as directory tree
 * Copyright (C) 2009-2015 Torello Querci <torello@torosoft.com>
 * 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

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
