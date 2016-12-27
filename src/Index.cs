/* 
 * FsGateway - navigate a database structure as directory tree
 * Copyright (C) 2009-2016 Torello Querci <tquerci@gmail.com>
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
//using Npgsql;

namespace FsGateway
{
	public class Index
	{
		private Byte[] _buffer=null;
		
		public Index()
		{
		}
		
		public Index(string schema, string name, string script) {
			Schema=schema;
			TableName="";
			Name=name;
			Script=script;
		}
			
		public Index(string schema, string tablename, string name, string script) {
			Schema=schema;
			TableName=tablename;
			Name=name;
			Script=script;
		}
		
		public string Schema { get; private set; }
		public string TableName { get; private set; }
		public string Name { get; private set; }
		public string Script { get; set; }

		public Byte[] Buffer {
			get {
				if (_buffer == null) {
					System.Text.UTF8Encoding encoder=new System.Text.UTF8Encoding();
					_buffer=encoder.GetBytes(Script);
				}
				return _buffer;
			}
		}
				
		public override string ToString() {
			return (Schema != null ? Schema+"." : "")+Name;
		}
	}
}
