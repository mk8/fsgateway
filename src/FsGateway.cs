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
using Mono.Fuse;
using Mono.Unix.Native;

namespace FsGateway
{
	class FsGateway:FileSystem
	{
		public static void PrintUsage(List<Type> modules) {
			IFsModule fsModule;
			SortedList<String, String> modulesDescription = new SortedList<string, string> ();

			String appName = System.AppDomain.CurrentDomain.FriendlyName;

			System.Console.Out.WriteLine("FsGateway version 0.1.2.0");
			System.Console.Out.WriteLine("FsGateway usage\n");
			System.Console.Out.WriteLine("\tmono " + appName + " storagetype connection_string [fuse_option] mountpoint\n");
			System.Console.Out.WriteLine("Where:");
			foreach (Type type in modules) {
				System.Reflection.ConstructorInfo ci=type.GetConstructor(new Type[0]);
				if (ci!=null) {
					fsModule=(IFsModule)ci.Invoke(null);
					if (fsModule.storageType!=null) {
						modulesDescription.Add (fsModule.storageType, fsModule.Usage);
					}
				}
			}

			IEnumerator<KeyValuePair<String, String>> en = modulesDescription.GetEnumerator ();
			while (en.MoveNext()) {
				System.Console.Out.WriteLine ("\t" + en.Current.Key + "\n\t\t" + en.Current.Value + "\n");
			}
		}
		
		public static void Main(string[] args)
		{
			List<Type> modules=new List<Type>();
			IFsModule fsModule=null;
			
			// Check for all the IFsGateway implementation in the main assemply
			FsGateway obj=new FsGateway();			
			Type[] types=obj.GetType().Assembly.GetTypes();
			foreach (Type type in types) {
				if (type.GetInterface("IFsGateway")!=null || type.GetInterface("IFsDb")!=null) {
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
				
				fsModule=(IFsModule)ci.Invoke(null);
				if (fsModule.storageType!=null && fsModule.storageType.Equals(storageType)) {
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
				fsModule.Connect(args[1]);

				IFsGateway gw=null;
				if (fsModule.GetType().GetInterface("IFsGateway")!=null) {
					gw = (IFsGateway) fsModule;
				} else {
					gw=new FsDbManager((IFsDb)fsModule);
				}

				using (FuseWrapper fw = new FuseWrapper(gw,arg)) {
					fw.Start ();
				}
			}
		}
	}
}

