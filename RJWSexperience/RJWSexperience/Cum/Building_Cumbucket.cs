﻿using RimWorld;
using Verse;

namespace RJWSexperience // Used in Menstruation with this namespace
{
	public class Building_CumBucket : Building_Storage
	{
		protected float storedcum = 0f;
		protected float totalgathered = 0f;

		public override void ExposeData()
		{
			Scribe_Values.Look(ref storedcum, "storedcum", storedcum, true);
			Scribe_Values.Look(ref totalgathered, "totalgathered", totalgathered, true);
			base.ExposeData();
		}

		public override string GetInspectString()
		{
			return Keyed.RSTotalGatheredCum + string.Format("{0:0.##}ml", totalgathered);
		}

		public void AddCum(float amount)
		{
			AddCum(amount, VariousDefOf.GatheredCum);
		}

		public void AddCum(float amount, ThingDef cumDef)
		{
			Thing cum = ThingMaker.MakeThing(cumDef);
			AddCum(amount, cum);
		}

		public void AddCum(float amount, Thing cum)
		{
			storedcum += amount;
			totalgathered += amount;
			int num = (int)storedcum;

			cum.stackCount = num;
			if (cum.stackCount > 0 && !GenPlace.TryPlaceThing(cum, PositionHeld, Map, ThingPlaceMode.Direct, out Thing res))
			{
				FilthMaker.TryMakeFilth(PositionHeld, Map, VariousDefOf.FilthCum, num);
			}
			storedcum -= num;
		}
	}
}
