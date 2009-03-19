using System;
using System.Data;
using System.Collections.Generic;

namespace FsGateway
{
	public interface IFsDb:IDisposable
	{
		bool Connect(string stringConnection); // Default method to connect to the database
		bool Unconnect(); // Default method to unconnect to the database
		bool isConnected { 
			get ; 
		}
		
		SortedList<string,Table> getTables();
		SortedList<string,View> getViews();
		SortedList<string,Index> getIndexes();
		SortedList<string,Sequence> getSequences();
	}
}
