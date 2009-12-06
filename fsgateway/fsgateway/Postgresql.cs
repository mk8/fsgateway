using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;
using Mono.Fuse;
using System.Text.RegularExpressions;

namespace FsGateway
{
	
	
	public class Postgresql : IFsDb
	{
		private IDbConnection dbcon=null;
		private string connectionString=null;
		private bool _isConnected=false;
		private int version_number_release=0; // e.s. 8.x.x
		private int version_number_major=0; // e.s. x.3.0
		private int version_number_minor=0; // e.s. x.x.0
		
		public Postgresql()
		{
		}

		public Postgresql(string host, string database, string user, string password, string port)
		{

			string connectionString = "Server="+host+";" +
				"Database="+database+";" +
				"User ID="+user+";" +
				"Password="+password+";"+
				"Port="+port+";";
			Connect(connectionString);
		}

		public void Dispose() {
			Unconnect();
			_isConnected=false;
		}
		
		public string storageType {
			get {
				return "postgresql";
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
			
			// Check for DB Connection
			if (dbcon!=null) {

				tableList=new SortedList<string,Table>();

				IDbCommand dbcmd = dbcon.CreateCommand();
				if (version_number_release == 8 && version_number_major >= 2) {
					sql = "SELECT c.tableoid "
						+ "     , c.oid "
						+ "     , relname "
						+ "     , relacl "
						+ "     , relkind "
						+ "     , relowner as rolname "
						+ "     , relchecks "
//						+ "     , reltriggers "
						+ "     , relhasindex "
						+ "     , relhasrules "
						+ "     , relhasoids "
						+ "     , d.refobjid as owning_tab "
						+ "     , d.refobjsubid as owning_col "
						+ "     , t.spcname as reltablespace "
						+ "     , n.nspname as namespace "
						+ "     , array_to_string(c.reloptions, ', ') as reloptions "
						+ "from pg_class c "
						+ "left join pg_depend d on (c.relkind = 'S' and d.classid = c.tableoid and d.objid = c.oid and d.objsubid = 0 and d.refclassid = c.tableoid and d.deptype = 'a') "
						+ "left join pg_tablespace t on t.oid = c.reltablespace "
						+ "left join pg_namespace n on n.oid = c.relnamespace "
						+ "where relkind = 'r' "
					//	+ "  AND pg_catalog.pg_table_is_visible(c.oid) "
						+ "  and n.nspname not in ('pg_catalog', 'information_schema') "
						+ "order by n.nspname, relname";
				} else if (version_number_release == 8) {
					sql = "SELECT c.tableoid "
						+ "     , c.oid "
						+ "     , relname "
						+ "     , relacl "
						+ "     , relkind "
						+ "     , relnamespace "
						+ "     , relowner as rolname "
						+ "     , relchecks "
						+ "     , reltriggers "
						+ "     , relhasindex "
						+ "     , relhasrules "
						+ "     , relhasoids "
						+ "     , d.refobjid as owning_tab "
						+ "     , d.refobjsubid as owning_col "
						+ "     , (SELECT spcname FROM pg_tablespace t WHERE t.oid = c.reltablespace) AS reltablespace "
						+ "     , n.nspname as namespace "
						+ "     , NULL as reloptions  "
						+ "from pg_class c "
						+ "left join pg_depend d "
						+ "  on (c.relkind = 'S' and d.classid = c.tableoid and d.objid = c.oid and d.objsubid = 0 and d.refclassid = c.tableoid and d.deptype = 'i') "
						+ "left join pg_namespace n on n.oid = c.relnamespace "
						+ "where relkind = 'r' "
						+ "  and n.nspname not in ('pg_catalog', 'information_schema') "
						+ "order by c.oid "
						;
				} else if (version_number_release == 7 && version_number_major >= 3) {
					sql = "SELECT c.tableoid "
						+ "     , c.oid "
						+ "     , relname "
						+ "     , relacl "
						+ "     , relkind "
						+ "     , relnamespace "
						+ "     , relowner as rolname "
						+ "     , relchecks "
						+ "     , reltriggers "
						+ "     , relhasindex "
						+ "     , relhasrules "
						+ "     , relhasoids "
						+ "     , d.refobjid as owning_tab "
						+ "     , d.refobjsubid as owning_col "
						+ "     , NULL AS reltablespace "
						+ "     , n.nspname as namespace "
						+ "     , NULL as reloptions  "
						+ "from pg_class c "
						+ "left join pg_depend d "
						+ "  on (c.relkind = 'S' and d.classid = c.tableoid and d.objid = c.oid and d.objsubid = 0 and d.refclassid = c.tableoid and d.deptype = 'i') "
						+ "left join pg_namespace n on n.oid = c.relnamespace "
						+ "where relkind = 'r' "
						+ "  and n.nspname not in ('pg_catalog', 'information_schema') "
						+ "order by c.oid "
						;
				} else if (version_number_release == 7 && version_number_major >= 2) {
					sql = "SELECT c.tableoid "
						+ "     , c.oid "
						+ "     , relname "
						+ "     , relacl "
						+ "     , relkind "
						+ "     , relnamespace "
						+ "     , relowner as rolname "
						+ "     , relchecks "
						+ "     , reltriggers "
						+ "     , relhasindex "
						+ "     , relhasrules "
						+ "     , relhasoids "
						+ "     , NULL::oid as owning_tab "
						+ "     , NULL::int4 as owning_col "
						+ "     , NULL AS reltablespace "
						+ "     , n.nspname as namespace "
						+ "     , NULL as reloptions  "
						+ "from pg_class c "
						+ "left join pg_namespace n on n.oid = c.relnamespace "
						+ "where relkind = 'r' "
						+ "  and n.nspname not in ('pg_catalog', 'information_schema') "
						+ "order by c.oid "
						;
				} else if (version_number_release == 7 && version_number_major >= 1) {
					sql = "SELECT c.tableoid "
						+ "     , c.oid "
						+ "     , relname "
						+ "     , relacl "
						+ "     , relkind "
						+ "     , 0::oid as relnamespace "
						+ "     , relowner as rolname "
						+ "     , relchecks "
						+ "     , reltriggers "
						+ "     , relhasindex "
						+ "     , relhasrules "
						+ "     , relhasoids "
						+ "     , NULL::oid as owning_tab "
						+ "     , NULL::int4 as owning_col "
						+ "     , NULL AS reltablespace "
						+ "     , n.nspname as namespace "
						+ "     , NULL as reloptions  "
						+ "from pg_class c "
						+ "left join pg_namespace n on n.oid = c.relnamespace "
						+ "where relkind = 'r' "
						+ "  and n.nspname not in ('pg_catalog', 'information_schema') "
						+ "order by c.oid "
						;
				} else {
					sql = "SELECT (SELECT oid FROM pg_class WHERE relname = 'pg_class') AS tableoid "
						+ "     , oid "
						+ "     , relname "
						+ "     , relacl "
						+ "     , CASE WHEN relhasrules and relkind = 'r' and EXISTS(SELECT rulename FROM pg_rewrite r WHERE r.ev_class = c.oid AND r.ev_type = '1') "
						+ "                 THEN '%c'::\"char\" "
						+ "                 ELSE relkind END AS relkind "
						+ "     , 0::oid as relnamespace "
						+ "     , (relowner) as rolname "
						+ "     , relchecks "
						+ "     , reltriggers "
						+ "     , relhasindex "
						+ "     , relhasrules "
						+ "     , 't'::bool as relhasoids "
						+ "     , NULL::oid as owning_tab "
						+ "     , NULL::int4 as owning_col "
						+ "     , NULL as reltablespace "
						+ "     , n.nspname as namespace "
						+ "     , NULL as reloptions "
						+ "from pg_class c "
						+ "left join pg_namespace n on n.oid = c.relnamespace "
						+ "where relkind = 'r' "
						+ "  and n.nspname not in ('pg_catalog', 'information_schema') "
						+ "order by oid"
						;
				}
				
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
								Field field=new Field(reader.GetString(reader.GetOrdinal("attname")),reader.GetString(reader.GetOrdinal("atttypname")));	
								table.Fields.Add(field.Name,field);
							}
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
