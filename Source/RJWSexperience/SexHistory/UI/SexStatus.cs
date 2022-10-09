﻿using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RJWSexperience.SexHistory.UI
{
	public enum PartnerOrderMode
	{
		Normal = 0,
		Recent = 1,
		Most = 2,
		Name, MaxValue = 3
	};

	public static class PartnerOrderModeExtension
	{
		public static PartnerOrderMode Next(this PartnerOrderMode mode)
		{
			return (PartnerOrderMode)(((int)mode + 1) % ((int)PartnerOrderMode.MaxValue + 1));
		}
	}

	public class SexStatusWindow : Window
	{
		public const float WINDOW_WIDTH = 900f;
		public const float WINDOW_HEIGHT = 600f;
		public const float FONTHEIGHT = RJWUIUtility.FONTHEIGHT;
		public const float CARDHEIGHT = RJWUIUtility.CARDHEIGHT;
		public const float LISTPAWNSIZE = RJWUIUtility.LISTPAWNSIZE;
		public const float BASESAT = RJWUIUtility.BASESAT;
		public const float ICONSIZE = RJWUIUtility.ICONSIZE;

		public static readonly int[] Sextype =
		{
			(int)xxx.rjwSextype.Vaginal,
			(int)xxx.rjwSextype.Anal,
			(int)xxx.rjwSextype.Oral,
			(int)xxx.rjwSextype.Fellatio,
			(int)xxx.rjwSextype.Cunnilingus,
			(int)xxx.rjwSextype.DoublePenetration,
			(int)xxx.rjwSextype.Boobjob,
			(int)xxx.rjwSextype.Handjob,
			(int)xxx.rjwSextype.Footjob,
			(int)xxx.rjwSextype.Fingering,
			(int)xxx.rjwSextype.Scissoring,
			(int)xxx.rjwSextype.MutualMasturbation,
			(int)xxx.rjwSextype.Fisting,
			(int)xxx.rjwSextype.Rimming,
			(int)xxx.rjwSextype.Sixtynine
		};

		protected Pawn pawn;
		protected SexPartnerHistoryRecord selectedPawn;
		protected SexHistoryComp history;
		protected CompRJW rjwcomp;
		protected List<SexPartnerHistoryRecord> partnerList;
		protected PartnerOrderMode orderMode;

		private static GUIStyle fontStyleCenter;
		private static GUIStyle fontStyleRight;
		private static GUIStyle fontStyleLeft;
		private static GUIStyle boxStyle;
		private static GUIStyle buttonStyle;

		private static Vector2 LastWindowPosition { get; set; }
		private Vector2 scroll;

		private static void InitStyles()
		{
			if (fontStyleCenter != null)
			{
				return;
			}

			GUIStyleState fontStyleState = new GUIStyleState() { textColor = Color.white };
			GUIStyleState boxStyleState = GUI.skin.textArea.normal;
			GUIStyleState buttonStyleState = GUI.skin.button.normal;
			fontStyleCenter = new GUIStyle() { alignment = TextAnchor.MiddleCenter, normal = fontStyleState };
			fontStyleRight = new GUIStyle() { alignment = TextAnchor.MiddleRight, normal = fontStyleState };
			fontStyleLeft = new GUIStyle() { alignment = TextAnchor.MiddleLeft, normal = fontStyleState };
			boxStyle = new GUIStyle(GUI.skin.textArea) { hover = boxStyleState, onHover = boxStyleState, onNormal = boxStyleState };
			buttonStyle = new GUIStyle(GUI.skin.button) { hover = buttonStyleState, onHover = buttonStyleState, onNormal = buttonStyleState };
		}

		public SexStatusWindow(Pawn pawn, SexHistoryComp history)
		{
			this.pawn = pawn;
			this.history = history;
			this.selectedPawn = null;
			this.rjwcomp = pawn.TryGetComp<CompRJW>();
			this.partnerList = history?.PartnerList;
			orderMode = PartnerOrderMode.Recent;
			SortPartnerList(orderMode);

			soundClose = SoundDefOf.CommsWindow_Close;
			absorbInputAroundWindow = false;
			forcePause = false;
			preventCameraMotion = false;
			draggable = true;
			doCloseX = true;
		}

		protected override void SetInitialSizeAndPosition()
		{
			base.SetInitialSizeAndPosition();

			if (LastWindowPosition == Vector2.zero)
				return;

			windowRect.x = LastWindowPosition.x;
			windowRect.y = LastWindowPosition.y;
		}

		public override Vector2 InitialSize => new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);

		public override void PreOpen()
		{
			base.PreOpen();
			InitStyles();
		}

		public override void PreClose()
		{
			base.PreClose();
			LastWindowPosition = windowRect.position;
		}

		public override void DoWindowContents(Rect inRect)
		{
			if (!SexperienceMod.Settings.SelectionLocked)
			{
				List<Pawn> selected = Find.Selector.SelectedPawns;
				if (selected.Count == 1)
				{
					Pawn p = selected.First();
					if (p != pawn)
					{
						SexHistoryComp h = p.TryGetComp<SexHistoryComp>();
						if (h != null) ChangePawn(p, h);
					}
				}
			}

			DrawSexStatus(inRect, history);
		}

		public static void ToggleWindow(Pawn pawn, SexHistoryComp history)
		{
			SexStatusWindow window = (SexStatusWindow)Find.WindowStack.Windows.FirstOrDefault(x => x.GetType() == typeof(SexStatusWindow));
			if (window != null)
			{
				if (window.pawn != pawn)
				{
					SoundDefOf.TabOpen.PlayOneShotOnCamera();
					window.ChangePawn(pawn, history);
				}
			}
			else
			{
				Find.WindowStack.Add(new SexStatusWindow(pawn, history));
			}
		}

		public void ChangePawn(Pawn pawn, SexHistoryComp history)
		{
			List<Pawn> selected = Find.Selector.SelectedPawns;
			if (!selected.NullOrEmpty())
			{
				foreach (Pawn p in selected)
				{
					Find.Selector.Deselect(p);
				}
			}

			this.pawn = pawn;
			this.history = history;
			this.selectedPawn = null;
			this.rjwcomp = pawn.TryGetComp<CompRJW>();
			this.partnerList = history?.PartnerList;
			if (!pawn.DestroyedOrNull() && Find.CurrentMap == pawn.Map) Find.Selector.Select(pawn);
			SortPartnerList(orderMode);
		}

		public void SortPartnerList(PartnerOrderMode mode)
		{
			if (partnerList.NullOrEmpty()) return;
			switch (mode)
			{
				default:
					partnerList = history?.PartnerList;
					break;
				case PartnerOrderMode.Recent:
					partnerList.Sort(new SexPartnerHistoryRecord.RecentOrderComparer());
					break;
				case PartnerOrderMode.Most:
					partnerList.Sort(new SexPartnerHistoryRecord.MostOrderComparer());
					break;
				case PartnerOrderMode.Name:
					partnerList.Sort(new SexPartnerHistoryRecord.NameOrderComparer());
					break;
			}
		}

		/// <summary>
		/// Main contents
		/// </summary>
		protected void DrawSexStatus(Rect mainrect, SexHistoryComp history)
		{
			float sectionwidth = mainrect.width / 3;

			Rect leftRect = new Rect(mainrect.x, mainrect.y, sectionwidth, mainrect.height);
			Rect centerRect = new Rect(mainrect.x + sectionwidth, mainrect.y, sectionwidth, mainrect.height);
			Rect rightRect = new Rect(mainrect.x + (sectionwidth * 2), mainrect.y, sectionwidth, mainrect.height);

			if (history != null)
			{
				//Left section
				DrawBaseSexInfoLeft(leftRect.ContractedBy(4f));

				//Center section
				DrawBaseSexInfoCenter(centerRect.ContractedBy(4f), history.parent as Pawn);

				//Right section
				DrawBaseSexInfoRight(rightRect.ContractedBy(4f));
			}
		}

		protected void DrawInfoWithPortrait(Rect rect, SexPartnerHistoryRecord history, string tooltip = "")
		{
			Widgets.DrawMenuSection(rect);
			string str = tooltip;
			Rect portraitRect = new Rect(rect.x, rect.y, rect.height - FONTHEIGHT, rect.height - FONTHEIGHT);
			Rect nameRect = new Rect(rect.x + portraitRect.width, rect.y, rect.width - portraitRect.width, FONTHEIGHT);
			Rect sexinfoRect = new Rect(rect.x + portraitRect.width, rect.y + FONTHEIGHT, rect.width - portraitRect.width, FONTHEIGHT);
			Rect sexinfoRect2 = new Rect(rect.x + portraitRect.width, rect.y + (FONTHEIGHT * 2), rect.width - portraitRect.width, FONTHEIGHT);
			Rect bestsexRect = new Rect(rect.x + 2f, rect.y + (FONTHEIGHT * 3), rect.width - 4f, FONTHEIGHT - 2f);

			if (history != null)
			{
				if (history.Incest) str += " - " + Keyed.Incest;
				Pawn partner = history.Partner;
				DrawPawn(portraitRect, history);
				Widgets.DrawHighlightIfMouseover(portraitRect);
				if (Widgets.ButtonInvisible(portraitRect))
				{
					SexHistoryComp pawnhistory = partner?.TryGetComp<SexHistoryComp>();
					if (pawnhistory != null)
					{
						ChangePawn(partner, pawnhistory);
						SoundDefOf.Click.PlayOneShotOnCamera();
					}
					else
					{
						SoundDefOf.ClickReject.PlayOneShotOnCamera();
					}
				}

				string rapeInfo = "";
				if (history.Raped > 0) rapeInfo += Keyed.RS_Raped + history.Raped + " ";
				if (history.RapedMe > 0) rapeInfo += Keyed.RS_RapedMe + history.RapedMe;

				GUI.Label(nameRect, partner?.Name?.ToStringFull ?? history.Label.CapitalizeFirst(), fontStyleLeft);
				GUI.Label(sexinfoRect, Keyed.RS_Sex_Count + history.TotalSexCount + " " + rapeInfo, fontStyleLeft);
				GUI.Label(sexinfoRect2, Keyed.RS_Orgasms + history.OrgasmCount, fontStyleLeft);
				GUI.Label(sexinfoRect2, pawn.GetRelationsString(partner) + " ", fontStyleRight);
				float p = history.BestSatisfaction / BASESAT;
				FillableBarLabeled(bestsexRect, String.Format(Keyed.RS_Best_Sextype + ": {0}", Keyed.Sextype[(int)history.BestSextype]), p / 2, HistoryUtility.SextypeColor[(int)history.BestSextype], Texture2D.blackTexture, null, String.Format("{0:P2}", p));

				if (history.IamFirst)
					str += "\n" + Keyed.RS_LostVirgin(history.Label, pawn.LabelShort);
				if (history.BestSexTickAbs != 0)
					str += "\n" + Keyed.RS_HadBestSexDaysAgo(history.BestSexElapsedTicks.ToStringTicksToDays() + " " + Keyed.RS_Ago);

				TooltipHandler.TipRegion(rect, str);
			}
			else
			{
				Widgets.DrawTextureFitted(portraitRect, HistoryUtility.UnknownPawn, 1.0f);
				Widgets.Label(nameRect, Keyed.Unknown);
				Widgets.Label(sexinfoRect, Keyed.RS_Sex_Count + "?");
				Widgets.Label(sexinfoRect2, Keyed.RS_Orgasms + "?");
				FillableBarLabeled(bestsexRect, String.Format(Keyed.RS_Best_Sextype + ": {0}", Keyed.Sextype[(int)xxx.rjwSextype.None]), 0, Texture2D.linearGrayTexture, Texture2D.blackTexture);
			}
		}

		protected void DrawSexInfoCard(Rect rect, SexPartnerHistoryRecord history, string label, string tooltip, string rightlabel = "")
		{
			Rect labelRect = new Rect(rect.x, rect.y, rect.width, FONTHEIGHT);
			Rect infoRect = new Rect(rect.x, rect.y + FONTHEIGHT, rect.width, rect.height - FONTHEIGHT);
			GUI.Label(labelRect, label, fontStyleLeft);
			GUI.Label(labelRect, rightlabel, fontStyleRight);
			DrawInfoWithPortrait(infoRect, history, tooltip);
		}

		/// <summary>
		/// Right section
		/// </summary>
		protected void DrawBaseSexInfoRight(Rect rect)
		{
			Listing_Standard listmain = new Listing_Standard();
			listmain.Begin(rect.ContractedBy(4f));
			DrawSexInfoCard(listmain.GetRect(CARDHEIGHT), history.GetRecentPartnersHistory, Keyed.RS_Recent_Sex_Partner, Keyed.RS_Recent_Sex_Partner_ToolTip, RJWUIUtility.GetSexDays(history.RecentSexTickAbs));
			DrawSexInfoCard(listmain.GetRect(CARDHEIGHT), history.GetFirstPartnerHistory, Keyed.RS_First_Sex_Partner, Keyed.RS_First_Sex_Partner_ToolTip, RJWUIUtility.GetSexDays(history.FirstSexTickAbs));
			DrawSexInfoCard(listmain.GetRect(CARDHEIGHT), history.GetMostPartnerHistory, Keyed.RS_Most_Sex_Partner, Keyed.RS_Most_Sex_Partner_ToolTip, RJWUIUtility.GetSexDays(history.MostSexTickAbs));
			DrawSexInfoCard(listmain.GetRect(CARDHEIGHT), history.GetBestSexPartnerHistory, Keyed.RS_Best_Sex_Partner, Keyed.RS_Best_Sex_Partner_ToolTip, RJWUIUtility.GetSexDays(history.BestSexTickAbs));
			GUI.Label(listmain.GetRect(FONTHEIGHT), Keyed.RS_PreferRace, fontStyleLeft);
			DrawPreferRace(listmain.GetRect(66f + 15f));
			listmain.GetRect(15f);
			listmain.End();
		}

		protected void DrawPreferRace(Rect rect)
		{
			Widgets.DrawMenuSection(rect);
			Rect portraitRect = new Rect(rect.x, rect.y, rect.height - 15f, rect.height - 15f);
			Rect infoRect1 = new Rect(rect.x + portraitRect.width, rect.y, rect.width - portraitRect.width, FONTHEIGHT);
			Rect infoRect2 = new Rect(rect.x + portraitRect.width, rect.y + FONTHEIGHT, rect.width - portraitRect.width, FONTHEIGHT);
			Rect infoRect3 = new Rect(rect.x + portraitRect.width, rect.y + (FONTHEIGHT * 2), rect.width - portraitRect.width - 2f, FONTHEIGHT);

			if (history.PreferRace != null)
			{
				Widgets.DrawTextureFitted(portraitRect, RJWUIUtility.GetRaceIcon(history.PreferRacePawn, portraitRect.size), 1.0f);
				GUI.Label(infoRect1, history.PreferRace?.label.CapitalizeFirst() ?? Keyed.None, fontStyleLeft);
				GUI.Label(infoRect2, Keyed.RS_Sex_Count + history.PreferRaceSexCount, fontStyleLeft);
				if (history.PreferRace != pawn.def)
				{
					if (history.PreferRace.race.Animal ^ pawn.def.race.Animal)
					{
						GUI.Label(infoRect1, Keyed.RS_Bestiality + " ", fontStyleRight);
						FillableBarLabeled(infoRect3, Keyed.RS_Sex_Info(Keyed.RS_Bestiality, history.BestialityCount.ToString()), history.BestialityCount / 100f, Texture2D.linearGrayTexture, Texture2D.blackTexture);
					}
					else
					{
						GUI.Label(infoRect1, Keyed.RS_Interspecies + " ", fontStyleRight);
						FillableBarLabeled(infoRect3, Keyed.RS_Sex_Info(Keyed.RS_Interspecies, history.InterspeciesCount.ToString()), history.InterspeciesCount / 100f, Texture2D.linearGrayTexture, Texture2D.blackTexture);
					}
				}
			}
			else
			{
				Widgets.DrawTextureFitted(portraitRect, HistoryUtility.UnknownPawn, 1.0f);
				GUI.Label(infoRect1, Keyed.None, fontStyleLeft);
			}
		}

		/// <summary>
		/// Center section
		/// </summary>
		protected void DrawBaseSexInfoCenter(Rect rect, Pawn pawn)
		{
			Rect portraitRect = new Rect(rect.x + (rect.width / 4), rect.y, rect.width / 2, rect.width / 1.5f);
			Rect nameRect = new Rect(portraitRect.x, portraitRect.yMax - (FONTHEIGHT * 2), portraitRect.width, FONTHEIGHT * 2);
			Rect infoRect = new Rect(rect.x, rect.y + portraitRect.height, rect.width, rect.height - portraitRect.height);
			Rect lockRect = new Rect(portraitRect.xMax - ICONSIZE, portraitRect.y, ICONSIZE, ICONSIZE);
			Rect tmp;

			if (Mouse.IsOver(portraitRect))
			{
				Configurations settings = SexperienceMod.Settings;
				Texture lockicon = settings.SelectionLocked ? HistoryUtility.Locked : HistoryUtility.Unlocked;
				Widgets.DrawTextureFitted(lockRect, lockicon, 1.0f);
				if (Widgets.ButtonInvisible(lockRect))
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					settings.SelectionLocked = !settings.SelectionLocked;
				}
			}

			GUI.Box(portraitRect, "", boxStyle);
			Widgets.DrawTextureFitted(portraitRect, PortraitsCache.Get(pawn, portraitRect.size, Rot4.South, default, 1, true, true, false, false), 1.0f);
			Widgets.DrawHighlightIfMouseover(portraitRect);
			if (Widgets.ButtonInvisible(portraitRect))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				selectedPawn = null;
			}

			GUI.Box(nameRect, "", boxStyle);
			GUI.Label(nameRect.TopHalf(), pawn.Name?.ToStringFull ?? pawn.Label, fontStyleCenter);
			if (pawn.story != null) GUI.Label(nameRect.BottomHalf(), pawn.ageTracker.AgeBiologicalYears + ", " + pawn.story.Title, fontStyleCenter);
			else GUI.Label(nameRect.BottomHalf(), pawn.ageTracker.AgeBiologicalYears + ", " + pawn.def.label, fontStyleCenter);

			Listing_Standard listmain = new Listing_Standard();
			listmain.Begin(infoRect);
			listmain.Gap(20f);
			float p;

			Trait virginity = pawn.story?.traits?.GetTrait(VariousDefOf.Virgin);
			if (virginity != null && virginity.Degree != Virginity.TraitDegree.FemaleAfterSurgery)
			{
				tmp = listmain.GetRect(FONTHEIGHT);
				GUI.color = Color.red;
				GUI.Box(tmp, "", boxStyle);
				GUI.color = Color.white;
				GUI.Label(tmp, virginity.Label, fontStyleCenter);
			}
			else
			{
				p = history.TotalSexHad;
				FillableBarLabeled(listmain.GetRect(FONTHEIGHT), String.Format(Keyed.RS_TotalSexHad + ": {0:0} ({1:0})", p, pawn.records.GetValue(xxx.CountOfSex)), p / 100, HistoryUtility.TotalSex, Texture2D.blackTexture, null, Keyed.RS_SAT_AVG(String.Format("{0:P2}", history.AVGSat)));
			}
			listmain.Gap(1f);

			tmp = listmain.GetRect(FONTHEIGHT);
			p = pawn.records.GetValue(VariousDefOf.Lust);
			FillableBarLabeled(tmp, String.Format(Keyed.Lust + ": {0:0.00}", p), Mathf.Clamp01(p.Normalization(-SexperienceMod.Settings.LustLimit * 3, SexperienceMod.Settings.LustLimit * 3)), HistoryUtility.Slaanesh, Texture2D.blackTexture, null, String.Format(xxx.sex_drive_stat.LabelCap + ": {0:P2}", pawn.Dead ? 0 : pawn.GetStatValue(xxx.sex_drive_stat)));
			listmain.Gap(1f);
			if (Mouse.IsOver(tmp))
			{
				TooltipHandler.TipRegion(tmp, RJWUIUtility.GetStatExplanation(pawn, xxx.sex_drive_stat, pawn.Dead ? 0 : pawn.GetStatValue(xxx.sex_drive_stat)));
			}

			p = history.GetBestSextype(out xxx.rjwSextype sextype) / BASESAT;
			FillableBarLabeled(listmain.GetRect(FONTHEIGHT), String.Format(Keyed.RS_Best_Sextype + ": {0}", Keyed.Sextype[(int)sextype]), p / 2, HistoryUtility.SextypeColor[(int)sextype], Texture2D.blackTexture, null, Keyed.RS_SAT_AVG(String.Format("{0:P2}", p)));
			listmain.Gap(1f);

			p = history.GetRecentSextype(out sextype) / BASESAT;
			FillableBarLabeled(listmain.GetRect(FONTHEIGHT), String.Format(Keyed.RS_Recent_Sextype + ": {0}", Keyed.Sextype[(int)sextype]), p / 2, HistoryUtility.SextypeColor[(int)sextype], Texture2D.blackTexture, null, String.Format("{0:P2}", p));
			listmain.Gap(1f);

			if (history.IncestuousCount < history.CorpseFuckCount)
			{
				p = history.CorpseFuckCount;
				FillableBarLabeled(listmain.GetRect(FONTHEIGHT), String.Format(Keyed.RS_Necrophile + ": {0}", p), p / 50, HistoryUtility.Nurgle, Texture2D.blackTexture);
				listmain.Gap(1f);
			}
			else
			{
				p = history.IncestuousCount;
				FillableBarLabeled(listmain.GetRect(FONTHEIGHT), String.Format(Keyed.Incest + ": {0}", p), p / 50, HistoryUtility.Nurgle, Texture2D.blackTexture);
				listmain.Gap(1f);
			}

			p = pawn.records.GetValue(VariousDefOf.AmountofEatenCum);
			FillableBarLabeled(listmain.GetRect(FONTHEIGHT), String.Format(Keyed.RS_Cum_Swallowed + ": {0} mL, {1} " + Keyed.RS_NumofTimes, p, pawn.records.GetValue(VariousDefOf.NumofEatenCum)), p / 1000, Texture2D.linearGrayTexture, Texture2D.blackTexture);
			listmain.Gap(1f);

			Hediff addiction = pawn.health.hediffSet.GetFirstHediffOfDef(VariousDefOf.CumAddiction)
				?? pawn.health.hediffSet.GetFirstHediffOfDef(VariousDefOf.CumTolerance);
			if (addiction != null)
			{
				p = addiction.Severity;
				FillableBarLabeled(listmain.GetRect(FONTHEIGHT), $"{addiction.Label}: {p.ToStringPercent()}", p, Texture2D.linearGrayTexture, Texture2D.blackTexture, addiction.GetTooltip(pawn, false));
			}
			else
			{
				listmain.GetRect(FONTHEIGHT);
			}
			listmain.Gap(1f);

			p = history.RapedCount;
			tmp = listmain.GetRect(FONTHEIGHT);
			if (p < history.BeenRapedCount)
			{
				p = history.BeenRapedCount;
				FillableBarLabeled(tmp, String.Format(Keyed.RS_BeenRaped + ": {0}", p), p / 50, Texture2D.grayTexture, Texture2D.blackTexture, null, String.Format(xxx.vulnerability_stat.LabelCap + ": {0:P2}", pawn.Dead ? 0 : pawn.GetStatValue(xxx.vulnerability_stat)));
				listmain.Gap(1f);
			}
			else
			{
				FillableBarLabeled(tmp, String.Format(Keyed.RS_RapedSomeone + ": {0}", p), p / 50, HistoryUtility.Khorne, Texture2D.blackTexture, null, String.Format(xxx.vulnerability_stat.LabelCap + ": {0:P2}", pawn.Dead ? 0 : pawn.GetStatValue(xxx.vulnerability_stat)));
				listmain.Gap(1f);
			}
			if (Mouse.IsOver(tmp))
			{
				TooltipHandler.TipRegion(tmp, RJWUIUtility.GetStatExplanation(pawn, xxx.vulnerability_stat, pawn.Dead ? 0 : pawn.GetStatValue(xxx.vulnerability_stat)));
			}

			p = pawn.Dead ? 0 : pawn.GetStatValue(xxx.sex_satisfaction);
			tmp = listmain.GetRect(FONTHEIGHT);
			FillableBarLabeled(tmp, String.Format(xxx.sex_satisfaction.LabelCap + ": {0:P2}", p), p / 2, HistoryUtility.Satisfaction, Texture2D.blackTexture);
			listmain.Gap(1f);
			if (Mouse.IsOver(tmp))
			{
				TooltipHandler.TipRegion(tmp, RJWUIUtility.GetStatExplanation(pawn, xxx.sex_satisfaction, pawn.Dead ? 0 : pawn.GetStatValue(xxx.sex_satisfaction)));
			}

			SkillRecord skill = pawn.skills?.GetSkill(VariousDefOf.Sex);
			p = skill?.Level ?? 0;
			tmp = listmain.GetRect(FONTHEIGHT);
			FillableBarLabeled(tmp, $"{Keyed.RS_SexSkill}: {p}, {skill?.xpSinceLastLevel / skill?.XpRequiredForLevelUp:P2}", p / 20, HistoryUtility.Tzeentch, Texture2D.blackTexture, null, $"{VariousDefOf.SexAbility.LabelCap}: {pawn.GetSexStat():P2}", HistoryUtility.GetPassionBG(skill?.passion));
			if (Mouse.IsOver(tmp))
			{
				TooltipHandler.TipRegion(tmp, RJWUIUtility.GetStatExplanation(pawn, VariousDefOf.SexAbility, pawn.GetSexStat()));
			}

			listmain.Gap(1f);

			if (selectedPawn != null) DrawSexInfoCard(listmain.GetRect(CARDHEIGHT), selectedPawn, Keyed.RS_Selected_Partner, Keyed.RS_Selected_Partner, RJWUIUtility.GetSexDays(selectedPawn.RecentSexTickAbs));
			else DrawExtraInfo(listmain.GetRect(CARDHEIGHT));

			listmain.End();
		}

		protected void DrawExtraInfo(Rect rect)
		{
			Widgets.DrawMenuSection(rect);
			Rect inRect = rect.ContractedBy(4f);
			Listing_Standard listmain = new Listing_Standard();
			listmain.Begin(inRect);
			listmain.Gap(4f);
			listmain.GetRect(FONTHEIGHT).DrawSexuality(rjwcomp);
			listmain.Gap(1f);
			listmain.GetRect(FONTHEIGHT * 3f).DrawQuirk(pawn);
			listmain.End();
		}

		/// <summary>
		/// Left section
		/// </summary>
		protected void DrawBaseSexInfoLeft(Rect rect)
		{
			Listing_Standard listmain = new Listing_Standard();
			listmain.Begin(rect);
			float p;

			//Sex statistics
			GUI.Label(listmain.GetRect(FONTHEIGHT), " " + Keyed.RS_Statistics, fontStyleLeft);
			listmain.Gap(1f);
			float maxSatisfaction = history.GetBestSextype(out _);
			if (maxSatisfaction == 0f) maxSatisfaction = BASESAT;
			for (int i = 0; i < Sextype.Length; i++)
			{
				int sexindex = Sextype[i];
				float relativeSat = history.GetAVGSat(sexindex) / maxSatisfaction;
				p = history.GetAVGSat(sexindex) / BASESAT;
				string label = Keyed.RS_Sex_Info(Keyed.Sextype[sexindex], history.GetSexCount(sexindex).ToString());
				Rect tmpRect = listmain.GetRect(FONTHEIGHT);
				FillableBarLabeled(tmpRect, label, relativeSat, HistoryUtility.SextypeColor[sexindex], Texture2D.blackTexture, null, Keyed.RS_SAT_AVG(String.Format("{0:P2}", p)));
				if (Mouse.IsOver(tmpRect))
				{
					TooltipHandler.TipRegion(tmpRect, Keyed.RS_LastSex.CapitalizeFirst() + ": " + RJWUIUtility.GetSexDays(history.GetSextypeRecentTickAbs(Sextype[i]), true));
				}

				listmain.Gap(1f);
			}

			p = history.PartnerCount;
			FillableBarLabeled(listmain.GetRect(FONTHEIGHT), String.Format(Keyed.RS_Sex_Partners + ": {0} ({1})", p, pawn.records.GetValue(VariousDefOf.SexPartnerCount)), p / 50, HistoryUtility.Partners, Texture2D.blackTexture);
			listmain.Gap(1f);

			p = history.VirginsTaken;
			FillableBarLabeled(listmain.GetRect(FONTHEIGHT), String.Format(Keyed.RS_VirginsTaken + ": {0:0}", p), p / 100, HistoryUtility.Partners, Texture2D.blackTexture);
			listmain.Gap(1f);

			//Partner list
			Rect listLabelRect = listmain.GetRect(FONTHEIGHT);
			Rect sortbtnRect = new Rect(listLabelRect.xMax - 80f, listLabelRect.y, 80f, listLabelRect.height);
			GUI.Label(listLabelRect, " " + Keyed.RS_PartnerList, fontStyleLeft);
			if (Widgets.ButtonText(sortbtnRect, orderMode.Translate()))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				orderMode = orderMode.Next();
				SortPartnerList(orderMode);
			}

			listmain.Gap(1f);

			Rect scrollRect = listmain.GetRect(CARDHEIGHT + 1f);
			GUI.Box(scrollRect, "", buttonStyle);
			if (!partnerList.NullOrEmpty())
			{
				Rect listRect = new Rect(scrollRect.x, scrollRect.y, LISTPAWNSIZE * partnerList.Count, scrollRect.height - 30f);
				Widgets.ScrollHorizontal(scrollRect, ref scroll, listRect);
				Widgets.BeginScrollView(scrollRect, ref scroll, listRect);
				DrawPartnerList(listRect, partnerList);
				Widgets.EndScrollView();
			}

			listmain.End();
		}

		protected void DrawPartnerList(Rect rect, List<SexPartnerHistoryRecord> partnerList)
		{
			Rect pawnRect = new Rect(rect.x, rect.y, LISTPAWNSIZE, LISTPAWNSIZE);
			for (int i = 0; i < partnerList.Count; i++)
			{
				Rect labelRect = new Rect(pawnRect.x, pawnRect.yMax - FONTHEIGHT, pawnRect.width, FONTHEIGHT);

				DrawPawn(pawnRect, partnerList[i]);
				Widgets.DrawHighlightIfMouseover(pawnRect);
				GUI.Label(labelRect, partnerList[i].Label, fontStyleCenter);
				if (Widgets.ButtonInvisible(pawnRect))
				{
					selectedPawn = partnerList[i];
					SoundDefOf.Click.PlayOneShotOnCamera();
				}
				if (partnerList[i] == selectedPawn)
				{
					Widgets.DrawHighlightSelected(pawnRect);
				}

				pawnRect.x += LISTPAWNSIZE;
			}
		}

		protected void DrawPawn(Rect rect, SexPartnerHistoryRecord history)
		{
			if (history != null)
			{
				bool drawheart = false;
				Rect iconRect = new Rect(rect.x + (rect.width * 3 / 4), rect.y, rect.width / 4, rect.height / 4);
				Texture img = HistoryUtility.UnknownPawn;

				if (history.IamFirst)
				{
					GUI.color = HistoryUtility.HistoryColor;
					Widgets.DrawTextureFitted(rect, HistoryUtility.FirstOverlay, 1.0f);
					GUI.color = Color.white;
				}

				if (history.Partner != null)
				{
					img = PortraitsCache.Get(history.Partner, rect.size, Rot4.South, default, 1, true, true, false, false);
					drawheart = LovePartnerRelationUtility.LovePartnerRelationExists(pawn, history.Partner);
				}
				else if (history.Race?.uiIcon != null)
				{
					img = history.Race.uiIcon;
				}

				if (history.Incest)
				{
					Widgets.DrawTextureFitted(iconRect, HistoryUtility.Incest, 1.0f);
					iconRect.x -= iconRect.width;
				}
				Widgets.DrawTextureFitted(rect, img, 1.0f);
				if (drawheart)
				{
					Widgets.DrawTextureFitted(iconRect, HistoryUtility.Heart, 1.0f);
					iconRect.x -= iconRect.width;
				}
			}
		}

		public static void FillableBarLabeled(Rect rect, string label, float fillPercent, Texture2D filltexture, Texture2D bgtexture, string tooltip = null, string rightlabel = "", Texture2D border = null)
		{
			Widgets.FillableBar(rect, Math.Min(fillPercent, 1.0f), filltexture, bgtexture, true);
			GUI.Label(rect, "  " + label.CapitalizeFirst(), fontStyleLeft);
			GUI.Label(rect, rightlabel.CapitalizeFirst() + "  ", fontStyleRight);
			Widgets.DrawHighlightIfMouseover(rect);
			if (tooltip != null) TooltipHandler.TipRegion(rect, tooltip);
			if (border != null)
			{
				rect.DrawBorder(border, 2f);
			}
		}
	}
}