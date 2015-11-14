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
