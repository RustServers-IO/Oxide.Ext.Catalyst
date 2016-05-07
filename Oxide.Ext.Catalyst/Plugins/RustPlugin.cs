// COPYRIGHT 2016 RUSTSERVERS.IO
using System;
using Oxide.Ext.Catalyst.Plugins;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Libraries;

namespace Oxide.Ext.Catalyst.Plugins
{
	public class RustPlugin : CatalystPlugin
	{
		public RustPlugin(CatalystExtension extension) : base( extension ) { }

		Command cmd = Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Command> ();

		protected override void AddConsoleCommand(string command, string methodName)
        {
            cmd.AddConsoleCommand(command, this, methodName);
        }

        [HookMethod("ccVersion")]
        protected override void ccStatus (ConsoleSystem.Arg arg)
        {
            if (arg.connection != null) {
                arg.ReplyWith ("Permission Denied");
                return;
            }

            Version();
        }

        [HookMethod("ccStatus")]
        protected override void ccStatus (ConsoleSystem.Arg arg)
        {
            if (arg.connection != null) {
                arg.ReplyWith ("Permission Denied");
                return;
            }

            Status();
        }

        [HookMethod("ccSync")]
        protected override void ccSync (ConsoleSystem.Arg arg)
        {
            if (arg.connection != null) {
                arg.ReplyWith ("Permission Denied");
                return;
            }

            Sync();
        }

		[HookMethod("ccDebug")]
		protected override void ccDebug (ConsoleSystem.Arg arg)
		{
			if (arg.connection != null) {
				arg.ReplyWith ("Permission Denied");
				return;
			}

			if (arg.Args != null && arg.Args.Length == 1) {
				Debug(arg.Args[0]);
			} else {
				Debug();
			}
		}

		[HookMethod("ccConfig")]
        protected override void ccConfig (ConsoleSystem.Arg arg)
		{
			if (arg.connection != null) 
			{
				arg.ReplyWith ("Permission Denied");
				return;
			}

			if (arg.Args != null && arg.Args.Length == 2 || arg.Args.Length == 3) 
			{
				string plugin = arg.Args[0];
				string key = arg.Args [1];
				string value = null;
				if (arg.Args.Length > 2) 
				{
					value = arg.Args[2];
				} 

				Configure(plugin, key, value);
			}
			else 
			{
				Log ("catalyst.config setting [value]");
			}
		}

		[HookMethod("ccSource")]
        protected override void ccSource (ConsoleSystem.Arg arg)
		{
			if (arg.connection != null)
			{
				arg.ReplyWith("Permission Denied");
				return;
			}

			if (arg.Args != null && arg.Args.Length == 1) 
			{
				Source (arg.Args [0]);
			} 
			else 
			{
				Source ();				
			}
		}

		[HookMethod("ccSearch")]
        protected override void ccSearch (ConsoleSystem.Arg arg)
		{
			if (arg.connection != null) 
			{
				arg.ReplyWith ("Permission Denied");
				return;
			}

			if (arg.Args != null && arg.Args.Length > 0) 
			{
				string terms = string.Join(" ", arg.Args).Trim();;
				Search(terms);
			}
			else
			{
				Log("catalyst.search PluginName [PluginName] ...");
			}
		}

		[HookMethod("ccUpdate")]
        protected override void ccUpdate (ConsoleSystem.Arg arg)
		{
			if (arg.connection != null) 
			{
				arg.ReplyWith ("Permission Denied");
				return;
			}

			if (arg.Args != null && arg.Args.Length > 0 && arg.Args [0] != "*") 
			{
				Update(arg.Args);
			} 
			else 
			{
				Update();
			}
		}

		[HookMethod("ccRequire")]
        protected override void ccRequire (ConsoleSystem.Arg arg)
		{
			if (arg.connection != null) 
			{
				arg.ReplyWith ("Permission Denied");
				return;
			}

			if (arg.Args != null && arg.Args.Length > 0) 
			{
				string name = arg.Args [0];
				string version = "*";
				if (arg.Args.Length == 2) 
				{
					version = arg.Args[1];
				}

				Require(name, version);
			} 
			else 
			{
				Log ("catalyst.require PluginName [PluginName] ..." );
			}
		}

		[HookMethod("ccRemove")]
        protected override void ccRemove (ConsoleSystem.Arg arg)
		{
			if (arg.connection != null) {
				arg.ReplyWith ("Permission Denied");
				return;
			}

			if (arg.Args != null && arg.Args.Length > 0) {
				Remove(arg.Args);
			} else {
				Log("catalyst.remove PluginName [PluginName] [...]");
			}
		}

		[HookMethod("ccValidate")]
        protected override void ccValidate(ConsoleSystem.Arg arg)
		{
			if (arg.connection != null)
			{
				arg.ReplyWith("Permission Denied");
				return;
			}

			Validate();
		}

		[HookMethod("ccInfo")]
        protected override void ccInfo (ConsoleSystem.Arg arg)
		{
			if (arg.connection != null) 
			{
				arg.ReplyWith ("Permission Denied");
				return;
			}

			if (arg.Args != null && arg.Args.Length == 1) 
			{
				string name = arg.Args [0];

				Info(name);
			} 
			else 
			{
				Log ("catalyst.info PluginName");
			}
		}
	}
}

