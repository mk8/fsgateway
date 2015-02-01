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
using MySql;
using Mono.Fuse;
using System.Text.RegularExpressions;

namespace FsGateway
{
	
	public class DB_MySql : IFsDb
	{
		private IDbConnection dbcon=null;
		private string connectionString=null;
		private bool _isConnected=false;
		private int version_number_release=0; // e.s. 8.x.x
		private int version_number_major=0; // e.s. x.3.0
		private int version_number_minor=0; // e.s. x.x.0
		
		public DB_MySql()
		{
		}

		public DB_MySql(string host, string database, string user, string password, string port)
		{

			string connectionString = "Server="+host+";" +
				"Database="+database+";" +
				"User ID="+user+";" +
				"Password="+password+";"+
				"Port="+port+";";
			Connect(connectionString);
		}

		public List<string> getTypeOfObjects() {
			List<string> names=new List<string>();
			
			names.Add ("/tables");
		    names.Add ("/views");
		    names.Add ("/indexes");
			
			return names;
		}


		public void Dispose() {
			Unconnect();
			_isConnected=false;
		}
		
		public string storageType {
			get {
				return "DB_MySQL5";
			}
		}

		public string Usage {
			get {
				return "Specify the connection parameter like this one: \"Server=localhost; Database=mydb; User ID=username;Password=password;Port=3306;";
			}
		}
		
		public bool isConnect {
			get {
				return _isConnected;
			}
		}
		
		public bool Connect() {
			return Connect(this.connectionString);
		}
		
		public bool Connect(string connectionString) {
			bool res=false;
			
			if (dbcon!=null && dbcon.State==System.Data.ConnectionState.Open) {
				Unconnect();
			}
			
			try {
				dbcon = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
				dbcon.Open();
				this.connectionString=connectionString;
				res=true;
				_isConnected=true;
				
//				readVersionNumber();
				
			} catch (Exception ex) {
				System.Console.Out.WriteLine("Exception during database connection opening. Error message: "+ex.Message);
				dbcon=null;
			}
			return res;
		}
		
/*
		private string readVersionNumber() {
			string sql="select version()";
			string version="";
			
			IDbCommand dbcmd = dbcon.CreateCommand();
			
			dbcmd.CommandText = sql;
			IDataReader reader = dbcmd.ExecuteReader();
			if (!reader.Read()) {
				reader.Close();
				return null;
			}

			// Get the version number
			version=reader.GetString(0);

			// Parse the version number string to get the single part
			Regex fileMultipleRegex = null;
			
			try {
				fileMultipleRegex = new Regex(@"(?<postgres>[^ ]+) ((?<numberVersion>[0-9]+)\.(?<numberMajor>[0-9]+)\.(?<numberMinor>[0-9]+))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			} catch (Exception ex) {
				System.Console.WriteLine("Exception during the Regex creation: "+ex.Message);
			}
			
			// Check for namefile with number
			int index=0;
			if (fileMultipleRegex!=null) {

				Match match=null;
				try {
					match = fileMultipleRegex.Match(version);
				} catch (Exception ex) {
					System.Console.WriteLine("Exception during match routine. Message: "+ex.Message);
				}

				try {
					if (match != null && match.Success) {					
						version_number_release =System.Int32.Parse(match.Groups["numberVersion"].Value);
						version_number_major =System.Int32.Parse(match.Groups["numberMajor"].Value);
						version_number_minor =System.Int32.Parse(match.Groups["numberMinor"].Value);
					}
				} catch (Exception ex) {
					Console.WriteLine("Exception: "+ex.Message);
				}
			}
			
			reader.Close();
			return version;
		}
*/
		public void Unconnect() {
			if (dbcon!=null && dbcon.State==System.Data.ConnectionState.Open) {
				dbcon.Close();
				dbcon=null;
			}
			
			_isConnected=true;
			return;
		}
		
		public SortedList<string,Table> getTables() {

			SortedList<string,Table> tableList=null;
			string sql;
			IDataReader reader=null;

			// Check for DB Connection
			if (dbcon!=null) {

				tableList=new SortedList<string,Table>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				sql = "SHOW TABLES"
				      ;
				
				try {
					dbcmd.CommandText = sql;
					reader = dbcmd.ExecuteReader();
					while(reader.Read()) {
						Table table=new Table(null,reader.GetString(0));
						tableList.Add(table.ToString(), table);
					}
				} catch (Exception ex) {
					Console.WriteLine("Exception reading of the tables list : "+ex.Message);
					Console.WriteLine("Postgresql Version: "+version_number_release+"."+version_number_major+"."+version_number_minor);
					Console.WriteLine("List tables: SQL=" + sql);
				}
				reader.Close();				

				// ReRead data for fetching detail
				foreach (Table table in tableList.Values) {
					   
					sql = "DESCRIBE "+table.Name;
					string listFilesStr="";
					try {
						dbcmd.CommandText = sql;
						reader = dbcmd.ExecuteReader();
						listFilesStr="";
						while (reader.Read()) {
							Field field=new Field(reader.GetString(reader.GetOrdinal("Field")),reader.GetString(reader.GetOrdinal("Type")));	
							table.Fields.Add(field.Name,field);
						}
	
						reader.Close();

						table.Script = "CREATE TABLE "+table.ToString()+"\n"
							+ "(\t";
						string separator="";
						foreach (Field field in table.Fields.Values) {
							table.Script += separator+field.Name+"\t"+field.Type;
							separator=",\n\t";
						}
						table.Script += "\n);\n";
						
					} catch (Exception ex) {
						Console.WriteLine("Exception reading the tables fields detail for table: " + table.ToString()+ " message : "+ex.Message);
						Console.WriteLine("SQL used: "+sql);
						Console.WriteLine("FIELDS: "+listFilesStr);
					}
				}
				
				// clean up
				reader.Close();
				reader = null;
				dbcmd.Dispose();
				dbcmd = null;

			}
		
			return tableList;
		}

		public SortedList<string,View> getViews() {

			System.Collections.Generic.SortedList<string,View> viewList=null;
			View view;

			// Check for DB Connection
			if (dbcon!=null) {

				viewList=new SortedList<string,View>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				string sql = "select table_name, view_definition from information_schema.views";
				dbcmd.CommandText = sql;
				IDataReader reader = dbcmd.ExecuteReader();
				while(reader.Read()) {
					view=new View(null,
					              reader.GetString(reader.GetOrdinal("table_name")),
					              reader.GetString(reader.GetOrdinal("view_definition"))+"\n");					                
					viewList.Add(view.ToString(),view);
				}
				
				// clean up
				reader.Close();
				reader = null;
				dbcmd.Dispose();
				dbcmd = null;

			}

			return viewList;
		}

		public SortedList<string,Index> getIndexes() {

			System.Collections.Generic.SortedList<string,Index> indexList=null;
			Index index;
			System.Collections.Generic.List<string> listTables=new System.Collections.Generic.List<string>();

			// Check for DB Connection
			if (dbcon!=null) {

				indexList=new SortedList<string,Index>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				string sql = "SHOW TABLES";
				dbcmd.CommandText = sql;
				IDataReader reader = dbcmd.ExecuteReader();
				while(reader.Read()) {
					listTables.Add(reader.GetString(0));
				}

				// clean up
				reader.Close();
				reader = null;
				dbcmd.Dispose();
				dbcmd = null;

				foreach (string tableName in listTables) {
					string sqlShowIndex="show index in "+tableName;
					dbcmd = dbcon.CreateCommand();
					dbcmd.CommandText=sqlShowIndex;
					reader = dbcmd.ExecuteReader();
					while (reader.Read()) {
						index=new Index(reader.GetString(reader.GetOrdinal("Table")),
						                reader.GetString(reader.GetOrdinal("Key_name")),
						                reader.GetString(reader.GetOrdinal("Key_name")),
						                "CREATE INDEX "+reader.GetString(reader.GetOrdinal("Key_name"))
						               +" ON "+reader.GetString(reader.GetOrdinal("Table"))
						               +" ("+reader.GetString(reader.GetOrdinal("Column_name"))+")"
						               +";\n");					                
						if (!indexList.ContainsKey(index.ToString())) {
							indexList.Add(index.ToString(),index);
						} else {
							Index oldIndex=indexList[index.ToString()];
							oldIndex.Script = oldIndex.Script.Substring(0,oldIndex.Script.Length-3)+","+reader.GetString(reader.GetOrdinal("Column_name"))+");\n";
						}
					}
					
					// clean up
					reader.Close();
					reader = null;
					dbcmd.Dispose();
					dbcmd = null;
				}

			}

			return indexList;
		}
		
		public SortedList<string,Sequence> getSequences() {

			SortedList<string,Sequence> sequencesList=null;
			sequencesList=new SortedList<string,Sequence>();

			return sequencesList;
		}

		public SortedList<string,Function> getFunctions() {

			SortedList<string,Function> tableList=new SortedList<string, Function>();
			return tableList;
		}
	}
}
