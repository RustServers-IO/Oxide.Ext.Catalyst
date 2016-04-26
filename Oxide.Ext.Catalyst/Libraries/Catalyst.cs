// COPYRIGHT 2016 RUSTSERVERS.IO
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;

using Newtonsoft.Json.Linq;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Plugins;

using UnityEngine;

namespace Oxide.Ext.Catalyst.Libraries
{
	public class Catalyst : Library
	{
		internal enum CommitType 
		{
			Write,
			Delete,
			Require,
			Remove
        }

        internal enum StatusMessage {
            SourceMismatch,
            VersionMismatch,
            NoUpgradePath
        }

		internal class CommitAction
		{
			public CommitType type;
			public string path;
			public string src;
			public string name;
			public string version;

			public CommitAction(CommitType type, string name, string path, string version = "*", string src = "") 
			{
				this.name = name;
				this.type = type;
				this.path = path;
				this.version = version;
				this.src = src;
			}

			public override string ToString() 
			{
				return name + " " + version;
			}
		}

		DependencyValidator _Validator;
		List<CommitAction> commitActions = new List<CommitAction> ();
		List<string> commitErrors = new List<string>();
		private bool isValidCommit = false;
		public bool IsCommitting = false;
		private Dictionary<string, JObject> pluginCache = new Dictionary<string, JObject>();

		private Core.Libraries.WebRequests webrequest = Interface.Oxide.GetLibrary<Core.Libraries.WebRequests>();
		private Core.Libraries.Plugins plugins = Interface.Oxide.GetLibrary<Core.Libraries.Plugins>();

		public override bool IsGlobal => false;

		private readonly string _ConfigDirectory;
		private readonly string _DataDirectory;
		private readonly string _PluginDirectory;
		private readonly DataFileSystem _DataFileSystem;

		public CatalystSettings Settings;

		private WebClient _WebClient = new WebClient();
		CatalystExtension Extension;

		private string[] exts = 
		{
			"cs",
			"py",
			"lua",
			"js",
			"coffee"
		};

		public bool HasErrors 
		{
			get 
			{
				return this.commitErrors.Count() > 0;
			}
		}

		public Catalyst(CatalystExtension catalystExtension)
		{
			Extension = catalystExtension;
			_DataFileSystem = Interface.Oxide.DataFileSystem;
			_ConfigDirectory = Interface.Oxide.ConfigDirectory;
			_DataDirectory = Interface.Oxide.DataDirectory;
			_PluginDirectory = Interface.Oxide.PluginDirectory;
			_Validator = new DependencyValidator(this);
		}

		internal void Initialize ()
		{
			CheckConfig ();
		}

		public void CheckConfig ()
		{
			string path = Path.Combine (_ConfigDirectory, "Catalyst");
			if (_DataFileSystem.ExistsDatafile (path)) 
			{
				Settings = _DataFileSystem.ReadObject<CatalystSettings> (path);
				if (Settings.Version != Extension.Version.ToString ()) 
				{
					UpgradeConfig();
					Settings.Version = Extension.Version.ToString();
					SaveConfig();
				}
			} 
			else 
			{
				Interface.Oxide.LogInfo("[Catalyst] Creating Default Configuration");
				Settings = new CatalystSettings();
				Settings.Debug = false;
				Settings.SourceList = new List<string>() 
				{
					"http://rustservers.io"
				};
				Settings.Require = new Dictionary<string, string>();
				Settings.RequireDev = new Dictionary<string, string>();
				Settings.Version = Extension.Version.ToString();

				SaveConfig ();
			}
		}

		private void UpgradeConfig() 
		{

		}

		private void SaveConfig() 
		{
			_DataFileSystem.WriteObject<CatalystSettings>(Path.Combine(_ConfigDirectory, "Catalyst"), Settings);
		}

        internal string SHA(string source)
        {
            var sha1 = System.Security.Cryptography.SHA1.Create();
            byte[] buf = System.Text.Encoding.UTF8.GetBytes(source);
            byte[] hash= sha1.ComputeHash(buf, 0, buf.Length);
            return System.BitConverter.ToString(hash).Replace("-", "");
        }

        internal Dictionary<StatusMessage, string> GetStatus()
        {
            Dictionary<StatusMessage, string> statusChanges = new Dictionary<StatusMessage, string>();
            foreach (KeyValuePair<string, string> kvp in Settings.Require)
            {
                Plugin loaded = plugins.Find(kvp.Key);
                JObject pluginInfo = GetPluginInfo(kvp.Key, kvp.Value);
                if (pluginInfo != null)
                {
                    string name = pluginInfo["name"].ToString();
                    string ext = pluginInfo["ext"].ToString();
                    string version = pluginInfo["version"].ToString();
                    string filename = name + "." + ext;
                    string path = Path.Combine(_PluginDirectory, filename);

                    string old_contents = System.IO.File.ReadAllText(path);
                    string new_contents = GetPluginSource(pluginInfo["src"].ToString());

                    string sha_1 = SHA(old_contents);
                    string sha_2 = SHA(new_contents);

                    if (sha_1 != sha_2)
                    {
                        if (loaded != null && loaded.Version.ToString() != version)
                        {
                            statusChanges.Add(StatusMessage.VersionMismatch, filename + ": local version (" + loaded.Version.ToString() + ") different than remote version (" + version + ")");
                        }
                        else
                        {
                            statusChanges.Add(StatusMessage.SourceMismatch, filename + ": " + loaded.Version.ToString() + " local sources are different");
                        }
                    }
                }
                else if(loaded != null)
                {
                    statusChanges.Add(StatusMessage.NoUpgradePath, loaded.Filename + ": No upgrade path, plugin is not found on sources");
                }
            }

            return statusChanges;
        }

		internal bool IsPluginInstalled (string plugin)
		{
			if (plugins.Exists (plugin)) 
			{
				return true;
			}

			foreach(string ext in exts) 
			{
				string path = Path.Combine (_PluginDirectory, plugin + "." + ext);
				if(System.IO.File.Exists(path)) 
				{
					return true;
				}
			}

			return false;
		}

		internal string[] FindPlugin (string name, string version = "")
		{
			List<string> result = new List<string> ();
			JObject results = null;
			foreach (string source in Settings.SourceList) 
			{
				string url = "";
				if (string.IsNullOrEmpty (version) || version == "*") 
				{
					url = source + "/s/" + name + ".json";
				} 
				else 
				{
					url = source + "/s/" + name + " " + version + ".json";
				}

				if (Settings.Debug) 
				{
					Interface.Oxide.LogInfo("Reading " + url);
				}

				try 
				{
					results = JObject.Parse (_WebClient.DownloadString (url));

					if (results ["data"] == null) 
					{
						continue;
					} 
					else 
					{
						foreach (string r in results["data"]) 
						{
							result.Add (r);
						}

						return result.ToArray();
					}
				} 
				catch (WebException ex) 
				{
					if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null) 
					{
						var resp = (HttpWebResponse)ex.Response;
						if (resp.StatusCode == HttpStatusCode.NotFound)  // HTTP 404
						{
							continue;
						}
					}
					throw;
				}
			}

			return null;
		}

		internal object PluginExists(string name, string version = "")
        {
            var pluginInfo = GetPluginInfo(name, version);
            if (pluginInfo is JObject)
            {
                return pluginInfo;
            }
			return false;
		}

		internal JObject GetPluginInfo (string name, string version = "")
		{
			if (pluginCache.ContainsKey (name + "-" + version)) 
			{
				return pluginCache [name + "-" + version];
			}

			JObject plugin = null;
			foreach (string source in Settings.SourceList) 
			{
				string url = "";
				if (string.IsNullOrEmpty (version) || version == "*") 
				{
					url = source + "/p/" + name + ".json";
				} 
				else 
				{
					url = source + "/p/" + name + "/" + version + ".json";
				}

				if (Settings.Debug) 
				{
					Interface.Oxide.LogInfo("Reading " + url);
				}

				try 
				{
					plugin = JObject.Parse (_WebClient.DownloadString (url));

					if (plugin["404"] != null) 
					{
						continue;
					} 
					else if (IsPluginValid(plugin)) 
					{
						pluginCache.Add(name + "-" + version, plugin);
						return plugin;
					}
				} 
				catch(WebException ex) 
				{
					if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
					{
						var resp = (HttpWebResponse)ex.Response;
						if (resp.StatusCode == HttpStatusCode.NotFound) // HTTP 404
						{
							continue;
						}
					}
					throw;
				}
			}

			return null;
		}

		internal string GetPluginSource(string src) 
		{
            if (Settings.Debug) 
            {
                Interface.Oxide.LogInfo ("Downloading: " + src);
            }

            return _WebClient.DownloadString (src.Replace (@"\", ""));
		}

		private void CommitWrite(string name, string path, string version, string src) 
		{
			commitActions.Add (new CommitAction (CommitType.Write, name, path, version, src));
		}

		private void CommitRequire(string name, string path, string version) 
		{
			commitActions.Add (new CommitAction (CommitType.Require, name, path, version));
		}

		private void CommitDelete(string name, string path) 
		{
			commitActions.Add (new CommitAction (CommitType.Delete, name, path));
		}

		private void CommitRemove(string name, string path) 
		{
			commitActions.Add (new CommitAction (CommitType.Remove, name, path));
		}

		internal void BeginCommit ()
		{
			if (Settings.Debug) 
			{
				Interface.Oxide.LogInfo ("[Catalyst] Begin Commit");
			}
			IsCommitting = true;
			isValidCommit = true;
			commitErrors = new List<string>();
			pluginCache = new Dictionary<string, JObject>();
		}

		internal string Error (string msg)
		{
			if (Settings.Debug) 
			{
				Interface.Oxide.LogInfo ("[Catalyst] Commit Error: " + msg);
			}
			isValidCommit = false;
			commitErrors.Add(msg);
			return msg;
		}

		internal void EndCommit()
        {
            if (Settings.Debug)
            {
                Interface.Oxide.LogInfo("[Catalyst] Ending Commit");
            }

            IsCommitting = false;

            if (!isValidCommit)
            {
                if (commitErrors.Count() > 0)
                {
                    foreach (string error in commitErrors)
                    {
                        Interface.Oxide.LogError(error);
                    }
                }
                return;
            }

            bool saveConfig = false;

            if (commitActions.Count > 0)
            {
                foreach (CommitAction commit in commitActions)
                {
                    if (Settings.Debug)
                    {
                        Interface.Oxide.LogInfo("[Catalyst] " + commit.type.ToString() + ": " + commit.name);
                    }
                    switch (commit.type)
                    {
                        case CommitType.Write:
                            System.IO.File.WriteAllText(commit.path, commit.src);
                            break;
                        case CommitType.Delete:
                            System.IO.File.Delete(commit.path);
                            break;
                        case CommitType.Require:
                            if (!Settings.Require.ContainsKey(commit.name))
                            {
                                Settings.Require.Add(commit.name, commit.version);
                                saveConfig = true;
                            }
                            break;
                        case CommitType.Remove:
                            if (Settings.Require.ContainsKey(commit.name))
                            {
                                Settings.Require.Remove(commit.name);
                                saveConfig = true;
                            }
                            break;
                    }
                }
            }

            if (saveConfig)
            {
                SaveConfig ();
            }

			commitActions.Clear();
		}

		public object InstallPlugin(string plugin, string version = "*") 
		{
			JObject pluginInfo = GetPluginInfo (plugin, version);
			if (pluginInfo != null) 
			{
				try 
				{
					return InstallPlugin (pluginInfo, version);
				} 
				catch (Exception ex) 
				{
					return Error(ex.Message);
				}
			}

			return Error("No plugin found");
		}

		internal object InstallPlugin (JObject pluginInfo, string version = "*")
		{
			string name = pluginInfo ["name"].ToString ();
            string ext = pluginInfo ["ext"].ToString ();
            string path = Path.Combine (_PluginDirectory, name + "." + ext);

			string v = pluginInfo ["version"].ToString ();
			bool matchingVersion = false;
			if (IsPluginInstalled (name)) 
			{
				Plugin p = plugins.Find (name);
				if (p.Version.ToString () == v) 
				{
					matchingVersion = true;
				}
			}

			var requires = pluginInfo ["plugin"] ["require"];
			if (requires != null) 
			{
				Dictionary<string, string> dependencies = requires.ToObject<Dictionary<string, string>>();
				foreach (KeyValuePair<string, string> kvp in dependencies) {
					if (!isValidCommit)
						break;
					InstallPlugin (kvp.Key, kvp.Value);
				}
			}

			if (!Validate ()) 
			{
				return false;
			}

			if (isValidCommit) 
			{
				// INSTALL PLUGIN
				if (!Settings.Require.ContainsKey (name)) 
				{
					CommitRequire (name, path, version);
				}

				if (!matchingVersion || !IsPluginInstalled (name)) 
				{
					string src = GetPluginSource (pluginInfo ["src"].ToString ());
					CommitWrite (name, path, version, src);
				} 

				return true;
			}

			return false;
		}

		public object UpdatePlugin (string plugin, string version = "*")
		{
			JObject pluginInfo = GetPluginInfo (plugin, version);
			if (pluginInfo != null) 
			{
				try 
				{
					return UpdatePlugin (pluginInfo, version);
				} 
				catch (Exception ex) 
				{
					return Error(ex.Message);
				}
			}

			return Error("No plugin found");
		}

        internal List<JObject> GetPluginChildren(JObject pluginInfo)
        {
            List<JObject> objs = new List<JObject>();
            objs.Add(pluginInfo);

            var requires = pluginInfo["plugin"]["require"];
            if (requires != null)
            {
                Dictionary<string, string> dependencies = requires.ToObject<Dictionary<string, string>>();
                foreach (KeyValuePair<string, string> kvp in dependencies)
                {
                    JObject childPlugin = GetPluginInfo(kvp.Key, kvp.Value);
                    if (childPlugin != null)
                    {
                        objs.Add(childPlugin);
                    }
                }
            }

            return objs;
        }

		internal object UpdatePlugin (JObject pluginInfo, string version = "*")
		{
			string name = pluginInfo ["name"].ToString ();
			if (!IsPluginInstalled (name)) 
			{
				return InstallPlugin (name, version);
			}

			string v = pluginInfo ["version"].ToString ();
			bool matchingVersion = false;
			Plugin p = plugins.Find (name);
			if (p.Version.ToString () == v) 
			{
				matchingVersion = true;
			}

			string ext = pluginInfo ["ext"].ToString ();
			string path = Path.Combine (_PluginDirectory, name + "." + ext);

			var requires = pluginInfo ["plugin"] ["require"];
			if (requires != null) 
			{
				Dictionary<string, string> dependencies = requires.ToObject<Dictionary<string, string>>();
				foreach (KeyValuePair<string, string> kvp in dependencies) 
				{
					if (!isValidCommit)
						break;
					UpdatePlugin (kvp.Key, kvp.Value);
				}
			}

			if (!Validate ()) 
			{
				return false;
			}

			if (isValidCommit) 
			{
				// UPDATE PLUGIN
				if (!matchingVersion) {
					if (System.IO.File.Exists (path)) 
					{
						CommitDelete (name, path);
					}
					string src = GetPluginSource (pluginInfo ["src"].ToString ());
					CommitWrite (name, path, version, src);
				}

				return true;
			}

			return false;
		}

		public object RemovePlugin (string plugin)
		{
			JObject pluginInfo = GetPluginInfo (plugin);
			if (pluginInfo != null) 
			{
				try 
				{
					return RemovePlugin (pluginInfo);
				} 
				catch (Exception ex) 
				{
					return Error (ex.Message);
				}
			}

			return Error("No plugin found");
		}

		internal object RemovePlugin (JObject pluginInfo)
		{
			//TODO: CHECK IF ANY PLUGINS DEPEND ON THIS ONE AND REMOVE THEM TOO
			string name = pluginInfo ["name"].ToString ();

			string ext = pluginInfo ["ext"].ToString ();
			string path = Path.Combine (_PluginDirectory, name + "." + ext);

			if (System.IO.File.Exists (path)) 
			{
				// REMOVE PLUGIN
				CommitRemove (name, path);
				if (IsPluginInstalled (name) && !HasErrors) 
				{
					CommitDelete (name, path);
				}
			} 
			else 
			{
				return Error("File does not exist");
			}

			return true;
		}

		internal bool Validate ()
		{
			return _Validator.Passes();
		}

		internal bool IsPluginValid(JObject pluginInfo) 
		{
			if(pluginInfo ["name"] == null) return false;
			if(pluginInfo ["version"] == null) return false;
			if(pluginInfo ["ext"] == null) return false;
			if(pluginInfo ["src"] == null) return false;
			if(pluginInfo ["plugin"] == null) return false;

			return true;
		}

		internal void Shutdown()
		{
		}
	}
}
