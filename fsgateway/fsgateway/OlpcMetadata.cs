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
