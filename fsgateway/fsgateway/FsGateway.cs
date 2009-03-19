using System;
using System.Collections.Generic;
using Mono.Fuse;
using Mono.Unix.Native;

namespace FsGateway
{
	class FsGateway:FileSystem
	{
//		private List<string> names = new List<string> ();
/*		
		private SortedList<string,Index> listIndexes=null;
		private SortedList<string,View> listViews=null;
		private SortedList<string,Sequence> listSequences=null;
		private SortedList<string,Table> listTables=null;
		
		public FsGateway (IFsDb pg, string[] args) : base (args)
		{
			names.Add ("/tables");
		    names.Add ("/views");
		    names.Add ("/indexes");
		    names.Add ("/sequences");
			
			this.pg=pg;
		}
		
		protected override Errno OnReadDirectory (string directory, OpenedPathInfo info,
		                                          out IEnumerable<DirectoryEntry> names)
		{
			System.Console.Out.WriteLine("DEBUG: OnReadDirectory on "+directory);
			
			// Check for root directory
			if (directory.Equals("/")) {
				names = ListNames (directory);
			} else if (directory.Equals("/tables")) {
				listTables=pg.getTables();
				names = ListNames(listTables);
			} else if (directory.Equals("/views")) {
				listViews=pg.getViews();
				names = ListNames(listViews);
			} else if (directory.Equals("/indexes")) {
				listIndexes=pg.getIndexes();
				names = ListNames(listIndexes);
			} else if (directory.Equals("/sequences")) {
				listSequences=pg.getSequences();
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

		protected Errno OnGetPathStatus (string path, ref Stat stbuf)
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

		
		protected override Errno OnReadHandle (string file, OpenedPathInfo info, byte[] buf, long offset, [System.Runtime.InteropServices.Out] out int bytesWritten) {
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
*/		
		public static void PrintUsage(List<Type> modules) {
			IFsGateway fsGw;
			System.Console.Out.WriteLine("FsGateway version 0.0.1.");
			System.Console.Out.WriteLine("FsGateway usage\n");
			System.Console.Out.WriteLine("\tmono fsgateway storagetype connection_string [fuse_option] mountpoint\n");
			System.Console.Out.WriteLine("Where:");
			foreach (Type type in modules) {
				System.Reflection.ConstructorInfo ci=type.GetConstructor(new Type[0]);
				if (ci!=null) {
					fsGw=(IFsGateway)ci.Invoke(null);
					System.Console.Out.WriteLine("\t"+fsGw.storageType+"\t"+fsGw.Usage);
				}
			}

//			System.Console.Out.WriteLine("\tstoragetype\tis the type of storage. For now is postgresql");
//			System.Console.Out.WriteLine("\tconnection_string\tis the string used to connect to the dbms");
//			System.Console.Out.WriteLine("\tfuse_option\tis additional option passed to fuse");
//			System.Console.Out.WriteLine("\tmountpoint\tis where the new filesystem will be mounted\n\n");
		}
		
		public static void Main(string[] args)
		{
			List<Type> modules=new List<Type>();
			IFsGateway fsGw=null;
			
			// Check for all the IFsGateway implementation in the main assemply
			FsGateway obj=new FsGateway();			
			Type[] types=obj.GetType().Assembly.GetTypes();
			foreach (Type type in types) {
				if (type.GetInterface("IFsGateway")!=null) {
					modules.Add(type);
				}
			}

			// Check the number of parameter
			if (args.Length<3) {
				FsGateway.PrintUsage(modules);
				System.Environment.Exit(1);
			}
			
			string storageType=args[0];
			bool foundIt=false;
			foreach (Type gw in modules) {
				System.Reflection.ConstructorInfo ci=gw.GetConstructor(new Type[0]);
				if (ci==null) {
					return;
				}
				
				fsGw=(IFsGateway)ci.Invoke(null);
				if (fsGw.storageType.Equals(storageType)) {
					foundIt=true;
					break;
				}
			}

			if (!foundIt) {
				FsGateway.PrintUsage(modules);
			} else {

				// Purge already used params
				string[] arg=new string[args.Length-2];
				Array.Copy(args,2,arg,0,arg.Length);
				fsGw.Connect(args[1]);
			
				using (FuseWrapper fw = new FuseWrapper(fsGw,arg)) {
					fw.Start ();
				}
			}
		}
	}
}

