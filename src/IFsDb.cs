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
using System.Data;
using System.Collections.Generic;

namespace FsGateway
{
	public interface IFsDb:IFsModule
	{
		
		// This is necessary to get the list of type of object that the database can handle (eg. Tables, Indexes, Sequences, Views, etc)
		List<string> getTypeOfObjects();
		
		SortedList<string,Table> getTables();
		SortedList<string,View> getViews();
		SortedList<string,Index> getIndexes();
		SortedList<string,Sequence> getSequences();
		SortedList<string,Function> getFunctions();
	}
}
