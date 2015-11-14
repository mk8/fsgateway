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
				if (_buffer ==null) {
					System.Text.UTF8Encoding encoder=new System.Text.UTF8Encoding();
					_buffer=encoder.GetBytes(_script);
				}
				return _buffer;
			}
		}
				
		public override string ToString() {
			return (_schema != null ? _schema+"." : "")+_name;
		}
	}
}
