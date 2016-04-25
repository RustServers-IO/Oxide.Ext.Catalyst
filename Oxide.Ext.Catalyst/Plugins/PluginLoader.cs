// COPYRIGHT 2016 RUSTSERVERS.IO
using System;
using System.Collections.Generic;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

namespace Oxide.Ext.Catalyst.Plugins
{
	public class PluginLoader : Oxide.Core.Plugins.PluginLoader
	{
	    private CatalystExtension Extension;
	    private Logger logger;

		public PluginLoader(CatalystExtension extension)
	    {
			this.Extension = extension;
			this.logger = (Logger) Interface.Oxide.RootLogger;
	    }

	    public override IEnumerable<string> ScanDirectory(string directory)
	    {
			return (IEnumerable<string>) new string[1]
			{
				"Catalyst"
			};
	    }

	    public override Plugin Load (string directory, string name)
		{
			switch (name) 
			{
				case "Catalyst":
					Plugins.RustPlugin catalystPlugin = new Plugins.RustPlugin (Extension);
					LoadedPlugins.Add (name, catalystPlugin);
					return (Plugin) catalystPlugin;
			    default:
		      		return (Plugin) null;
			}
	    }
	}
}

