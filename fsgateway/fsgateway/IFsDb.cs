using System;
using System.Data;
using System.Collections.Generic;

namespace FsGateway
{
	public interface IFsDb:IFsModule
	{
		SortedList<string,Table> getTables();
		SortedList<string,View> getViews();
		SortedList<string,Index> getIndexes();
		SortedList<string,Sequence> getSequences();
	}
}
