using System;
using Mono.Unix.Native;
using System.Collections.Generic;
using Mono.Fuse;

namespace FsGateway
{
	public class FuseWrapper:Mono.Fuse.FileSystem
	{
		IFsGateway gw=null;
		
		public FuseWrapper(IFsGateway gw,string[] args)
		{
			// Check if the gateway is set
			if (gw!=null) {
				
				// Check if the gateway object is connected
				if (!gw.isConnect) {
					bool res=gw.Connect();
					
					// If the system is not able to connect the gw object 
					if (res) {
						this.gw=gw;
					}
				} else {
					this.gw=gw;
				}
			}
			
			// Check if the fuse argument array contains at least 1 parameter, the mount point otherwise generate exception
			if (args.Length>0) {
				ParseFuseArguments(args);
				MountPoint=args[args.Length-1];
			} else {
				throw new Exception("The fuse mount point is not specified");
			}
		}

		public string storageType {
			get {
				return "tagfs";
			}
		}
		
		public IFsGateway gateway {
			get {
				return gw;
			}
			set {
				// Check if the gateway is already setuped
				if (gw!=null) {
					if (gw.isConnect) {
						gw.Unconnect();
					}
					gw.Dispose();
					gw=null;
				}

				
				// Check if the object to set is not null
				if (value!=null) {
					gw=value;
					if (!gw.isConnect) {
						bool res=gw.Connect();
						if (res==false) {
							throw new Exception("Unable to connect");
						}
					}
				}
			}
		}

		protected override Errno OnReadDirectory (string directory
		                                         ,OpenedPathInfo info
		                                         ,out IEnumerable<DirectoryEntry> names)
		{
			return gw.OnReadDirectory(directory, info, out names);
		}
		
		protected Errno OnGetPathStatus (string path
		                                         ,ref Stat stbuf) {
			return gw.OnGetPathStatus (path, ref stbuf);
		}
		
		protected override Errno OnReadHandle (string file
                                              ,OpenedPathInfo info
                                              ,byte[] buf
                                              ,long offset
                                              ,out int bytesWritten)
		{
			return gw.OnReadHandle (file
		                                      ,info
		                                      ,buf
		                                      ,offset
		                                      ,out bytesWritten);
		}

		protected override Errno OnReadSymbolicLink (string link, out string target) {
			return gw.OnReadSymbolicLink(link,out target);
		}

	}
}
