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

		public FsDbManager()
		{
			names.Add ("/tables");
		    names.Add ("/views");
		    names.Add ("/indexes");
		    names.Add ("/sequences");
		}
				
		public FsDbManager (string connectionString, string[] args)
		{
			names.Add ("/tables");
		    names.Add ("/views");
		    names.Add ("/indexes");
		    names.Add ("/sequences");
			
			this.connectionString=connectionString;
			IFsDb pg=new Postgresql();
			pg.Connect(connectionString);
			this.db=pg;
		}

		public void Dispose() {
			if (db!=null)
				db.Unconnect();
				
			db=null;
		}
		
		public bool Connect() {
			if (db==null) {
				if (connectionString!=null && connectionString.Length>0) {
					IFsDb pg=new Postgresql();
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
					res=db.isConnected;
				
				return res;
			}
		}
		
		public string storageType {
			get {
				return "postgres";
			}
		}

		public string Usage {
			get {
				return "Specify the connection parameter like this one: \"Server=localhsot; Database=mydb; User ID=username;Password=password;Port=5432;";
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
			System.Console.Out.WriteLine("DEBUG: OnReadDirectory on "+directory);
			
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
			} else {
				names = null;
				return Errno.ENOENT;
			}
		    return 0;
		}

		private IEnumerable<DirectoryEntry> ListNames (string directory)
		{
			System.Console.Out.WriteLine("DEBUG: ListName for "+directory);
			foreach (string name in names) {
				System.Console.Out.WriteLine("-name="+name);
				yield return new DirectoryEntry (name.Substring (1));
			}
		}

		private IEnumerable<DirectoryEntry> ListNames (SortedList<string,Index> list)
		{
			System.Console.Out.WriteLine("DEBUG: ListName for indexes");
			foreach (Index name in list.Values) {
				yield return new DirectoryEntry (name.ToString());
			}
		}

		private IEnumerable<DirectoryEntry> ListNames (SortedList<string,View> list)
		{
			System.Console.Out.WriteLine("DEBUG: ListName for views");
			foreach (View name in list.Values) {
				yield return new DirectoryEntry (name.ToString());
			}
		}

		private IEnumerable<DirectoryEntry> ListNames (SortedList<string,Sequence> list)
		{
			System.Console.Out.WriteLine("DEBUG: ListName for sequence");
			foreach (Sequence name in list.Values) {
				yield return new DirectoryEntry (name.ToString());
			}
		}

		private IEnumerable<DirectoryEntry> ListNames (SortedList<string,Table> list)
		{
			System.Console.Out.WriteLine("DEBUG: ListName for table");
			foreach (Table name in list.Values) {
				yield return new DirectoryEntry (name.ToString());
			}
		}

		public Errno OnGetPathStatus (string path, ref Stat stbuf)
		{
			System.Console.Out.WriteLine("DEBUG: OnGetPathStatus for "+path+" UID="+Mono.Unix.Native.Syscall.getuid()+" GID="+Mono.Unix.Native.Syscall.getgid());			

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
				} else {
					stbuf.st_mode = NativeConvert.FromUnixPermissionString ("-r--r--r--");
				}
			}
			return 0;
		}

		
		public Errno OnReadHandle (string file, OpenedPathInfo info, byte[] buf, long offset, [System.Runtime.InteropServices.Out] out int bytesWritten) {
			// Check the type of file
			System.Console.Out.WriteLine("DEBUG: OnReadHandle for "+file);			
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
				}
			} catch (Exception ex) {
				System.Console.Out.WriteLine("Exception. Message: "+ex.Message);
			}
			return 0;
		}
	
	}
}
