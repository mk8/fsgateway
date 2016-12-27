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
using Mono.Fuse;
using System.Data.SqlClient;

namespace FsGateway
{
	
	
	public class DB_SqlServer : IFsDb
	{
		
		private IDbConnection dbcon=null;
		private string connectionString=null;
		private bool _isConnected=false;

		public DB_SqlServer()
		{
		}

		public DB_SqlServer(string host, string database, string user, string password, string port)
		{

			string connectionString = "Server="+host+","+port+";" +
				"Database="+database+";" +
				"User ID="+user+";" +
				"Password="+password+";";
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
				return "DB_SqlServer";
			}
		}
		
		public string Usage {
			get {
				return "Specify the connection parameter like this one: \"Server=localhost,port; Database=mydb; User ID=username;Password=password;\r\n"
				      +"or like this one: \"Server=localhost,port; Database=mydb; User ID=domainname\\username;Password=password;Integrated Security=SSPI\r\n"
					  +"For detail information about the parameters connection look at http://www.mono-project.com/SQLClient";
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
				dbcon = new System.Data.SqlClient.SqlConnection(connectionString);
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

				lock (dbcmd) {
					sql = "select name, user_name(uid) from sysobjects where type='U'"
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
						Console.WriteLine("List tables: SQL=" + sql);
					}
					reader.Close();				
	
					// ReRead data for fetching detail
					foreach (Table table in tableList.Values) {
						   
						sql = "SELECT COLUMN_NAME, data_type, CHARACTER_MAXIMUM_LENGTH "
							+ "FROM INFORMATION_SCHEMA.COLUMNS "
							+ "WHERE TABLE_NAME = '"+table.Name+"'";
						try {
							dbcmd.CommandText = sql;
							reader = dbcmd.ExecuteReader();
							while (reader.Read()) {
								Field field;
								if (reader.IsDBNull(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH"))) {
									field=new Field(reader.GetString(reader.GetOrdinal("Column_name")),reader.GetString(reader.GetOrdinal("data_type")));
								} else {
									field=new Field(reader.GetString(reader.GetOrdinal("Column_name")),reader.GetString(reader.GetOrdinal("data_type"))+"("+reader.GetInt32(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH"))+")");	
								}
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
							Console.WriteLine(ex.StackTrace);
						}
	
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

			string sql;
			IDataReader reader=null;
			SortedList<string,View> viewList=null;
			View view;

			// Check for DB Connection
			if (dbcon!=null) {

				viewList=new SortedList<string,View>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				lock (dbcmd) {
					sql = "select name, user_name(uid) from sysobjects where type='V'"
					      ;
					
					try {
						dbcmd.CommandText = sql;
						reader = dbcmd.ExecuteReader();
						while(reader.Read()) {
							view=new View(null,reader.GetString(0),null);
							viewList.Add(view.ToString(), view);
						}
					} catch (Exception ex) {
						Console.WriteLine("Exception reading of the view list : "+ex.Message);
						Console.WriteLine("List views: SQL=" + sql);
					}
					reader.Close();				
					
					// ReRead data for fetching detail
					foreach (View viewDetail in viewList.Values) {
						   
						sql = "SELECT COLUMN_NAME, data_type, CHARACTER_MAXIMUM_LENGTH "
							+ "FROM INFORMATION_SCHEMA.COLUMNS "
							+ "WHERE TABLE_NAME = '"+viewDetail.Name+"'";
						SortedList<string,Field> listFields=new SortedList<string,Field>();
						try {
							dbcmd.CommandText = sql;
							reader = dbcmd.ExecuteReader();
							while (reader.Read()) {
								Field field;
								if (reader.IsDBNull(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH"))) {
									field=new Field(reader.GetString(reader.GetOrdinal("Column_name")),reader.GetString(reader.GetOrdinal("data_type")));
								} else {
									field=new Field(reader.GetString(reader.GetOrdinal("Column_name")),reader.GetString(reader.GetOrdinal("data_type"))+"("+reader.GetInt32(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH"))+")");	
								}
								listFields.Add(field.Name,field);
							}
		
							reader.Close();
							reader = null;
	
							viewDetail.Script = "CREATE VIEW "+viewDetail.ToString()+"\n"
								+ "(\t";
							string separator="";
							foreach (Field field in listFields.Values) {
								viewDetail.Script += separator+field.Name+"\t"+field.Type;
								separator=",\n\t";
							}
							viewDetail.Script += "\n);\n";
							
						} catch (Exception ex) {
							Console.WriteLine("Exception reading the views fields detail for table: " + viewDetail.ToString()+ " message : "+ex.Message);
							Console.WriteLine("SQL used: "+sql);
							Console.WriteLine(ex.StackTrace);
						}
	
					}
				}

				// clean up
				dbcmd.Dispose();
				dbcmd = null;

			}

			return viewList;

		}

		public SortedList<string,Index> getIndexes() {

			SortedList<string,Index> indexList=null;
			Index index;
			string sql;
			List<string> listTables=new System.Collections.Generic.List<string>();
			IDataReader reader=null;

			// Check for DB Connection
			if (dbcon!=null) {

				indexList=new SortedList<string,Index>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				lock (dbcmd) {
					sql = "select name, user_name(uid) from sysobjects where type='U'"
					      ;
					try {
						dbcmd.CommandText = sql;
						reader = dbcmd.ExecuteReader();
						while(reader.Read()) {
							listTables.Add(reader.GetString(0));
						}
					} catch (Exception ex) {
						Console.WriteLine("Exception reading of the tables list for the indexes : "+ex.Message);
						Console.WriteLine("List tables: SQL=" + sql);
					}
					reader.Close();				
	
					foreach (string tableName in listTables) {
						sql="exec sp_helpindex "+tableName;
						dbcmd.CommandText=sql;
						reader = dbcmd.ExecuteReader();
						while (reader.Read()) {
							index=new Index(tableName,
							                reader.GetString(reader.GetOrdinal("index_name")),
							                reader.GetString(reader.GetOrdinal("index_name")),
							                "CREATE INDEX "+reader.GetString(reader.GetOrdinal("index_name"))
							               +" ON "+tableName
							               +" ("+reader.GetString(reader.GetOrdinal("index_keys"))+")"
							               +";\n");
							indexList.Add(index.ToString(),index);
						}
						
						// clean up
						reader.Close();
						reader = null;
					}
				}

				dbcmd.Dispose();
				dbcmd = null;
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

		public SortedList<string,fsgateway.Constraint> getConstraints() {

			SortedList<string,fsgateway.Constraint> constraintList=null;
			constraintList=new SortedList<string,fsgateway.Constraint>();

			return constraintList;
		}

	}
}
