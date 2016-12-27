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

namespace fsgateway
{
	public class Constraint
	{
		public Constraint ()
		{
			FieldMapping = new List<KeyValuePair<string,string>> ();
		}

		public Constraint (string schema, string name, string tableName, string targetTableName) {
			Schema = schema;
			Name = name;
			TableName = tableName;
			TargetTableName = targetTableName;
			FieldMapping = new List<KeyValuePair<string,string>> ();
		}

		private Byte[] buffer=null;

		public string Schema { get; set; }
		public string Name { get; set; }
		public string TableName { get; set; }
		public string TargetTableName { get; set; }

		public List<KeyValuePair<string, string>> FieldMapping { get; set; }

		public Byte[] Buffer {
			get {
				if (buffer == null) {

					string toString = "ALTER TABLE " + (Schema != null ? Schema + "." : "") + TableName
						+ " ADD CONSTRAINT " + Name
						+ " FOREIGN KEY ";
					string srcMap = "",
						   trgMap = "", 
						   separator = "";
					foreach (KeyValuePair<string, string> map in FieldMapping) {
						srcMap += separator + map.Key;
						trgMap += separator + map.Value;
						separator = ", ";
					}

					toString += "(" + srcMap + ") REFERENCES " + TargetTableName + "(" + trgMap + ")\n";



					System.Text.UTF8Encoding encoder=new System.Text.UTF8Encoding();
					buffer=encoder.GetBytes(toString);
				}

				return buffer;
			}
		}

		public override string ToString() {
			return (Schema != null ? Schema+"." : "") + TableName + "." + Name;
		}
	}
}
