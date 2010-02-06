/* 
 * FsGateway - navigate a database structure as directory tree
 * Copyright (C) 2009-2010 Torello Querci <torello@torosoft.com>
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
using Mono.Unix.Native;
using System.Collections.Generic;
using Mono.Fuse;
using System.Text.RegularExpressions;

namespace FsGateway
{
	
	
	public class DirAsTag: IFsGateway
	{

		private SortedList<string,SortedList<string,List<string>>> listTags=new SortedList<string,SortedList<string,List<string>>>();
		private string root="";
		
		public DirAsTag() 
		{
			root=null;
		}
		
		public DirAsTag(string homeDir, string[] args) 
		{
			root=homeDir;
			ParseDir(root);
		}
		
		public void Dispose() {
		}
		
		public bool Connect() {
			return false;
		}
		
		public bool Connect(string parameter) {
			
			bool ret;
			SortedList<string,SortedList<string,List<string>>> oldListTags=listTags;
			string oldRoot=root;
			
			root=parameter;
			ret=ParseDir(parameter);
			if (ret) {
				root=parameter;
			} else {
				listTags=oldListTags;
				root=oldRoot;
			}
			return ret;
		}

		public string connectionParameter {
			get {
				return root;
			}
		}
		
		public void Unconnect() {
			root=null;
		}
		
		public string Usage {
			get {
				return "You need to specify the directory entry point where this fs start to examine.";
			}
		}
		
		public string storageType {
			get {
				return "tagfs";
			}
		}
		
		public bool isConnect {
			get {
				return true;
			}
		}
		
		private bool ParseDir(string dir) {
			
			string name="";
			
			if (!dir.StartsWith(root)) {
				System.Console.Out.WriteLine("Error: "+dir+" is not child of "+root);
				return false;
			}
			
			string suffix=null;
			string[] keys=null;
			if (dir.Length!=root.Length) {
				
				suffix=dir.Substring(root.Length);
				if (suffix[0]=='/') {
					suffix=suffix.Substring(1);
				}
				keys=suffix.Split('/');
			}
						
			System.IntPtr dirHandle=Syscall.opendir(dir);
			if (dirHandle!=null) {
				
				Dirent dirent=null;
				Stat buf=new Stat();
				while ((dirent=Syscall.readdir(dirHandle))!=null) {
					if (!dirent.d_name.Equals(".") && !dirent.d_name.Equals("..")) {
						
						name=dir+"/"+dirent.d_name;

						Syscall.lstat(name,out buf);
						if ((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IFDIR)!=0) {
							// This file is a directory
						
							// Check if the directory is in the list
							if (!listTags.ContainsKey(dirent.d_name)) {
								// Directory not present in the list
								listTags.Add(dirent.d_name,new SortedList<string,List<string>>());
							}
						
							// Analyze the files contained in the directory
							ParseDir(name);

						} else {
							
							// Check if you are in the root directory, so jump all the files present in the root directory
							if (keys!=null) {
								
								// Otherwise suppose to be a file
								foreach (string key in keys) {
								
									SortedList<string,List<string>> element=listTags[key];
									string name_key=dirent.d_name;
									if (!element.ContainsKey(name_key)) {
										element[name_key]=new List<string>();
									}
								
									element[name_key].Add(name);
								}
							}
						}
//						System.Console.Out.WriteLine("\t"+buf.st_mode.ToString());
					}
				}
				Syscall.closedir(dirHandle);
			}
			return true;
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
			string key="";
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
							List<string> listFiles=new List<string>();
							foreach (string fullPathName in listTags[tag][keyContents]) {
								if (contents[keyContents].Contains(fullPathName)) {
									listFiles.Add(fullPathName);
								}
							}
							contentsPurged.Add(keyContents,listFiles);
							
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

				foreach (string listTagsKey in listTags.Keys) {
					
					// Check in this tags is already in the tag list
					if (!tagsList.Contains(listTagsKey)) {
						
						// Check if this tag is already specify in the contents (file list)
						// We need modify this behaviour later
						if (!contents.ContainsKey(listTagsKey)) {
							
							// Check if this tag have some files with actual path
							bool checkDir=false;
							foreach (string filename in contents.Keys) {
								if (listTags[listTagsKey].ContainsKey(filename)) {
									foreach (string realFileName in listTags[listTagsKey][filename]) {
										if (contents[filename].Contains(realFileName)) {
											checkDir=true;
											break;
										}
									}
								}
							}

							if (checkDir) {
								contents.Add(listTagsKey,null);
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
	
/*		public Errno OnReadDirectory (string directory, OpenedPathInfo info,
		                                          out IEnumerable<DirectoryEntry> names)
		{
			names=null;
			
			// Check for root directory
			if (directory.Equals("/")) {
				
				// Read all directory tag
				IEnumerator<string> en=listTags.Keys.GetEnumerator();
				names=ListNames(en);

			} else {
				// Expand path as list of tags
				string[] tags=directory.Substring(1).Split('/');
				SortedList<string,List<string>> contents=null;
				SortedList<string,List<string>> contentsPurged=null;
				IEnumerator<string> en;
				List<string> tagsList=new List<string>();
				string key="";
				foreach (string tag in tags) {
					
					// Check if is the first look
					if (contents==null) {
						contents=new SortedList<string,List<string>>();
						en=listTags[tag].Keys.GetEnumerator();
						while (en.MoveNext()) {
							contents.Add(en.Current,listTags[tag][en.Current]);
						}
					} else {
						en=contents.Keys.GetEnumerator();
						contentsPurged=new SortedList<string,List<string>>();
						while (en.MoveNext()) {
							key=en.Current;
							if (listTags[tag].ContainsKey(key)) {
								contentsPurged.Add(key,contents[key]);
							}
						}
						
						contents=contentsPurged;

						if (contents.Count<1) {
							break;
						}
					}
					tagsList.Add(tag);
				}

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
*/
		public Errno OnGetPathStatus (string path, out Stat stbuf)
		{
//			System.Console.Out.Write("DEBUG: OnGetPathStatus for "+path+" UID="+Mono.Unix.Native.Syscall.getuid()+" GID="+Mono.Unix.Native.Syscall.getgid());			

			stbuf = new Stat ();

			stbuf.st_uid = Mono.Unix.Native.Syscall.getuid();
			stbuf.st_gid = Mono.Unix.Native.Syscall.getgid();

			Mono.Unix.Native.Timeval timeval;
			Mono.Unix.Native.Syscall.gettimeofday(out timeval);
			stbuf.st_mtime = timeval.tv_sec;
			
			if (path == "/") {
//				System.Console.Out.WriteLine("path=/");			
				stbuf.st_mode  = NativeConvert.FromUnixPermissionString ("dr-xr-xr-x");
				stbuf.st_nlink = 1;
				return 0;
			}
			if (path.IndexOf('/',1)<=0) {
//				System.Console.Out.WriteLine("path="+path);			
				if (!this.listTags.ContainsKey(path.Substring(1))) {
					return Errno.ENOENT;
				}

//				System.Console.Out.WriteLine("\t okPath");			
				stbuf.st_mode  = NativeConvert.FromUnixPermissionString ("dr-xr-xr-x");
					stbuf.st_nlink = 1;
				return 0;
			} else {
//				System.Console.Out.WriteLine("other path="+path);
				
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
				yield return new DirectoryEntry (directories.Current);
			}
		}

		public Errno OnReadSymbolicLink (string link, out string target) {
//			System.Console.Out.WriteLine("DEBUG: OnReadSymbolicLynk for path="+link);
			
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
		
		
		public static void Main(string[] args) {

			DirAsTag dirAsTag=new DirAsTag("/home/torello/mono_project",args);
			dirAsTag.dumpStructure();
			
		}
	}
}
