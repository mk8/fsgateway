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
using Npgsql;
using Mono.Fuse;
using System.Text.RegularExpressions;

namespace FsGateway
{
	
	
	public class DB_Postgresql : IFsDb
	{
		private IDbConnection dbcon=null;
		private string connectionString=null;
		private bool _isConnected=false;
		private int version_number_release=0; // e.s. 8.x.x
		private int version_number_major=0; // e.s. x.3.0
		private int version_number_minor=0; // e.s. x.x.0
		
		public DB_Postgresql()
		{
		}

		public DB_Postgresql(string host, string database, string user, string password, string port)
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
			names.Add ("/functions");
			names.Add ("/constraints");

			return names;
		}

		public void Dispose() {
			Unconnect();
			_isConnected=false;
		}
		
		public string storageType {
			get {
				return "DB_Postgresql";
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

				sql = "SELECT c.oid "
					+ "     , relname "
					+ "     , n.nspname as namespace "
					+ "from pg_class c "
					+ "left join pg_namespace n on n.oid = c.relnamespace "
					+ "where relkind = 'r' "
					+ "  and n.nspname not in ('pg_catalog', 'information_schema') "
			        + "order by n.nspname, relname";
					;
				try {
					dbcmd.CommandText = sql;
					reader = dbcmd.ExecuteReader();
					while(reader.Read()) {
						Table table=new Table(reader.GetInt64(reader.GetOrdinal("oid")),reader.GetString(reader.GetOrdinal("namespace")),reader.GetString(reader.GetOrdinal("relname")));
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
					   
					sql = "SELECT a.attnum "
						+ "     , a.attrelid "
						+ "     , a.attname "
						+ "     , a.atttypmod "
						+ "     , a.attstattarget "
						+ "     , a.attstorage "
						+ "     , t.typstorage "
						+ "     , a.attnotnull "
						+ "     , a.atthasdef "
						+ "     , a.attisdropped "
						+ "     , a.attislocal "
						+ "     , pg_catalog.format_type(t.oid,a.atttypmod) as atttypname "
						+ "from pg_catalog.pg_attribute a "
						+ "left join pg_catalog.pg_type t on a.atttypid = t.oid "
						+ "where a.attnum > 0::pg_catalog.int2 "
						+ "  and a.attrelid = "+table.Id+ " "
						+ "  and NOT a.attisdropped "
						+ "order by a.attnum ";
					string listFilesStr="";
					try {
						dbcmd.CommandText = sql;
						reader = dbcmd.ExecuteReader();
						listFilesStr="";
						while (reader.Read()) {
							if (!reader.IsDBNull(reader.GetOrdinal("atttypname"))) {
								listFilesStr += "Row number: "+reader.GetValue(0).ToString()+"\n";
								listFilesStr += "Row name: "+reader.GetValue(reader.GetOrdinal("attname")).ToString();
								Field field=new Field(
									reader.GetString(reader.GetOrdinal("attname")),
									reader.GetString(reader.GetOrdinal("atttypname")),
									reader.GetBoolean(reader.GetOrdinal("attnotnull"))
								);
								table.Fields.Add(field.Name,field);
							}
						}
	
						reader.Close();

						table.Script = "CREATE TABLE "+table.ToString()+"\n"
							+ "(\t";
						string separator="";
						foreach (Field field in table.Fields.Values) {
							table.Script += separator+field.Name+"\t"+field.Type + (field.NotNull ? "\tNOT NULL" : "");
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
				string sql = "SELECT * "
						   + "FROM pg_catalog.pg_views "
						   + "WHERE schemaname!='pg_catalog' "
						   + "  AND schemaname!='information_schema' "
						   + "ORDER BY schemaname, viewname ";
				dbcmd.CommandText = sql;
				IDataReader reader = dbcmd.ExecuteReader();
				while(reader.Read()) {
					view=new View(reader.GetString(reader.GetOrdinal("schemaname")),
					              reader.GetString(reader.GetOrdinal("viewname")),
					              reader.GetString(reader.GetOrdinal("definition"))+"\n");					                
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
				string sql = "SELECT n.nspname as SCHEMA "
					       + "     , c.relname as NAME "
						   + "     , u.usename as OWNER "
						   + "FROM pg_catalog.pg_class c "
						   + "LEFT JOIN pg_catalog.pg_user u ON u.usesysid = c.relowner "
						   + "LEFT JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace "
						   + "WHERE c.relkind='S' "
						   + "  AND n.nspname NOT IN ('pg_catalog', 'pg_toast') "
						   + "  AND pg_catalog.pg_table_is_visible(c.oid) "
						   + "ORDER BY 1,2";
				dbcmd.CommandText = sql;
				IDataReader reader = dbcmd.ExecuteReader();
				while(reader.Read()) {
					Sequence sequence=new Sequence(reader.GetString(reader.GetOrdinal("SCHEMA")),reader.GetString(reader.GetOrdinal("NAME")));					
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

		public SortedList<string,Function> getFunctions() {

			SortedList<string,Function> functionsList=null;

			// Check for DB Connection
			if (dbcon!=null) {

				functionsList=new SortedList<string,Function>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				string sql = "SELECT n.nspname as Schema " +
				             "     , p.proname as Name " +
				             "     , l.lanname as Language " +
				             "     , t.typname as TypeName " +
				             "     , t.typlen as TypeLen " +
				             "     , p.proargnames as Parameters " +
				             "     , p.prosrc as Source " +
				             "FROM pg_catalog.pg_proc p " +
				             "LEFT JOIN pg_catalog.pg_namespace n " +
				             "  ON n.oid = p.pronamespace " +
				             "LEFT JOIN pg_catalog.pg_language l " +
				             "  ON l.oid = p.prolang " +
				             "LEFT JOIN pg_catalog.pg_type t " +
				             "  ON t.oid = p.prorettype " +
				             "WHERE n.nspname not in ('pg_catalog', 'information_schema') ";
				Console.WriteLine ("SQL: " + sql);
				dbcmd.CommandText = sql;
				IDataReader reader = dbcmd.ExecuteReader();
				while(reader.Read()) {
					String signature = reader.GetString(reader.GetOrdinal("Name")) + "(" + reader.GetValue(reader.GetOrdinal("Parameters")).ToString() + ")";
					Function function=new Function(reader.GetString(reader.GetOrdinal("Schema")),signature);	

					function.Script = "CREATE OR REPLACE FUNCTION  " + function.ToString () + "\n"
					+ "RETURNS " + reader.GetString (reader.GetOrdinal ("TypeName")) + " AS \n"
					+ "$BODY$ " + reader.GetString (reader.GetOrdinal ("Source")) + "$BODY$\n"
					+ "LANGUAGE " + reader.GetString (reader.GetOrdinal ("Language")) + ";\n";

					functionsList.Add(function.ToString(),function);
				}

				// clean up
				reader.Close();
				reader = null;
				dbcmd.Dispose();
				dbcmd = null;
			}

			return functionsList;
		}

		public SortedList<string,fsgateway.Constraint> getConstraints() {

			SortedList<string,fsgateway.Constraint> constraintList=null;

			// Check for DB Connection
			if (dbcon!=null) {

				constraintList=new SortedList<string,fsgateway.Constraint>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				string sql = @"
SELECT c.conname, n.nspname as schemaname, class1.relname as tablename, class2.relname, attribute1.attname as source, class3.relname as targetname, attribute2.attname as destination 
FROM (SELECT conname
           , connamespace
           , contype
           , condeferrable
           , condeferred
           , convalidated
           , conrelid
           , contypid
           , conindid
           , confrelid
           , confupdtype
           , confdeltype
           , confmatchtype
           , conislocal
           , coninhcount
           , unnest(conkey) as conkey
           , unnest(confkey) as confkey
      FROM pg_catalog.pg_constraint
     ) c

JOIN pg_catalog.pg_namespace n
  ON n.oid=c.connamespace
JOIN pg_catalog.pg_class class1
  ON c.conrelid = class1.oid
LEFT JOIN pg_catalog.pg_class class2
  ON c.conindid = class2.oid
JOIN pg_catalog.pg_attribute attribute1
  ON attribute1.attrelid = c.conrelid
 AND attribute1.attnum = c.conkey

JOIN pg_catalog.pg_class class3
  ON c.confrelid = class3.oid
JOIN pg_catalog.pg_attribute attribute2
  ON attribute2.attrelid = c.confrelid
 AND attribute2.attnum = c.confkey

WHERE c.contype='f' 
";
				dbcmd.CommandText = sql;
				IDataReader reader = dbcmd.ExecuteReader();
				while(reader.Read()) {
					string key = reader.GetString (reader.GetOrdinal ("schemaname")) + "." +
					             reader.GetString (reader.GetOrdinal ("tablename")) + "." +
					             reader.GetString (reader.GetOrdinal ("conname"));
					KeyValuePair<string, string> pair = new KeyValuePair<string, string> (
						reader.GetString (reader.GetOrdinal ("source")),
						reader.GetString (reader.GetOrdinal ("destination")));

					if (constraintList.ContainsKey (key)) {
						constraintList [key].FieldMapping.Add (pair);
					} else {
						fsgateway.Constraint constraint = new fsgateway.Constraint (reader.GetString (reader.GetOrdinal ("schemaname")),
							                        reader.GetString (reader.GetOrdinal ("conname")),
													reader.GetString (reader.GetOrdinal ("tablename")),
							                        reader.GetString (reader.GetOrdinal ("targetname")));
						constraint.FieldMapping.Add (pair);
						constraintList.Add (key, constraint);
					}
				}

				// clean up
				reader.Close();
				reader = null;
				dbcmd.Dispose();
				dbcmd = null;
			}

			return constraintList;
		}

	}
}
