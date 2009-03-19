using System;
using System.Collections.Generic;
using Mono.Unix.Native;
using Mono.Fuse;

namespace FsGateway
{
	
	public interface IFsGateway:IDisposable
	{
		string Usage  { get; }
		string connectionParameter {get ; }
		string storageType {
			get ;
		}
		
		bool Connect();
		bool Connect(string parameter);
		void Unconnect();
		bool isConnect { get; }
		
		Errno OnReadDirectory (string directory
		                      ,OpenedPathInfo info
		                      ,out IEnumerable<DirectoryEntry> names);
		Errno OnGetPathStatus (string path
		                      ,ref Stat stbuf);
		Errno OnReadHandle (string file
		                   ,OpenedPathInfo info
		                   ,byte[] buf
		                   ,long offset
		                   ,out int bytesWritten);
		Errno OnReadSymbolicLink (string link, out string target);

	}
}
