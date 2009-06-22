using System;
using System.Collections.Generic;
using Mono.Unix.Native;
using Mono.Fuse;

namespace FsGateway
{
	
	public interface IFsModule:IDisposable
	{
		bool Connect(string parameter);
		void Unconnect();
		bool isConnect { get; }
		string storageType { get ;}
		string Usage  { get; }
	}
}