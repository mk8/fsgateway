/* 
 * FsGateway - navigate a database structure as directory tree
 * Copyright (C) 2009-2016 Torello Querci <tquerci@gmail.com>
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
using System.IO;
using Mono.Unix.Native;
using System.Collections.Generic;
using Mono.Fuse;
using System.Text.RegularExpressions;

namespace FsGateway
{
	
	public class SugarDatastore : IFsGateway
	{
		private SortedList<string,SortedList<string,List<string>>> listTags=new SortedList<string,SortedList<string,List<string>>>();
		private string datastoreRoot="";
		private DateTime lastUpdate=DateTime.MinValue;

		public SugarDatastore()
		{
			datastoreRoot=null;
		}

		public SugarDatastore(string homeDir, string[] args) 
		{
			// Normalize HomeDir
			homeDir=NormalizeHomeDir(homeDir);

			datastoreRoot=homeDir;
			ParseDir(datastoreRoot);
		}

		public void Dispose() {
		}
		
		public bool Connect() {
			return false;
		}

		public bool Connect(string parameter) {
			
			bool ret;
			SortedList<string,SortedList<string,List<string>>> oldListTags=listTags;
			string oldRoot=datastoreRoot;

			// Normalize HomeDir
			parameter=NormalizeHomeDir(parameter);

			datastoreRoot=parameter;
			ret=ParseDir(parameter);
			if (ret) {
				datastoreRoot=parameter;
			} else {
				listTags=oldListTags;
				datastoreRoot=oldRoot;
			}
			return ret;
		}

		public string connectionParameter {
			get {
				return datastoreRoot;
			}
		}
		
		public void Unconnect() {
			datastoreRoot=null;
		}

		private string NormalizeHomeDir(string homeDir) {
			// Check to full path homedir
			if (!Path.IsPathRooted(homeDir)) {
				homeDir = Directory.GetCurrentDirectory()+Path.DirectorySeparatorChar+homeDir;
			}

			// Check if target directory exist			
			if (!System.IO.Directory.Exists(homeDir)) {
				throw new Exception("Target directory "+homeDir+" doesn't exist");
			}
			return homeDir;
		}
		
		public string Usage {
			get {
				return "You need to specify the directory used by the Sugar datastore.";
			}
		}
		
		public string storageType {
			get {
				return "sugar_datastore";
			}
		}

		public bool isConnect {
			get {
				return true;
			}
		}
		
		public Errno OnReadHandle (string file
		                   ,OpenedPathInfo info
		                   ,byte[] buf
		                   ,long offset
		                   ,out int bytesWritten) {
			bytesWritten=0;
			return Errno.ENODATA;
		}		

		/*********************************************************************
		 * 
		 * Method to get the list of tags that are embedded in the directory path.
		 * This method setup also the out variable contents where will stored all
		 * the SortedList where the key is the filename and the value is the list 
		 * of all the file with full path that have the same name
		 * 
		 *********************************************************************/
		 private List<string> getTagListAndContents(string directory, out SortedList<string,List<string>> contents) {

			// Expand path as list of tags
			string[] tags=directory.Substring(1).Split('/');
			SortedList<string,List<string>> contentsPurged=null;
			List<string> tagsList=new List<string>();
			contents=null;
			foreach (string tag in tags) {
					
				// Check if is the first look
				if (contents==null) {
					contents=new SortedList<string,List<string>>();
					foreach (string keyListTags in listTags[tag].Keys) {
						contents.Add(keyListTags,listTags[tag][keyListTags]);
					}
						
				} else {
					contentsPurged=new SortedList<string,List<string>>();
					foreach (string keyContents in contents.Keys) {
						if (listTags[tag].ContainsKey(keyContents)) {
							contentsPurged.Add(keyContents,contents[keyContents]);
						}
					}
						
					contents=contentsPurged;

					if (contents.Count<1) {
						break;
					}
				}
				tagsList.Add(tag);
			}
			
			return tagsList;
		}
		
		public Errno OnReadDirectory (string directory, OpenedPathInfo info,
		                                          out IEnumerable<DirectoryEntry> names)
		{
			names=null;

			// Update the files list.
			ParseDir(datastoreRoot);

			// Check for root directory
			if (directory.Equals("/")) {
				
				// Read all the directory tag
				IEnumerator<string> en=listTags.Keys.GetEnumerator();
				names=ListNames(en);

			} else {
				// Expand path as list of tags
				SortedList<string,List<string>> contents=null;
				List<string> tagsList=this.getTagListAndContents(directory,out contents);

				IEnumerator<string> en;
				
				// Add other tags
				if (contents==null) {
					contents=new SortedList<string,List<string>>();
				}

				if (contents.Count>0) {
					en=listTags.Keys.GetEnumerator();
					// Loop for all tags
					while (en.MoveNext()) {
						
						// Check in this tags is already in the tag list
						if (!tagsList.Contains(en.Current)) {
							
							// Check if this tag is already specify in the contents (file list)
							// We need modify this behaviour later
							if (!contents.ContainsKey(en.Current)) {
								
								// Check if this tag have some files with actual path
								bool checkDir=false;
								foreach (string filename in contents.Keys) {
									if (listTags[en.Current].ContainsKey(filename)) {
										checkDir=true;
										break;
									}
								}
								if (checkDir) {
									contents.Add(en.Current,null);
								}
							}
						}
					}
				}

				en=contents.Keys.GetEnumerator();
				List<string> simbolicName=new List<string>();
				IEnumerator<string> en_link=null;
				while (en.MoveNext()) {
					int count=1;
					if (contents[en.Current]!=null) {
						en_link=contents[en.Current].GetEnumerator();
						while (en_link.MoveNext()) {
							while (simbolicName.Contains(en.Current + (count>1 ? " ("+count+")" : ""))) {
								++count;
							}
							simbolicName.Add(en.Current + (count>1 ? " ("+count+")" : ""));

						}
					} else {
						// Suppose to be a directory (TAG)
						simbolicName.Add(en.Current + (count>1 ? " ("+count+")" : ""));
					}

				}
				en_link=simbolicName.GetEnumerator();
				names=ListNames(en_link);
			}
			return 0;
		}

		public Errno OnGetPathStatus (string path, out Stat stbuf)
		{

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
			if (path.IndexOf('/',1)<=0) {
				if (!this.listTags.ContainsKey(path.Substring(1))) {
					return Errno.ENOENT;
				}

				stbuf.st_mode  = NativeConvert.FromUnixPermissionString ("dr-xr-xr-x");
					stbuf.st_nlink = 1;
				return 0;
			} else {
				
				// Path split to identify if the path is a directory of it is a file
				string[] tags=path.Substring(1).Split('/');
				bool isDirectory=true;
				foreach (string tag in tags) {
					if (!listTags.ContainsKey(tag)) {
						isDirectory=false;
						break;
					}
				}

				if (isDirectory) {
					stbuf.st_mode  = NativeConvert.FromUnixPermissionString ("dr-xr-xr-x");
					stbuf.st_nlink = 1;
				} else {
					stbuf.st_mode = NativeConvert.FromUnixPermissionString ("lrwxrwxrwx");
				}
			}
			return 0;
		}

		private IEnumerable<DirectoryEntry> ListNames (IEnumerator<string> directories)
		{
			while (directories.MoveNext()) {
				yield return new DirectoryEntry (directories.Current.Replace('/','\\'));
			}
		}

		public Errno OnReadSymbolicLink (string link, out string target) {
			
			Regex fileMultipleRegex = null;
			
			try {
				fileMultipleRegex = new Regex(@"(?<filename>.+)(\((?<number>[0-9]+)\)$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
//				fileMultipleRegex = new Regex(@"(?<filename>.*)(\((?<number>[0-9]+)\)$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			} catch (Exception ex) {
				System.Console.WriteLine("Exception during the Regex creation: "+ex.Message);
			}
			
			// Expand path as list of tags
			string[] srcTags=link.Substring(1).Split('/');
			string[] tags=new string[srcTags.Length-1];
			Array.Copy(srcTags,tags,tags.Length);

			string file=srcTags[srcTags.Length-1];
						
			// Check for namefile with number
			int index=0;
			if (fileMultipleRegex!=null) {

				Match match=null;
				try {
				match = fileMultipleRegex.Match(file);
				} catch (Exception ex) {
					System.Console.WriteLine("Exception during match routine. Message: "+ex.Message);
				}

				try {
					if (match != null && match.Success) {					
						index=System.Int32.Parse(match.Groups["number"].Value)-1;
						file=match.Groups["filename"].Value;
						file=file.Substring(0,file.Length-1);
					}
				} catch (Exception ex) {
					System.Console.WriteLine("Exception generic: "+ex.Message);
				}
					
			}
						
			List<string> contents=null;
			List<string> contentsMerged=null;
			IEnumerator<string> en;
			
			foreach (string tag in tags) {
				
				// Check if is the first look
				if (contents==null) {
					contents=new List<string>();
					en=listTags[tag][file].GetEnumerator();
					while (en.MoveNext()) {
						contents.Add(en.Current);
					}
				} else if (!tag.Equals(file)) {
					contentsMerged=new List<string>();
						
					en=listTags[tag][file].GetEnumerator();
					while (en.MoveNext()) {
						string key=en.Current;
						if (contents.Contains(key)) {
							contentsMerged.Add(key);
						}
					}
					contents=contentsMerged;
				}
			}

			if (contents.Count>0) {
				target=contents[index];
			} else {
				target="";
				return Errno.ENOENT;
			}
			return 0;
		}
			
			
		private void dumpStructure() {
			IEnumerator<string> en=listTags.Keys.GetEnumerator();
			
			while (en.MoveNext()) {
				System.Console.Out.WriteLine("TAG: "+en.Current);
				SortedList<string,List<string>> element=listTags[en.Current];
				IEnumerator<string> en_link=element.Keys.GetEnumerator();
				while (en_link.MoveNext()) {
					System.Console.Out.WriteLine("\t"+en_link.Current+" => ");
					foreach (string destination_file in element[en_link.Current]) {
						System.Console.Out.WriteLine("\t\t"+destination_file);
					}
				}
			}
		}
		
		private bool ParseDir(string dir) {
		
			// Check for last update
			if (lastUpdate.AddTicks(100000) > DateTime.Now) {
				return true;
			}
			
			Dirent dirent=null;

			if (!dir.StartsWith(datastoreRoot)) {
				System.Console.Out.WriteLine("Error: "+dir+" is not child of "+datastoreRoot);
				return false;
			}
						
			this.listTags["Sugar"]=new SortedList<string,List<string>>();

			// Analyze datastore directory to looking for datastore files
			if (dir.Length==datastoreRoot.Length) {
				
				System.IntPtr dirHandle=Syscall.opendir(dir);
				if (dirHandle!=System.IntPtr.Zero) {

					while ((dirent=Syscall.readdir(dirHandle))!=null) {

						if (!dirent.d_name.Equals(".") && !dirent.d_name.Equals("..")) {
							
							// Check if the directory is a datastore main data directory
							if (dirent.d_name.Length==2) {
								// Examinate datastore files
								ExaminateDir(dir+"/"+dirent.d_name);
							}
						}
					}
					Syscall.closedir(dirHandle);
				}
			}
			
			// Update last update timestamp
			lastUpdate=DateTime.Now;
			
			return true;
		}
		
		private bool ExaminateDir(string dir) {
			string name="";
			Dirent dirent=null;
			Stat buf;

			System.IntPtr dirHandle=Syscall.opendir(dir);
			if (dirHandle!=IntPtr.Zero) {
				buf=new Stat();
				while ((dirent=Syscall.readdir(dirHandle))!=null) {
					name=dir+"/"+dirent.d_name;
					if (!dirent.d_name.Equals(".") && !dirent.d_name.Equals("..")) {

						
						// Check if the directory is a datastore main data directory
						Syscall.lstat(name,out buf);
						if ((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IFDIR)!=0) {
							// Examinate datastore files
							ExaminateDatastoreEntry(name);
						}
					}
				}
				Syscall.closedir(dirHandle);
			}

			return true;
		}

		private bool ExaminateDatastoreEntry(string datastoreEntry) {
			string name="";
			Dirent dirent=null;
			Stat buf;
			List<string> element=null;

			System.IntPtr dirHandle=Syscall.opendir(datastoreEntry);
			if (dirHandle != System.IntPtr.Zero) {
				buf=new Stat();
				while ((dirent=Syscall.readdir(dirHandle))!=null) {
					if (!dirent.d_name.Equals(".") && !dirent.d_name.Equals("..")) {

						// Check if the directory is a datastore main data directory
						name=datastoreEntry+"/"+dirent.d_name;
						Syscall.lstat(name,out buf);
						if ((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IFREG)!=0) {
							OlpcMetadata metadata=new OlpcMetadata(datastoreEntry);

							string entryName=metadata.ToString();
							if (listTags["Sugar"].ContainsKey(entryName)) {
								listTags["Sugar"][entryName].Add(name);
							} else {
								element=new List<string>();
								element.Add(name);
								listTags["Sugar"].Add(entryName, element);
							}
							
							// Loop on all the tags
							if (metadata.tags != null) {
								IEnumerator<string> en_tags=metadata.tags.GetEnumerator();
								while (en_tags.MoveNext()) {
									
									// Check if this tag is present on the system
									string tag=en_tags.Current;
									
									if (listTags.ContainsKey(tag) ) {
										if (listTags[tag].ContainsKey(entryName)) {
											listTags[tag][entryName].Add(name);
										} else {
											element=new List<string>();
											element.Add(name);
											listTags[tag].Add(entryName, element);
										}
										
									} else {
										element=new List<string>();
										element.Add(name);
										listTags[tag]=new SortedList<string,List<string>>();
										listTags[tag].Add(entryName, element);
									}
								}
							}
						}
					}
				}
				Syscall.closedir(dirHandle);
			}
			return true;
		}

	}
}
