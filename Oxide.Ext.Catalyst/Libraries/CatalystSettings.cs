// COPYRIGHT 2016 RUSTSERVERS.IO
using System;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Plugins;

namespace Oxide.Ext.Catalyst
{
	public class CatalystSettings
	{
		public List<string> SourceList { get; set; }

		public bool Debug { get; set; }
		public Dictionary<string, string> Require { get; set; }
		public Dictionary<string, string> RequireDev { get; set; }
		public string Version { get; set; }
	}
}

