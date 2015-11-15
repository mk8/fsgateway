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
using System.Collections.Generic;
using Mono.Fuse;
using Mono.Unix.Native;

namespace FsGateway
{
	
	public class FsDbManager: IFsGateway
	{
		private List<string> names = new List<string> ();
		private IFsDb db=null;
		private string connectionString=null;
		
		private SortedList<string,Index> listIndexes=null;
		private SortedList<string,View> listViews=null;
		private SortedList<string,Sequence> listSequences=null;
		private SortedList<string,Table> listTables=null;
		private SortedList<string,Function> listFunctions = null;
		private SortedList<string,fsgateway.Constraint> listConstraints = null;

		public FsDbManager()
		{
		}
				
		public FsDbManager (IFsDb db) {
			
			names = db.getTypeOfObjects();
			this.db=db;
		}
		
		public void Dispose() {
			if (db!=null)
				db.Unconnect();
				
			db=null;
		}
		
		public bool Connect() {
			if (db==null) {
				if (connectionString!=null && connectionString.Length>0) {
					IFsDb pg=new DB_Postgresql();
					pg.Connect(connectionString);
					this.db=pg;
				}
			}

			if (db!=null) 
				return db.Connect(this.connectionString);
			else
				return false;
		}
		
		public bool Connect(string parameter) {
			connectionString=parameter;
			return this.Connect();
		}
		
		public void Unconnect() {
			if (db!=null) {
				db.Unconnect();
				db=null;
			}
		}
			
		public bool isConnect {
			get {
				bool res=false;
			
				if (db!=null)
					res=db.isConnect;
				
				return res;
			}
		}
		
		public string storageType {
			get {
				return null;
			}
		}

		public string Usage {
			get {
				return null;
			}
		}
		
		public string connectionParameter {
			get {
				return this.connectionString;
			}
		}

		public Errno OnReadSymbolicLink (string link, out string target) {
			target=null;
			return Errno.ENOENT;
		}

		public Errno OnReadDirectory (string directory, OpenedPathInfo info,
		                                          out IEnumerable<DirectoryEntry> names)
		{
			Console.WriteLine ("@@ OnReadDirectory:" + directory);

			// Check for root directory
			if (directory.Equals("/")) {
				names = ListNames (directory);
			} else if (directory.Equals("/tables")) {
				listTables=db.getTables();
				names = ListNames(listTables);
			} else if (directory.Equals("/views")) {
				listViews=db.getViews();
				names = ListNames(listViews);
			} else if (directory.Equals("/indexes")) {
				listIndexes=db.getIndexes();
				names = ListNames(listIndexes);
			} else if (directory.Equals("/sequences")) {
				listSequences=db.getSequences();
				names = ListNames(listSequences);
			} else if (directory.Equals("/constraints")) {
				listConstraints=db.getConstraints();
				names = ListNames(listConstraints);
			} else if (directory.Equals("/functions")) {
				Console.WriteLine ("@@ HIHIHIHIHI:");
				try {
					listFunctions=db.getFunctions();
				} catch (Exception ex) {
					Console.WriteLine ("Exception: " + ex.Message);
					Console.WriteLine (ex.StackTrace);
				}
				Console.WriteLine ("@@ OK TRY TO CALL ListNames with " + listFunctions.Count);
				names = ListNames(listFunctions);
			} else {
				Console.WriteLine ("@@ BUUUUU:");
				names = null;
				return Errno.ENOENT;
			}
		    return 0;
		}

		private IEnumerable<DirectoryEntry> ListNames (string directory)
		{
			foreach (string name in names) {
				yield return new DirectoryEntry (name.Substring (1));
			}
		}

		private IEnumerable<DirectoryEntry> ListNames (SortedList<string,Index> list)
		{
			foreach (Index name in list.Values) {
				yield return new DirectoryEntry (name.ToString());
			}
		}

		private IEnumerable<DirectoryEntry> ListNames (SortedList<string,View> list)
		{
			foreach (View name in list.Values) {
				yield return new DirectoryEntry (name.ToString());
			}
		}

		private IEnumerable<DirectoryEntry> ListNames (SortedList<string,Sequence> list)
		{
			foreach (Sequence name in list.Values) {
				yield return new DirectoryEntry (name.ToString());
			}
		}

		private IEnumerable<DirectoryEntry> ListNames (SortedList<string,Table> list)
		{
			foreach (Table name in list.Values) {
				yield return new DirectoryEntry (name.ToString());
			}
		}

		private IEnumerable<DirectoryEntry> ListNames (SortedList<string,Function> list)
		{
			Console.WriteLine ("CHK FIRST CALL");
			foreach (Function name in list.Values) {
				Console.WriteLine ("CHK NAME FUNCTIONS: " + name.ToString ());
				yield return new DirectoryEntry (name.ToString());
			}
		}

		private IEnumerable<DirectoryEntry> ListNames (SortedList<string,fsgateway.Constraint> list)
		{
			foreach (fsgateway.Constraint name in list.Values) {
				yield return new DirectoryEntry (name.ToString());
			}
		}

		public Errno OnGetPathStatus (string path, out Stat stbuf)
		{
			stbuf = new Stat ();

			stbuf.st_uid = Mono.Unix.Native.Syscall.getuid();
			stbuf.st_gid = Mono.Unix.Native.Syscall.getgid();

			Mono.Unix.Native.Timeval timeval;
			Mono.Unix.Native.Syscall.gettimeofday(out timeval);
			stbuf.st_mtime = timeval.tv_sec;
			
			if (path == "/") {
				stbuf.st_mode  = NativeConvert.FromUnixPermissionString ("dr-xr-xr-x");
				stbuf.st_nlink = 1;
				return 0;
			}
			if (path.IndexOf('/',1)<0) {
				if (!names.Contains(path)) {
					return Errno.ENOENT;
				}
				stbuf.st_mode  = NativeConvert.FromUnixPermissionString ("dr-xr-xr-x");
				stbuf.st_nlink = 1;
				return 0;
			} else {
				stbuf.st_mode = NativeConvert.FromUnixPermissionString ("-r--r--r--");
				if (path.StartsWith("/indexes")) {
					string file=path.Substring(path.IndexOf("/",1)+1);
					if (listIndexes.ContainsKey(file)) {
						Index index=listIndexes[file];
						stbuf.st_size=index.Script.Length;
					} else {
						return Errno.ENOENT;
					}
				} else if (path.StartsWith("/views")) {
					string file=path.Substring(path.IndexOf("/",1)+1);
					if (listViews.ContainsKey(file)) {
						View view=listViews[file];
						stbuf.st_size=view.Script.Length;
					} else {
						return Errno.ENOENT;
					}
				} else if (path.StartsWith("/sequences")) {
					string file=path.Substring(path.IndexOf("/",1)+1);
					if (listSequences.ContainsKey(file)) {
						Sequence sequence=listSequences[file];
						stbuf.st_size=sequence.Script.Length;
					} else {
						return Errno.ENOENT;
					}
				} else if (path.StartsWith("/tables")) {
					string file=path.Substring(path.IndexOf("/",1)+1);
					if (listTables.ContainsKey(file)) {
						Table table=listTables[file];
						stbuf.st_size=table.Script.Length;
					} else {
						return Errno.ENOENT;
					}
				} else if (path.StartsWith("/functions")) {
					string file=path.Substring(path.IndexOf("/",1)+1);
					if (listFunctions.ContainsKey(file)) {
						Function function=listFunctions[file];
						stbuf.st_size=function.Script.Length;
					} else {
						return Errno.ENOENT;
					}					                           
				} else if (path.StartsWith("/constraints")) {
					string file=path.Substring(path.IndexOf("/",1)+1);
					if (listConstraints.ContainsKey(file)) {
						fsgateway.Constraint constraint=listConstraints[file];
						stbuf.st_size=constraint.Buffer.Length;
					} else {
						return Errno.ENOENT;
					}					                           
				} else {
					stbuf.st_mode = NativeConvert.FromUnixPermissionString ("-r--r--r--");
				}
			}
			return 0;
		}

		
		public Errno OnReadHandle (string file, OpenedPathInfo info, byte[] buf, long offset, [System.Runtime.InteropServices.Out] out int bytesWritten) {

			// Check the type of file
			bytesWritten=0;
			
			try {
				if (file.StartsWith("/indexes")) {
					string name=file.Substring(file.IndexOf("/",1)+1);
					if (listIndexes.ContainsKey(name)) {
						Index index=listIndexes[name];
						index.Buffer.CopyTo(buf,offset);
						bytesWritten=System.Math.Min(buf.Length,index.Buffer.Length-(int)offset);
						
					} else {
						return Errno.ENOENT;
					}
				} else if (file.StartsWith("/views")) {
					string name=file.Substring(file.IndexOf("/",1)+1);
					if (listViews.ContainsKey(name)) {
						View view=listViews[name];
						view.Buffer.CopyTo(buf,offset);
						bytesWritten=System.Math.Min(buf.Length,view.Buffer.Length-(int)offset);
					} else {
						return Errno.ENOENT;
					}
				} else if (file.StartsWith("/sequences")) {
					string name=file.Substring(file.IndexOf("/",1)+1);
					if (listSequences.ContainsKey(name)) {
						Sequence sequence=listSequences[name];
						sequence.Buffer.CopyTo(buf,offset);
						bytesWritten=System.Math.Min(buf.Length,sequence.Buffer.Length-(int)offset);
					} else {
						return Errno.ENOENT;
					}
				} else if (file.StartsWith("/tables")) {
					string name=file.Substring(file.IndexOf("/",1)+1);
					if (listTables.ContainsKey(name)) {
						Table table=listTables[name];
						table.Buffer.CopyTo(buf,offset);
						bytesWritten=System.Math.Min(buf.Length,table.Buffer.Length-(int)offset);
					} else {
						return Errno.ENOENT;
					}
				} else if (file.StartsWith("/constraints")) {
					string name=file.Substring(file.IndexOf("/",1)+1);
					if (listConstraints.ContainsKey(name)) {
						fsgateway.Constraint constraint=listConstraints[name];
						constraint.Buffer.CopyTo(buf,offset);
						bytesWritten=System.Math.Min(buf.Length,constraint.Buffer.Length-(int)offset);
					} else {
						return Errno.ENOENT;
					}
				} else if (file.StartsWith("/functions")) {
					Console.WriteLine("@@@@ CHK 1");
					string name=file.Substring(file.IndexOf("/",1)+1);
					if (listFunctions.ContainsKey(name)) {
						Console.WriteLine("@@@@ CHK 2");
						Function function=listFunctions[name];
						function.Buffer.CopyTo(buf,offset);
						bytesWritten=System.Math.Min(buf.Length,function.Buffer.Length-(int)offset);
					} else {
						Console.WriteLine("@@@@ CHK 3");
						return Errno.ENOENT;
					}
				}
			} catch (Exception ex) {
				System.Console.Out.WriteLine("Exception. Message: "+ex.Message);
			}
			return 0;
		}
	
	}
}
