## Sourcing Catalyst

Instructions on how to duplicate the RESTful web service used by Catalyst.

### Warning

The standards set below are still subject to substantial change during the ALPHA stage of Catalyst development.

### Plugin Information

**URL**: ``http://example.org/p/{PluginName}.json``
**URL**: ``http://example.org/p/{PluginName}/{PluginVersion}.json`` **(still in development)**

#### Example

``http://rustservers.io/p/EntityOwner.json``

**JSON Result**:
``
{
   "name":"EntityOwner",
   "ext":"cs",
   "plugin":{
      "name":"EntityOwner",
      "description":"Modify entity ownership and cupboard\/turret authorization",
      "author":"Calytic",
      "version":"3.0.3",
      "configuration":true,
      "suggest":{
         "DeadPlayersList":"*"
      },
      "language":"cs",
      "games":[
         "rust"
      ],
      "keywords":[
         "oxide",
         "rust"
      ]
   },
   "doc":"https:\/\/raw.githubusercontent.com\/Calytic\/oxideplugins\/master\/rust\/EntityOwner.md",
   "src":"https:\/\/raw.githubusercontent.com\/Calytic\/oxideplugins\/master\/rust\/EntityOwner.cs",
   "version":"3.0.3"
}
``

``http://rustservers.io/p/HumanNPC.json``

**JSON Result**:
``
{
   "name":"HumanNPC",
   "ext":"cs",
   "plugin":{
      "name":"Human NPC - Core",
      "description":"Add Interactive Human NPC Ingame, can be modded by other plugins",
      "author":"Nogrod",
      "version":"0.3.2",
      "configuration":true,
      "require":{
         "Waypoints":"*",
         "PathFinding":"*"
      },
      "language":"cs",
      "games":[
         "rust"
      ],
      "keywords":[
         "oxide",
         "rust"
      ]
   },
   "doc":"https:\/\/raw.githubusercontent.com\/Calytic\/oxideplugins\/master\/rust\/HumanNPC.md",
   "src":"https:\/\/raw.githubusercontent.com\/Calytic\/oxideplugins\/master\/rust\/HumanNPC.cs",
   "version":"0.3.2"
}
``

## Search Plugins

Provides a list of plugins where given search terms are found anywhere in the plugin name, description, documentation, or source code.

**URL**: ``http://example.org/s/{search}.json``

#### Example

``http://rustservers.io/s/Anti.json``

**JSON Result**:
``
{
   "data":[
      "AntiSpeedHack",
      "AntiCheat",
      "AntigriefJail",
      "PillsHere",
      "AntiAds",
      "AntiDecay",
      "antisuicide",
      "AntiAdminAbuse",
      "AntiOfflineRaid",
      "AntiRaidTower",
      "AntiWeaponSpeedHack",
      "AntiWounded",
      "BlockBlockElevators",
      "BoxLooters",
      "Anti-Dupe",
      "AntiChatFlood",
      "AntiGlitch"
   ]
}
``

## Using Your New Service

* Add/Remove source service where plugins made be found

  ````catalyst.source http://my.source.com````

## Development

As this service is still in development, any suggestions or proposed changes to the protocol may be submitted here in the form of an issue or pull request.