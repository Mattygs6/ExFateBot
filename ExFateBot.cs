namespace ExFateBot
{
	using System;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;

	using Clio.Utilities;

	using ff14bot;
	using ff14bot.AClasses;
	using ff14bot.Behavior;
	using ff14bot.BotBases;
	using ff14bot.Enums;
	using ff14bot.Helpers;
	using ff14bot.Managers;
	using ff14bot.Navigation;
	using ff14bot.Objects;
	using ff14bot.RemoteWindows;
	using ff14bot.Settings;
	using ff14bot.Windows.FateBotSettingsWindow;

	using TreeSharp;

	using Action = TreeSharp.Action;

	public class ExFateBot : BotBase
	{
		private FateBotSettingsWindow settingsWindow;
		private static readonly DefaultFateScoreProvider FateScoreProvider = new DefaultFateScoreProvider();

		public static readonly Stopwatch FateTimer = new Stopwatch();

		internal static FateData FateData;

		private static bool shouldMoveIntoFate;

		private readonly FateIconType[] fateIconType =
			{
				FateIconType.Battle, FateIconType.Boss, FateIconType.ProtectNPC2,
				FateIconType.ProtectNPC
			};

		private Composite composite;

		static ExFateBot()
		{
		}

		public string Description
		{
			get
			{
				return "FateBot by ExMatt";
			}
		}

		public override string EnglishName
		{
			get
			{
				return "ExFateBot";
			}
		}

		public override bool IsAutonomous
		{
			get
			{
				return true;
			}
		}

		public override bool IsPrimaryType
		{
			get
			{
				return true;
			}
		}

		public override string Name
		{
			get
			{
				return "ExFateBot";
			}
		}

		public override PulseFlags PulseFlags
		{
			get
			{
				return PulseFlags.All;
			}
		}

		public override bool RequiresProfile
		{
			get
			{
				return false;
			}
		}

		public override Composite Root
		{
			get
			{
				return this.composite;
			}
		}

		public override bool WantButton
		{
			get
			{
				return true;
			}
		}

		internal bool Withinfate
		{
			get
			{
				if (!FateManager.WithinFate)
				{
					return false;
				}
				if (FateData == null)
				{
					return false;
				}
				var location = Core.Player.Location;
				return FateData.Within2D(location);
			}
		}

		private Composite BotLogic
		{
			get
			{
				ContextChangeHandler unit = obj => Poi.Current.Unit as BattleCharacter;
				Composite[] composite0 = { null, null, null, null, null, null };
				composite0[0] = new Decorator(
					obj =>
					{
						if (FateData == null)
						{
							return false;
						}
						return !FateData.IsValid;
					},
					new Action(obj => FateData = null));
				composite0[1] = new Decorator(
					obj =>
					{
						if (!this.Withinfate || FateData == null)
						{
							return false;
						}
						return Core.Player.ClassLevel > FateData.MaxLevel;
					},
					new Action(obj => ToDoList.LevelSync()));
				composite0[2] = new Decorator(
					obj =>
					{
						if (!shouldMoveIntoFate)
						{
							return false;
						}
						if (FateData == null || !FateData.IsValid || FateData.Status == FateStatus.COMPLETE)
						{
							return true;
						}
						return FateData.Location.Distance2D(Core.Player.Location) <= FateData.HotSpot.Radius * 0.9;
					},
					new Action(obj => shouldMoveIntoFate = false));
				composite0[3] = new Decorator(
					obj => shouldMoveIntoFate,
					CommonBehaviors.MoveAndStop(
						obj => FateData.Location,
						FateData != null ? FateData.HotSpot.Radius * 0.8f : 5f,
						true,
						"Moving back into fate radius"));
				composite0[4] = new Decorator(
					obj =>
					{
						var bc = obj as BattleCharacter;
						if (Poi.Current.Type != PoiType.Kill || bc == null || !bc.IsValid
							|| !bc.IsFate || !bc.IsAlive || bc.CanAttack
							|| this.Withinfate || FateData == null || !FateData.IsValid)
						{
							return false;
						}

						return !bc.IsFateGone;
					},
					new Action(obj => shouldMoveIntoFate = true));
				return new PrioritySelector(unit, composite0);
			}
		}

		private Composite CreateSetFatePoi
		{
			get
			{
				return new HookExecutor(
					"SetFatePoi",
					"Handles deciding which fate to move towards.",
					new PrioritySelector(new Decorator(
							obj =>
							{
								if (FateData == null)
								{
									return false;
								}
								return FateData.IsValid;
							},
							new Action(obj => Poi.Current = new Poi(FateData, PoiType.Fate))),
						new Decorator(
							obj =>
							{
								if (FateData == null)
								{
									return true;
								}
								return !FateData.IsValid;
							},
							new ActionRunCoroutine(obj => ResolveFate()))));
			}
		}

		private Composite CreateSetRestPoi
		{
			get
			{
				return new HookExecutor(
					"SetRestPoi",
					"Handles deciding which fate to move towards.",
					new PrioritySelector(new Decorator(
							obj =>
							{
								if (FateData != null)
								{
									return false;
								}
								return FatebotSettings.Instance.IdleAction != FateIdleAction.Nothing;
							},
							new ActionRunCoroutine(obj => HandleFateIdleAction()))));
			}
		}

		public new void Dispose() { }

		public override void Initialize() { }

		public override void OnButtonPress()
		{
			if (this.settingsWindow == null || !this.settingsWindow.IsLoaded)
			{
				this.settingsWindow = new FateBotSettingsWindow();
			}
			this.settingsWindow.Show();
		}

		public new void Pulse() { }

		public static void SetFate(FateData fate)
		{
			FateData = fate;
			Poi.Current = new Poi(fate, PoiType.Fate);
		}

		public override void Start()
		{
			Navigator.PlayerMover = new SlideMover();
			Navigator.NavigationProvider = new GaiaNavigator();
			GameSettingsManager.FaceTargetOnAction = true;
			GameSettingsManager.FlightMode = true;
			TreeHooks.Instance.ClearAll();
			this.composite = BrainBehavior.CreateBrain();
			CharacterSettings.Instance.AutoEquip = FatebotSettings.Instance.UseAutoEquip;
			TreeHooks.Instance.ReplaceHook("SelectPoiType", this.SelectPoiType());
			TreeHooks.Instance.AddHook("PoiAction", this.SetFatePoi());
			TreeHooks.Instance.AddHook("PoiAction2", this.SetRestPoi());
			TreeHooks.Instance.AddHook("TreeStart", this.BotLogic);

			CombatTargeting.Instance.Provider = new ExFateCombatTargetingProvider(); // TODO Make new targeting
			FateData = null;
		}

		public override void Stop()
		{
			CharacterSettings.Instance.AutoEquip = false;
			CombatTargeting.Instance.Provider = new DefaultCombatTargetingProvider();
			var navigationProvider = Navigator.NavigationProvider as GaiaNavigator;
			if (navigationProvider != null)
			{
				navigationProvider.Dispose();
			}

			Navigator.NavigationProvider = null;
		}

		public override string ToString()
		{
			return this.Name;
		}

		protected async Task<bool> Main()
		{
			return true;
		}

		private async Task<bool> HandleFateIdleAction()
		{
			// TODO: Set idle action location (grind area, aethernet etc)

			return true;
		}

		private Composite SelectPoiType()
		{
			Composite[] hookExecutor =
				{
					new HookExecutor("SetDeathPoi"),
					new HookExecutor("SetCombatPoi"), this.CreateSetFatePoi, this.CreateSetRestPoi
				};
			return new PrioritySelector(hookExecutor);
		}

		private Composite SetFatePoi()
		{
			return new HookExecutor(
				"FatePoi",
				"A hook location that executes fate logic.",
				new Decorator(obj => Poi.Current.Type == PoiType.Fate, new PrioritySelector(new Decorator(
						obj =>
						{
							if (FateData != null && !FateData.IsValid)
							{
								return true;
							}
							return FateData == null;
						},
						new Action(obj => Poi.Clear("Fate is no longer valid"))),
					new Decorator(obj => !Core.Player.IsMounted && !this.Withinfate, CommonBehaviors.CreateMountBehavior()),
					new Decorator(
						obj =>
						{
							if (!Core.Player.IsMounted)
							{
								return true;
							}
							return this.Withinfate;
						},
						new HookExecutor("SetCombatPoi")),
					CommonBehaviors.MoveAndStop(
						obj => FateData.Location,
						obj => FateData.Radius / 2f,
						true,
						"Moving to fate"))));
		}

		private Composite SetRestPoi()
		{
			return new HookExecutor(
				"RestPoi",
				"A hook location that executes downtime logic.",
				new Decorator(obj => Poi.Current.Type == PoiType.Wait, new PrioritySelector(new Decorator(
						obj =>
						{
							if (FateData == null)
							{
								return true;
							}
							return !FateData.IsValid;
						},
						new ActionRunCoroutine(obj => ResolveFate())),
					new Decorator(
						obj => FateData != null,
						new Action(obj => Poi.Clear("Time for resting is over!"))),
					new Decorator(
						obj =>
						{
							if (!Core.Player.IsMounted)
							{
								return true;
							}
							return Poi.Current.Location.Distance(Core.Player.Location) <= 25f;
						},
						new HookExecutor("SetCombatPoi")),
					new Decorator(
						obj => Poi.Current.Location.Distance(Core.Player.Location) > 15f,
						CommonBehaviors.CreateMountBehavior()),
					new Decorator(
						obj => Poi.Current.Location != Vector3.Zero,
						new Action(obj => Navigator.MoveToPointWithin(Poi.Current.Location, 15f, "Moving to rest location"))))));
		}

		private async Task<bool> ResolveFate()
		{
			if (!FateTimer.IsRunning)
			{
				FateTimer.Restart();
			}

			if (FateTimer.ElapsedMilliseconds < 3000)
			{
				return false;
			}

			FateTimer.Restart();

			FateData fate;
			var singleFate = FatebotSettings.Instance.ThisFateOnly;
			if (!string.IsNullOrWhiteSpace(singleFate))
			{
				fate =
					FateManager.ActiveFates.FirstOrDefault(
						f => string.Equals(f.Name, singleFate, StringComparison.InvariantCultureIgnoreCase));

				if (fate == null)
				{
					return false;
				}

				FateData = fate;
				return true;
			}

			fate = FateScoreProvider.GetObjectsByWeight(FateManager.ActiveFates).FirstOrDefault(ShouldSelectFate);
			if (fate == null)
			{
				return false;
			}

			FateData = fate;
			return true;
		}

		private bool ShouldSelectFate(FateData fatedata)
		{
			if (Blacklist.Contains(fatedata.Id, BlacklistFlags.Node))
			{
				if (FatebotSettings.Instance.VerboseLogging)
				{
					Logging.WriteVerbose(
						"{0} - Blacklist.Contains(fate.Id, BlacklistFlags.Node) - We couldn't find a path most likely",
						fatedata.Name);
				}

				return false;
			}

			var instance = FatebotSettings.Instance;
			if (instance.LevelCheck
				&& (fatedata.Level < instance.MinLevel || Core.Player.ClassLevel + instance.MaxLevel < fatedata.Level))
			{
				if (FatebotSettings.Instance.VerboseLogging)
				{
					object[] name = { fatedata.Name, null };
					name[1] = (fatedata.Level < instance.MinLevel
									? "fate.Level < settings.MinLevel"
									: "(Core.Player.ClassLevel + settings.MaxLevel) < fate.Level");
					Logging.WriteVerbose("{0} {1}", name);
				}

				return false;
			}

			var icon = fatedata.Icon;
			if (!this.fateIconType.Contains(icon))
			{
				if (FatebotSettings.Instance.VerboseLogging)
				{
					object[] objArray = { fatedata.Name, fatedata.Icon };
					Logging.WriteVerbose("{0} - {1} - !_goodFates.Contains(fateIconType)", objArray);
				}

				return false;
			}

			if (instance.BlackListedFates.Contains(fatedata.Name))
			{
				if (FatebotSettings.Instance.VerboseLogging)
				{
					Logging.WriteVerbose("{0} - settings.BlackListedFates.Contains(fate.Name)", fatedata.Name);
				}

				return false;
			}

			if (icon == FateIconType.Boss)
			{
				if (!instance.BossEnabled)
				{
					return false;
				}

				if (FatebotSettings.Instance.IgnorePercentageFates.Contains(fatedata.Id))
				{
					return true;
				}

				if (fatedata.Progress < instance.BossPercentRequired)
				{
					if (FatebotSettings.Instance.VerboseLogging)
					{
						Logging.WriteVerbose("{0} - fate.Progress < settings.BossPercentRequired", fatedata.Name);
					}

					return false;
				}
			}

			if (icon != FateIconType.Battle || instance.MonsterSlayingEnabled)
			{
				if (icon == FateIconType.ProtectNPC2 && !instance.EscortEnabled)
				{
					return false;
				}

				return true;
			}

			if (FatebotSettings.Instance.VerboseLogging)
			{
				Logging.WriteVerbose("{0} - fateIconType == FateIconType.Battle && !settings.MonsterSlayingEnabled", fatedata.Name);
			}

			return false;
		}
	}
}