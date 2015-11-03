namespace ExFateBot
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Configuration;
	using System.IO;

	using Clio.Utilities;

	using ff14bot.BotBases;
	using ff14bot.Helpers;
	using ff14bot.Settings;

	internal class ExFateBotSettings : JsonSettings
	{
		private static ExFateBotSettings instance;

		public ObservableCollection<string> BlackListedFates;

		public Dictionary<ushort, Vector3> IdleLocations;

		public List<uint> IgnorePercentageFates;

		private bool bossEnabled;

		private float bossPercentRequired;

		private bool escortEnabled;

		private FateIdleAction fateIdleAction;

		private bool levelCheck;

		private int maxLevel;

		private int minLevel;

		private bool monsterSlayingEnabled;

		private string thisFateOnly;

		private bool useAutoEquip;

		private bool verboseLogging;

		public ExFateBotSettings()
			: base(Path.Combine(CharacterSettingsDirectory, "ExFateBotSettings.json"))
		{
			if (this.BlackListedFates == null)
			{
				this.BlackListedFates = new ObservableCollection<string>();
			}
			if (this.IdleLocations == null)
			{
				this.IdleLocations = new Dictionary<ushort, Vector3>();
			}
			if (this.IgnorePercentageFates == null)
			{
				this.IgnorePercentageFates = new List<uint> { 503, 504 };
			}
		}

		[DefaultValue(true)]
		[Setting]
		public bool BossEnabled
		{
			get
			{
				return bossEnabled;
			}
			set
			{
				bossEnabled = value;
				Save();
			}
		}

		[DefaultValue(60f)]
		[Setting]
		public float BossPercentRequired
		{
			get
			{
				return bossPercentRequired;
			}
			set
			{
				bossPercentRequired = value;
				Save();
			}
		}

		[DefaultValue(true)]
		[Setting]
		public bool EscortEnabled
		{
			get
			{
				return escortEnabled;
			}
			set
			{
				escortEnabled = value;
				Save();
			}
		}

		[DefaultValue(FateIdleAction.ReturnToAetheryte)]
		[Setting]
		public FateIdleAction IdleAction
		{
			get
			{
				return fateIdleAction;
			}
			set
			{
				fateIdleAction = value;
				Save();
			}
		}

		public static ExFateBotSettings Instance
		{
			get
			{
				return instance ?? (instance = new ExFateBotSettings());
			}
		}

		[DefaultValue(false)]
		[Setting]
		public bool LevelCheck
		{
			get
			{
				return levelCheck;
			}
			set
			{
				levelCheck = value;
				Save();
			}
		}

		[DefaultValue(3)]
		[Setting]
		public int MaxLevel
		{
			get
			{
				return maxLevel;
			}
			set
			{
				maxLevel = value;
				Save();
			}
		}

		[DefaultValue(0)]
		[Setting]
		public int MinLevel
		{
			get
			{
				return minLevel;
			}
			set
			{
				minLevel = value;
				Save();
			}
		}

		[DefaultValue(true)]
		[Setting]
		public bool MonsterSlayingEnabled
		{
			get
			{
				return monsterSlayingEnabled;
			}
			set
			{
				monsterSlayingEnabled = value;
				Save();
			}
		}

		[DefaultValue("")]
		[Setting]
		public string ThisFateOnly
		{
			get
			{
				return thisFateOnly;
			}
			set
			{
				thisFateOnly = value;
				Save();
			}
		}

		[DefaultValue(false)]
		public bool UseAutoEquip
		{
			get
			{
				return useAutoEquip;
			}
			set
			{
				useAutoEquip = value;
				CharacterSettings.Instance.AutoEquip = useAutoEquip;
				Save();
			}
		}

		[DefaultValue(true)]
		[Setting]
		public bool VerboseLogging
		{
			get
			{
				return verboseLogging;
			}

			set
			{
				verboseLogging = value;
				Save();
			}
		}
	}
}