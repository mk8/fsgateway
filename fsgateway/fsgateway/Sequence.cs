/* 
 * FsGateway - navigate a database structure as directory tree
 * Copyright (C) 2009-2010 Torello Querci <torello@torosoft.com>
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
	
	
	public class Sequence
	{
		private string _schema="";
		private string _name="";
		private Int64 _last_value;
		private Int64 _increment_by;
		private Int64 _max_value;
		private Int64 _min_value;
		private Int64 _cache_value;
		private bool _is_cycled;
		private bool _is_called;
		private string _script="";
		private Byte[] _buffer=null;
		
		public Sequence()
		{
		}
		
		public Sequence(string schema, string name) {
			_schema=schema;
			_name=name;
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

		public Int64 LastValue {
			get {
				return _last_value;
			}
			set {
				_last_value=value;
			}
		}
		
		public Int64 IncrementBy {
			get {
				return _increment_by;
			}
			set {
				_increment_by=value;
			}
		}
		
		public Int64 MaxValue {
			get {
				return _max_value;
			}
			set {
				_max_value=value;
			}
		}
		
		public Int64 MinValue {
			get {
				return _min_value;
			}
			set {
				_min_value=value;
			}
		}

		public Int64 CacheValue {
			get {
				return _cache_value;
			}
			set {
				_cache_value=value;
			}
		}
		
		public bool IsCycled {
			get {
				return _is_cycled;
			}
			set {
				_is_cycled=value;
			}
		}
		
		public bool IsCalled {
			get {
				return _is_called;
			}
			set {
				_is_called=value;
			}
		}

		public string Script {
			get {
				if (_script==null) {
					/******/
				}
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
