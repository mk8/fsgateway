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
using Mono.Fuse;
using System.Text.RegularExpressions;

namespace FsGateway
{
	
	
	public class FsPostgresql : IFsDb
	{
		
		private IDbConnection dbcon=null;
		private string connectionString=null;
		private bool _isConnected=false;
		private int version_number_release=0; // e.s. 8.x.x
		private int version_number_major=0; // e.s. x.3.0
		private int version_number_minor=0; // e.s. x.x.0

		public FsPostgresql()
		{
		}

		public FsPostgresql(string host, string database, string user, string password, string port)
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
		    names.Add ("/sequences");
			
			return names;
		}

		public void Dispose() {
			Unconnect();
			_isConnected=false;
		}
		
		public string storageType {
			get {
				return "PostgreSQL";
			}
		}

		public string Usage {
			get {
				return "Specify the connection parameter like this one: \"Server=localhost; Database=mydb; User ID=username;Password=password;Port=5432;";
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
				dbcon = new NpgsqlConnection(connectionString);
				dbcon.Open();
				this.connectionString=connectionString;
				res=true;
				_isConnected=true;
				
				readVersionNumber();
				
			} catch (Exception ex) {
				System.Console.Out.WriteLine("Exception during database connection opening. Error message: "+ex.Message);
				dbcon=null;
			}
			return res;
		}
		
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
		
		public void Unconnect() {
			bool res=false;
			
			if (dbcon!=null && dbcon.State==System.Data.ConnectionState.Open) {
				dbcon.Close();
				dbcon=null;
				res=false;
			}
			
			_isConnected=true;
			return;
		}
		
		public SortedList<string,Table> getTables() {

			SortedList<string,Table> tableList=null;
			string sql;
			IDataReader reader=null;
			IDbDataParameter parameter=null;
			
			// Check for DB Connection
			if (dbcon!=null) {

				tableList=new SortedList<string,Table>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				sql = "SELECT * "
					+ "FROM information_schema.tables "
					+ "where table_schema not in ('pg_catalog', 'information_schema') "
					+ "  AND table_type='BASE TABLE' "
					+ "ORDER BY table_schema, table_name "
					;				
				try {
					dbcmd.CommandText = sql;
					reader = dbcmd.ExecuteReader();
					while(reader.Read()) {
						Table table=new Table(reader.GetString(reader.GetOrdinal("table_schema")),reader.GetString(reader.GetOrdinal("table_name")));
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
					   
					sql = "SELECT * "
						+ "FROM information_schema.columns "
						+ "WHERE table_schema = :table_schema " // table.Schema
						+ "  AND table_name = :table_name " // table.Name
						+ "ORDER BY ordinal_position "
						;
					string listFilesStr="";
					try {
						dbcmd.CommandText = sql;

						parameter = dbcmd.CreateParameter();
						parameter.ParameterName="table_schema";
						parameter.DbType = DbType.String;
						parameter.Direction = ParameterDirection.Input;
						parameter.Value=table.Schema;
						dbcmd.Parameters.Add(parameter);
						
						parameter = dbcmd.CreateParameter();
						parameter.ParameterName="table_name";
						parameter.DbType = DbType.String;
						parameter.Direction = ParameterDirection.Input;
						parameter.Value=table.Name;
						dbcmd.Parameters.Add(parameter);
						
						reader = dbcmd.ExecuteReader();
						listFilesStr="";
						while (reader.Read()) {
							Field field=null;
							
							if (reader.IsDBNull(reader.GetOrdinal("character_maximum_length"))) {
								field=new Field(reader.GetString(reader.GetOrdinal("column_name")),reader.GetString(reader.GetOrdinal("data_type")));	
							} else {
								field=new Field(reader.GetString(reader.GetOrdinal("column_name")),reader.GetString(reader.GetOrdinal("data_type"))+" ("+reader.GetInt32(reader.GetOrdinal("character_maximum_length"))+")");	
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
						Console.WriteLine("FIELDS: "+listFilesStr);
						Console.WriteLine("StackTrace: "+ex.StackTrace);
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
				string sql = "select * "
						   + "from information_schema.views "
						   + "where table_schema not in ('pg_catalog', 'information_schema') "
						   + "ORDER BY table_schema, table_name "
						   ;
				dbcmd.CommandText = sql;
				IDataReader reader = dbcmd.ExecuteReader();
				while(reader.Read()) {
					view=new View(reader.GetString(reader.GetOrdinal("table_schema")),
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
			
			// Check for DB Connection
			if (dbcon!=null) {

				indexList=new SortedList<string,Index>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				string sql = "SELECT * "
						   + "FROM pg_catalog.pg_indexes "
						   + "WHERE schemaname!='pg_catalog' "
						   + "ORDER BY schemaname, indexname ";
				dbcmd.CommandText = sql;
				IDataReader reader = dbcmd.ExecuteReader();
				while(reader.Read()) {
					index=new Index(reader.GetString(reader.GetOrdinal("schemaname")),
					                reader.GetString(reader.GetOrdinal("tablename")),					                
					                reader.GetString(reader.GetOrdinal("indexname")),
					                reader.GetString(reader.GetOrdinal("indexdef"))+";\n");					                
					indexList.Add(index.ToString(),index);
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

			SortedList<string,Sequence> sequencesList=null;
			
			// Check for DB Connection
			if (dbcon!=null) {

				sequencesList=new SortedList<string,Sequence>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				string sql = "SELECT * "
					       + "FROM INFORMATION_SCHEMA.SEQUENCES "
						   + "ORDER BY sequence_schema, sequence_name "
						   ;
				dbcmd.CommandText = sql;
				IDataReader reader = dbcmd.ExecuteReader();
				while(reader.Read()) {
					Sequence sequence=new Sequence(reader.GetString(reader.GetOrdinal("sequence_schema")),reader.GetString(reader.GetOrdinal("sequence_name")));					
					sequencesList.Add(sequence.ToString(),sequence);
					
				}
				reader.Close();				
				
				// ReRead data for fetching detail
				foreach (Sequence sequence in sequencesList.Values) {
					
					sql = "SELECT sequence_name "
						+ "     , last_value "
						+ "     , increment_by "
						+ "     , max_value "
						+ "     , min_value "
						+ "     , cache_value "
						+ "     , is_cycled "
						+ "     , is_called from "+sequence.ToString();
					dbcmd.CommandText = sql;
					reader = dbcmd.ExecuteReader();
					if (reader.Read()) {
						sequence.LastValue=reader.GetInt64(reader.GetOrdinal("last_value"));
						sequence.IncrementBy=reader.GetInt64(reader.GetOrdinal("increment_by"));
						sequence.MaxValue=reader.GetInt64(reader.GetOrdinal("max_value"));
						sequence.MinValue=reader.GetInt64(reader.GetOrdinal("min_value"));
						sequence.CacheValue=reader.GetInt64(reader.GetOrdinal("cache_value"));
						sequence.IsCycled=reader.GetBoolean(reader.GetOrdinal("is_cycled"));
						sequence.IsCalled=reader.GetBoolean(reader.GetOrdinal("is_called"));

						sequence.Script="CREATE SEQUENCE "+sequence.ToString()+"\n"
							    + (sequence.IsCalled ? "    START WITH "+sequence.LastValue + "\n" : "")
								+ "    INCREMENT BY "+sequence.IncrementBy+"\n"
								+ (sequence.MaxValue!=0x7FFFFFFFFFFFFFFF ? "    MAXVALUE "+sequence.MaxValue : "    NO MAXVALUE") + "\n"
								+ (sequence.MinValue!=-0x7FFFFFFFFFFFFFFF ? "    MINVALUE "+sequence.MinValue : "    NO MINVALUE") + "\n"
								+ "    CACHE "+sequence.CacheValue + (sequence.IsCycled ? "\n    CYCLE" : "") + "\n"
								+ ";\n";
					}
					reader.Close();
				}
				
				// clean up
				reader.Close();
				reader = null;
				dbcmd.Dispose();
				dbcmd = null;
			}
			
			return sequencesList;
		}
		
		/*
		 * Just for class testing only
		 */
		public static void Main(string[] args)
		{
			Postgresql pg=new Postgresql("localhost", "affissioni", "affissioni", "", "5432");

			System.Console.Out.WriteLine("Tables:");
			SortedList<string,Table> tables=pg.getTables();
			foreach (Table table in tables.Values) {
				System.Console.Out.WriteLine(table.ToString());
			}

			System.Console.Out.WriteLine("Views:");
			SortedList<string,View> views=pg.getViews();
			foreach (View view in views.Values) {
				System.Console.Out.WriteLine(view.ToString());
			}
		
			System.Console.Out.WriteLine("Indexes:");
			SortedList<string,Index> indexes=pg.getIndexes();
			foreach (Index index in indexes.Values) {
				System.Console.Out.WriteLine(index.ToString());
			}
		
			System.Console.Out.WriteLine("Sequences:");
			SortedList<string,Sequence> sequences=pg.getSequences();
			foreach (Sequence sequence in sequences.Values) {
				System.Console.Out.WriteLine(sequence.ToString());
			}
		
		}		
	}
}
