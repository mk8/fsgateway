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
using System.Collections;
using System.Collections.Generic;
using Mono.Fuse;
using System.Xml;
using System.Xml.XPath;

namespace FsGateway
{
	public class FsXml: IFsGateway
	{
		XmlDocument document=null;
		XmlNamespaceManager nsm;
		string defaultPrefix="";
		string fileXml=null;
		long timeval;
		uint uid,gid;

		public FsXml() 
		{
			document=null;
		}
		
		public FsXml(string fileXml, string[] args) 
		{
			analyzeXmlFile(fileXml);
		}
		
		public void Dispose() {
		}
		
		public bool Connect() {
			return false;
		}
		
		public bool Connect(string parameter) {

			analyzeXmlFile(parameter);
			return true;
		}

		private void analyzeXmlFile(string file) {

			fileXml=file;
			
			document=new XmlDocument();
			document.Load(file);
			
			Stat stat;
			Mono.Unix.Native.Syscall.stat(file,out stat);
			gid=stat.st_gid;
			uid=stat.st_uid;
			
			timeval=stat.st_mtime;			

			// Namespaces analyze
			nsm=new XmlNamespaceManager(document.NameTable);
			SortedDictionary<string,string> namespaces=new SortedDictionary<string, string>();
			string defaultNameSpace;
			defaultNameSpace=analyzeNamespaces(document.FirstChild, out namespaces);
			
			// Check for default namespace
			if (defaultNameSpace!=null && defaultNameSpace.Length>0) {
				
				int i=0;
				defaultPrefix="def";
				while (namespaces.ContainsKey(defaultPrefix)) {
					++i;
					defaultPrefix="def"+i.ToString();					
				};
				namespaces.Add(defaultPrefix,defaultNameSpace);
			}

			if (namespaces.Keys!=null) {
				IEnumerator<string> itKey=namespaces.Keys.GetEnumerator();
				while (itKey.MoveNext()) {
					nsm.AddNamespace(itKey.Current,namespaces[itKey.Current]);
				}
			}
		}
		
		public string connectionParameter {
			get {
				return fileXml;
			}
		}
		
		public void Unconnect() {
			fileXml=null;
			document=null;
		}
		
		public string Usage {
			get {
				return "You need to specify the XML file.";
			}
		}
		
		public string storageType {
			get {
				return "xmlfs";
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

			string data;
			Byte[] buffer=null;
			System.Text.UTF8Encoding encoder=new System.Text.UTF8Encoding();

			if (isXmlnsDeclaration(file)) {

				buffer = encoder.GetBytes(nsm.LookupNamespace(getXmlnsDeclaration(file)));
			} else {

				XmlNode	node=document.SelectSingleNode(file,nsm);
				if (node==null) {
					bytesWritten=0;
					return Errno.ENODATA;
				}

				data=node.Value;
				buffer=encoder.GetBytes(data);
			}

			buffer.CopyTo(buf,offset);
			bytesWritten=System.Math.Min(buf.Length,buffer.Length-(int)offset);

			return 0;
		}		

		private IEnumerable<DirectoryEntry> ListNames (IEnumerator<string> directories)
		{
			while (directories.MoveNext()) {
				yield return new DirectoryEntry (directories.Current);
			}
		}

		public Errno OnReadDirectory (string directory, OpenedPathInfo info,
		                                          out IEnumerable<DirectoryEntry> names)
		{
			XmlNode node=document.SelectSingleNode(directory,nsm);

			List<string> listPath=analyzeNode(node);
			IEnumerator<string> en=listPath.GetEnumerator();
			names=ListNames(en);

			return 0;
		}

		private bool isXmlnsDeclaration (string path) {
			// Check for specific attrib
			char[] pattern = { '/' };
			string[] pathSplitted = path.Split(pattern);
			
			// Check if the path is an attribute
			string lastTag=pathSplitted[pathSplitted.Length-1];
			if (lastTag.StartsWith("@xmlns")) {
				if (lastTag.Length==6)
					return true;
				if (lastTag.Substring(6,1).Equals(":"))
					return true;
			}
			
			return false;
		}

		private string getXmlnsDeclaration (string path) {
			// Check for specific attrib
			char[] pattern = { '/' };
			string[] pathSplitted = path.Split(pattern);
			
			// Check if the path is an attribute
			string lastTag=pathSplitted[pathSplitted.Length-1];
			if (lastTag.StartsWith("@xmlns")) {
				if (lastTag.Length==6)
					return this.defaultPrefix;
				if (lastTag.Substring(6,1).Equals(":"))
					return lastTag.Substring(7);
			}
			
			return null;
		}
		

		public Errno OnGetPathStatus (string path, out Stat stbuf)
		{
			stbuf = new Stat ();
			stbuf.st_uid = uid;
			stbuf.st_gid = gid;

			stbuf.st_mtime = timeval;

			if (isXmlnsDeclaration(path)) {
				stbuf.st_mode = NativeConvert.FromUnixPermissionString ("-r--r--r--");
				System.Text.UTF8Encoding encoder=new System.Text.UTF8Encoding();
				stbuf.st_size=encoder.GetByteCount(nsm.LookupNamespace(getXmlnsDeclaration(path)));
			} else {
				
				XmlNode node=null;

				node=document.SelectSingleNode(path,nsm);

				if (node==null) {
					return Errno.ENOENT;
				}
	
				if (node.NodeType==System.Xml.XmlNodeType.Element || node.NodeType==System.Xml.XmlNodeType.Document) {
					stbuf.st_mode  = NativeConvert.FromUnixPermissionString ("dr-xr-xr-x");
					stbuf.st_nlink = 1;
				} else {
					stbuf.st_mode = NativeConvert.FromUnixPermissionString ("-r--r--r--");
	
					System.Text.UTF8Encoding encoder=new System.Text.UTF8Encoding();
					stbuf.st_size=encoder.GetByteCount(node.Value);
				}
			}
			return 0;
		}

		List<string> analyzeNode(XmlNode reader) {
			List<string> listPath=new List<string>();

			if (reader==null) return listPath;

			XmlNode child=reader.FirstChild;

			if (reader.NodeType==XmlNodeType.Element || reader.NodeType==XmlNodeType.Document) {
				if (reader.Attributes !=null) {
					XmlAttributeCollection attributes=reader.Attributes;
					for (int j=0; j<attributes.Count; j++) {
						addPathToList(listPath,attributes.Item(j));
					}
				}

				if (child!=null) {
			
					do {
						addPathToList(listPath,child);
					}
					while ((child=child.NextSibling)!=null);
				}
			}
			
			return listPath;
		}

		private void addPathToList(List<string> list, XmlNode node) {
			string name="";
			if (node.NodeType==XmlNodeType.Attribute) {
				name = "@"+node.Name;
			} else {
			
				if (defaultPrefix!=null && defaultPrefix.Length>0 && (node.Prefix==null || node.Prefix.Length<1)) {
					name += defaultPrefix+":";
				}
				
				switch (node.NodeType) {
				case XmlNodeType.Text:
					name = "text()";
					break;
				case XmlNodeType.XmlDeclaration:
					name=null;
					break;
				default:
					name += node.Name;
					break;
				}
			}
			if (name != null) {
				addPathToList(list,name);
			}
		}

		private void addPathToList(List<string> list, string path) {
			
			// Check if the string is present in the list
			if (list.Contains(path)) {
				
				// There is the path string in the list so remove it and add two specify the index of the path
				list.Remove(path);
				list.Add(path+"[1]");
				list.Add(path+"[2]");
			} else if (list.Contains(path+"[1]")) {
				
				// The path is present in several copy so I need to find the first index empty
				// This implementation is not goot 'cause have a o(n2) complexity
				int i=2;
				for (; list.Contains(path+"["+i+"]"); ++i);
				path = path+"["+i+"]";
				list.Add(path);
				
			} else {
				
				// The name is not present
				list.Add(path);
			}
		}

		static string analyzeNamespaces(XmlNode reader,out SortedDictionary<string,string> ns) {

			char[] delimiters = { ':' };
			string defaultNameSpace=null;
			
			ns=new SortedDictionary<string,string>();
			if (reader==null) return defaultNameSpace;

			do {
				if (reader.Attributes!=null) {
					IEnumerator en=reader.Attributes.GetEnumerator();
					while (en.MoveNext()) {
						XmlNode node=(XmlNode)en.Current;
						string[] name=node.Name.Split(delimiters);
					
						if (name[0].Equals("xmlns")) {
							// Check for default namespace
							if (name.Length<2) {
								defaultNameSpace=node.Value;
							} else {
								ns.Add(name[1],node.Value);
							}
						}
					}
				}

				SortedDictionary<string,string> nns;
				string defaultNS;
				defaultNS=analyzeNamespaces(reader.FirstChild,out nns);
				
				// Check for default namespace
				if (defaultNameSpace==null || defaultNameSpace.Length<1) {
					defaultNameSpace=defaultNS;
				}
				
				// Merge the two sorted dictionary
				IEnumerator<string> enk=nns.Keys.GetEnumerator();
				while (enk.MoveNext()) {
					if (!ns.ContainsKey(enk.Current)) {
						ns.Add(enk.Current,nns[enk.Current]);
					} else {
						if (!nns[enk.Current].Equals(ns[enk.Current])) {
							Console.WriteLine("XML that use the same prefix "+enk.Current+" for different namespaces "+nns[enk.Current]+", "+ns[enk.Current]+".");
						}
					}
				}
			} while ((reader=reader.NextSibling)!=null);

			return defaultNameSpace;
		}

		public Errno OnReadSymbolicLink (string link, out string target) {
			target=link;
			return Errno.ENOENT;
		}
	}
}
