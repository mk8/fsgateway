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
using System.Collections.Generic;

namespace FsGateway
{

	public class OlpcMetadata
	{
		private string _title;
		private string _title_set_by_user;
		private string _activity;
		private List<string> _tags=new List<string>();
		
		public OlpcMetadata(string path)
		{
			System.IO.StreamReader sr=null;
			char[] tagSeparator = { ' ', ',', ':', ';', '\t'}; 
			
			
			try  {
				sr = System.IO.File.OpenText(path+"/metadata/title");
				_title = sr.ReadLine();
				sr.Close();
			} catch {};

			try  {
				sr = System.IO.File.OpenText(path+"/metadata/title_set_by_user");
				_title_set_by_user = sr.ReadLine();
				sr.Close();
			} catch {};

			try  {
				sr = System.IO.File.OpenText(path+"/metadata/activity");
				_activity = sr.ReadLine();
				sr.Close();
			} catch {};
			
			try  {
				sr = System.IO.File.OpenText(path+"/metadata/tags");
				String tag;
				while ((tag = sr.ReadLine()) != null) {
						
					string[] splitted=tag.Split(tagSeparator);
					foreach (string splittedTag in splitted) {
						if (splittedTag.Trim().Length>0) {
							tags.Add(splittedTag.Trim());
						}
					}
				}
				sr.Close();
			} catch {};

		}
		
		public string title {
			get {
				return _title;
			}
		}
		
		public string titleSetByUser {
			get {
				return _title_set_by_user;
			}
		}
		
		public string activity {
			get {
				return _activity;
			}
		}

		public List<string> tags {
			get {
				return _tags;
			}
		}
		
		public string ToString() {
			return title + (activity != null && activity.Length > 0 ? " ("+activity+")" : "");
		}
	}
}
