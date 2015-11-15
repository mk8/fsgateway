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
using Mono.Data.Sqlite;
using Mono.Fuse;
using System.Text.RegularExpressions;

namespace FsGateway
{
	
	
	public class DB_Sqlite : IFsDb
	{
		private SqliteConnection dbcon=null;
		private string connectionString=null;
		private bool _isConnected=false;
		
		public DB_Sqlite()
		{
		}

		public DB_Sqlite(string host, string database, string user, string password, string port)
		{

			string connectionString = "Data Source="+database+"version=3";
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
				return "DB_SQLite";
			}
		}

		public bool isConnect {
			get {
				return _isConnected;
			}
		}
		
		public string Usage {
			get {
				return "Specify the connection parameter like this one: \"Data Source=myfile,version=3";
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
				dbcon = new SqliteConnection(connectionString);
				dbcon.Open();
				this.connectionString=connectionString;
				res=true;
				_isConnected=true;
			} catch (Exception ex) {
				System.Console.Out.WriteLine("Exception during database connection opening. Error message: "+ex.Message);
				dbcon=null;
			}
			return res;
		}
		
		public void Unconnect() {
			if (dbcon!=null && dbcon.State==System.Data.ConnectionState.Open) {
				dbcon.Close();
				dbcon=null;
			}
			
			_isConnected=true;
			return;
		}
		
		public bool isConnected {
			get {
				return _isConnected;
			}
		}
		
		public SortedList<string,Table> getTables() {

			SortedList<string,Table> tableList=null;
			string sql;
			IDataReader reader=null;
			
			tableList=new SortedList<string,Table>();

			// Check for DB Connection
			if (dbcon!=null) {

				System.Data.Common.DbCommand dbcmd = dbcon.CreateCommand();
				sql = "select * "
					+ "from sqlite_master "
					+ "where type='table'; "
					;
				
				try {
					dbcmd.CommandText = sql;
					reader = dbcmd.ExecuteReader();
					while(reader.Read()) {
						Table table=new Table(null,reader.GetString(reader.GetOrdinal("name")));
						table.Script = reader.GetString(reader.GetOrdinal("sql"))+"\n";
						tableList.Add(table.ToString(), table);
					}
				} catch (Exception ex) {
					Console.WriteLine("Exception reading the tables list : "+ex.Message);
					Console.WriteLine("List tables: SQL=" + sql);
				}
				reader.Close();				
				
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
			String sql;
			IDataReader reader=null;
			
			viewList=new SortedList<string,View>();
			// Check for DB Connection
			if (dbcon!=null) {

				System.Data.Common.DbCommand dbcmd = dbcon.CreateCommand();
				sql = "select * "
					+ "from sqlite_master "
					+ "where type='view' "
					;
				
				try {
					dbcmd.CommandText = sql;
					reader = dbcmd.ExecuteReader();
					while(reader.Read()) {
						view=new View(null,
						              reader.GetString(reader.GetOrdinal("name")),
						              reader.GetString(reader.GetOrdinal("sql"))+"\n");					                
						viewList.Add(view.ToString(),view);
					}
				} catch (Exception ex) {
					Console.WriteLine("Exception reading the views list : "+ex.Message);
					Console.WriteLine("List tables: SQL=" + sql);
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
			IDataReader reader=null;
			string sql;
			
			indexList=new SortedList<string,Index>();

			// Check for DB Connection
			if (dbcon!=null) {

				System.Data.Common.DbCommand dbcmd = dbcon.CreateCommand();
				sql = "select * "
					+ "from sqlite_master "
					+ "where type='index' "
					;
				
				try {
					dbcmd.CommandText = sql;
					reader = dbcmd.ExecuteReader();
					while(reader.Read()) {
						index=new Index(null,
						                reader.GetValue(reader.GetOrdinal("tbl_name")).ToString(),					                
						                reader.GetValue(reader.GetOrdinal("name")).ToString(),
						                reader.GetValue(reader.GetOrdinal("sql")).ToString()+";\n");					                
						indexList.Add(index.ToString(),index);
					}
				} catch (Exception ex) {
					Console.WriteLine("Exception reading the indexes list : "+ex.Message);
					Console.WriteLine("List tables: SQL=" + sql);
				}
				
				// clean up
				reader.Close();
				reader = null;
				dbcmd.Dispose();
				dbcmd = null;
			}
			
			return indexList;
		}
		
		public SortedList<string,Sequence> getSequences() {

			SortedList<string,Sequence> sequenceList=null;
			sequenceList=new SortedList<string,Sequence>();
			return sequenceList;
		}

		public SortedList<string,Function> getFunctions() {

			SortedList<string,Function> tableList=new SortedList<string, Function>();
			return tableList;
		}

		public SortedList<string,fsgateway.Constraint> getConstraints() {

			SortedList<string,fsgateway.Constraint> constraintList=null;
			constraintList=new SortedList<string,fsgateway.Constraint>();

			return constraintList;
		}

	}
}
