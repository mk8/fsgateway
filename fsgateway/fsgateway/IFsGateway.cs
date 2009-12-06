using System;
using System.Collections.Generic;
using Mono.Unix.Native;
using Mono.Fuse;

namespace FsGateway
{
	
	public interface IFsGateway:IFsModule
	{
		string connectionParameter {get ; }
		
		bool Connect();
		
		Errno OnReadDirectory (string directory
		                      ,OpenedPathInfo info
		                      ,out IEnumerable<DirectoryEntry> names);
		Errno OnGetPathStatus (string path
		                      ,out Stat stbuf);
		Errno OnReadHandle (string file
		                   ,OpenedPathInfo info
		                   ,byte[] buf
		                   ,long offset
		                   ,out int bytesWritten);
		Errno OnReadSymbolicLink (string link, out string target);

	}
}
