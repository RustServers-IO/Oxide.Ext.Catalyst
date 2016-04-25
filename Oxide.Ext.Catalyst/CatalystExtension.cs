// COPYRIGHT 2016 RUSTSERVERS.IO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;

namespace Oxide.Ext.Catalyst
{
	public class CatalystExtension : Extension
	{
		public static CatalystExtension Instance { get; private set; }
		public static Libraries.Catalyst CatalystLibrary { get; private set; }
		public static Plugins.CatalystPlugin CatalystPlugin { get; private set; }

		public CatalystExtension(ExtensionManager manager) : base(manager)
		{
			CatalystExtension.Instance = this;
		}

		public override string Name => "Catalyst";

		public override VersionNumber Version => new VersionNumber (
			(ushort) Assembly.GetExecutingAssembly().GetName().Version.Major,
			(ushort) Assembly.GetExecutingAssembly().GetName().Version.Minor,
			(ushort) Assembly.GetExecutingAssembly().GetName().Version.Build
		);

		public override string Author => "Calytic";

		public override void Load()
		{
			Manager.RegisterPluginLoader(new Plugins.PluginLoader(this));
			CatalystLibrary = new Libraries.Catalyst(this);
			Manager.RegisterLibrary("Catalyst", CatalystLibrary);
		}

		public override void LoadPluginWatchers(string plugindir)
		{
			CatalystLibrary?.Initialize();
		}

		public override void OnModLoad()
		{

		}

		public override void OnShutdown()
		{
			CatalystLibrary?.Shutdown();
		}
	}
}
