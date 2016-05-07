// COPYRIGHT 2016 RUSTSERVERS.IO
using ConVar;
using Network;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
using UnityEngine;


namespace Oxide.Ext.Catalyst.Plugins
{
	public abstract class CatalystPlugin : CSPlugin
	{
		private CatalystExtension Extension;
		private Oxide.Ext.Catalyst.Libraries.Catalyst library;
        private Core.Libraries.Plugins plugins;

		public CatalystPlugin(CatalystExtension extension)
	    {
			this.Name = "Catalyst";
			this.Title = "Catalyst";
            this.Author = "RustServers.IO";
			this.Version = extension.Version;
			this.HasConfig = true;
	    	Extension = extension;
	    }

		protected abstract void AddConsoleCommand(string command, string methodName);

		[HookMethod("Init")]
	    private void Init ()
		{
            plugins = Interface.Oxide.GetLibrary<Core.Libraries.Plugins>();
			library = Interface.Oxide.GetLibrary<Oxide.Ext.Catalyst.Libraries.Catalyst> ();
			if (library == null) 
			{
				LogError("[Catalyst] Library not found");
				return;
			}

			AddConsoleCommand ("catalyst.update", "ccUpdate");
			AddConsoleCommand ("catalyst.require", "ccRequire");
			AddConsoleCommand ("catalyst.remove", "ccRemove");
			AddConsoleCommand ("catalyst.validate", "ccValidate");
			AddConsoleCommand ("catalyst.source", "ccSource");
			AddConsoleCommand ("catalyst.config", "ccConfig");
			AddConsoleCommand ("catalyst.search", "ccSearch");
			AddConsoleCommand ("catalyst.info", "ccInfo");
			AddConsoleCommand ("catalyst.debug", "ccDebug");
            AddConsoleCommand ("catalyst.status", "ccStatus");
            AddConsoleCommand ("catalyst.sync", "ccSync");
            AddConsoleCommand ("catalyst.version", "ccVersion");
		}

        protected abstract void ccUpdate(ConsoleSystem.Arg arg);
        protected abstract void ccRequire(ConsoleSystem.Arg arg);
        protected abstract void ccRemove(ConsoleSystem.Arg arg);
        protected abstract void ccValidate(ConsoleSystem.Arg arg);
        protected abstract void ccSource(ConsoleSystem.Arg arg);
        protected abstract void ccConfig(ConsoleSystem.Arg arg);
        protected abstract void ccSearch(ConsoleSystem.Arg arg);
        protected abstract void ccInfo(ConsoleSystem.Arg arg);
        protected abstract void ccDebug(ConsoleSystem.Arg arg);
        protected abstract void ccStatus(ConsoleSystem.Arg arg);
        protected abstract void ccSync(ConsoleSystem.Arg arg);
        protected abstract void ccVersion(ConsoleSystem.Arg arg);

        protected void Version()
        {
            Log("Version:" + Extension.Version.ToString());
        }

        protected void Status()
        {
            library.BeginCommit();
            Dictionary<Libraries.Catalyst.StatusMessage, string> statusChanges = library.GetStatus();
            library.EndCommit();

            if (statusChanges.Count == 0)
            {
                Log("No changes to plugins.  Everything up to date and matching public sources");
            }
            else
            {
                foreach (KeyValuePair<Libraries.Catalyst.StatusMessage, string> kvp in statusChanges)
                {
                    Log(kvp.Value);
                }
            }
        }

		protected void Debug (string debugArg = null)
		{
			bool debug;
			if (string.IsNullOrEmpty (debugArg)) {
				debug = !library.Settings.Debug;
			}

			if (!bool.TryParse (debugArg, out debug)) {
				LogError("Debug mode must be true/false");
				return;
			}

			library.Settings.Debug = debug;

			if(library.Settings.Debug) {
				Log("[Catalyst] Debug mode: enabled");
			} else {
				Log("[Catalyst] Debug mode: disabled");
			}
		}

		protected void Configure (string pluginName, string key, object value = null)
		{
			Plugin plugin = plugins.Find (pluginName);
			if (plugin == null) {
				Log ("No plugin found");
				return;
			}

			string[] parts = key.Split (new char[] { '.' });

			if (value == null) 
			{
				object val = plugin.Config.Get (parts);
				if (val != null) 
				{
					Log (key + " : " + val.ToString ());
				} 
				else 
				{
					Log ("No setting found");
				}
			} 
			else 
			{
				bool bl = false;
				if (bool.TryParse (value.ToString (), out bl)) 
				{
					value= bl;
				}
				plugin.Config.Set (parts, value);
				plugin.Config.Save ();
			}
		}

		protected void Source (string name = null)
		{
			if (name != null) 
			{
				if (!library.Settings.SourceList.Contains (name)) 
				{
					library.Settings.SourceList.Add (name);
					Log ("[Catalyst] Source added!");
				} 
				else 
				{
					library.Settings.SourceList.Remove (name);
					Log ("[Catalyst] Source removed!");
				}
			} 
			else 
			{
				StringBuilder sb = new StringBuilder ();
				sb.AppendLine ("Sources:");
				foreach (string source in library.Settings.SourceList) 
				{
					sb.AppendLine (source);
				}

				Log (sb.ToString ());
			}
		}

		protected void Search (string terms)
		{
			string[] plugins = library.FindPlugin (terms);

			if (plugins.Length > 0) 
			{
				StringBuilder sb = new StringBuilder();
				int i = 1;
				sb.AppendLine ("Found (" + plugins.Length + ")");
				foreach (string plugin in plugins) 
				{
					sb.AppendLine (i + ". " + plugin);
					i++;
				}

				Log(sb.ToString());
			} 
			else 
			{
				Log("No Plugin Found!");
			}
		}

		protected void Update (params string[] names)
		{
			if (names.Length == 0) 
			{
				library.BeginCommit ();

				foreach (KeyValuePair<string, string> kvp in library.Settings.Require) 
				{
                    var result = library.PluginExists (kvp.Key);
					if (result is JObject) 
					{
                        HandleResult (library.UpdatePlugin ((JObject)result), "Updating " + kvp.Key);
					}
				}
				library.EndCommit();
			} 
			else 
			{
				library.BeginCommit ();
				foreach (string name in names) 
				{
					if (library.Settings.Debug) 
					{
						Log ("[Catalyst] Updating " + name);
					}

                    var result = library.PluginExists (name);
					if (!(result is JObject) && names.Length == 1) 
					{
						Search(name);
						return;
					}
                    HandleResult (library.UpdatePlugin ((JObject)result), "Updating " + name);
				}

				library.EndCommit ();
			}
		}

        protected void Sync()
        {
            library.BeginCommit();

            List<Plugin> plugins = Manager.GetPlugins().ToList();

            int i = 0;
            foreach (Plugin plugin in plugins)
            {
                if (!plugin.IsCorePlugin)
                {
                    var result = library.PluginExists(plugin.Name);

                    if (result is JObject)
                    {
                        if (!library.Settings.Require.ContainsKey(plugin.Name))
                        {
                            HandleResult(library.RequirePlugin((JObject)result), "Requiring " + plugin.Name);
                            i++;
                        }
                    }
                    else
                    {
                        Log("Plugin Not Found. " + plugin.Name);
                    }
                }
            }

            Log("Added ("+i+") plugins to requires");

            library.EndCommit();
        }

		protected void Require (string name, string version)
		{
			library.BeginCommit ();

			if (library.Settings.Debug) 
			{
				Log ("[Catalyst] Requiring " + name);
			}

            var result = library.PluginExists (name, version);

			if (!(result is JObject)) 
			{
				Search (name);
				return;
			}

			HandleResult (library.InstallPlugin ((JObject)result, version), "Installing " + name);

			library.EndCommit ();
		}

		protected void Remove (params string[] names)
		{
			library.BeginCommit();
			foreach (string name in names) {
				if (library.Settings.Debug) 
				{
					Log ("[Catalyst] Removing " + name);
				}

				HandleResult(library.RemovePlugin (name), "Removing " + name);
			}

			library.EndCommit();
		}

		protected void Validate () {
			bool errors = false;

			if (library.Settings.Debug) 
			{
				Log ("[Catalyst] Validating");
			}

			library.BeginCommit();
			HandleResult(library.Validate(), "Validating");
			errors = library.HasErrors;
			library.EndCommit();

			if (errors) 
			{
				LogWarning ("[Catalyst] Validation failed");
			} else {
				Log ("[Catalyst] Validation success!");
			}
		}

		protected void Info(string name) 
		{
			library.BeginCommit ();
            var result = library.PluginExists (name);
			if (!(result is JObject)) 
			{
				LogWarning ("[Catalyst] No plugin found");
				return;
			}

            JObject pluginInfo = (JObject)result;

			StringBuilder sb = new StringBuilder ();

			if (pluginInfo ["plugin"] == null) 
			{
				LogWarning ("[Catalyst] Plugin invalid");
			}

			name = pluginInfo ["name"].ToString();
			string desc = pluginInfo["plugin"]["description"].ToString();
			string author = pluginInfo["plugin"]["author"].ToString();
			string version = pluginInfo["plugin"]["version"].ToString();

			sb.AppendLine(name + " by " + author);
			sb.AppendLine("Version: " + version);
			sb.AppendLine("Description: " + desc);

			var requires = pluginInfo ["plugin"] ["require"];
			if (requires != null) 
			{
				sb.AppendLine("Require: "); 
				foreach (string require in requires) {
					sb.AppendLine(require);	
				}
			}

			Log (sb.ToString());

			library.EndCommit();	
		}

		void HandleResult(object result, string action) 
		{
			if ((result is string || result is bool || result == null) && !library.IsCommitting) 
			{
				if (result is string) 
				{
					LogError (result.ToString ());
				} 
				else if (result is bool && (bool)result == false) 
				{
					LogError ("Unknown Error: " + action);
				}
			} 
			else if(result is bool && (bool)result == true) 
			{
				Log (action + " queued.");
			}
		}

        protected void Log(string format, params object[] args)
        {
            Interface.Oxide.LogInfo("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        protected void LogWarning(string format, params object[] args)
        {
            Interface.Oxide.LogWarning("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        protected void LogError(string format, params object[] args)
        {
            Interface.Oxide.LogError("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        protected void Puts(string format, params object[] args)
        {
            Interface.Oxide.LogInfo("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }
	}
}