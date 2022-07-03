﻿using RimWorld;
using rjw;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Verse;

namespace RJWSexperience.Ideology
{
	public class PartnerFilter
	{
		[SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "Field value loaded from XML")]
		public bool? isAnimal;
		[SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "Field value loaded from XML")]
		public bool? isVeneratedAnimal;
		[SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "Field value loaded from XML")]
		public bool? isSlave;
		[SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "Field value loaded from XML")]
		public bool? isAlien;
		[SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "Field value loaded from XML")]
		public List<PawnRelationDef> hasOneOfRelations;
		[SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "Field value loaded from XML")]
		public List<PawnRelationDef> hasNoneOfRelations;

		public bool Applies(Pawn pawn, Pawn partner)
		{
			if (isAnimal != null && isAnimal != partner.IsAnimal())
				return false;

			if (isVeneratedAnimal != null && isVeneratedAnimal != pawn.Ideo.IsVeneratedAnimal(partner))
				return false;

			if (isSlave != null && isSlave != partner.IsSlave)
				return false;

			//if (isAlien != null && isAlien != partner)
			//	return false;

			if (!hasOneOfRelations.NullOrEmpty())
			{
				if (pawn.relations == null)
					return false;

				bool found = false;
				foreach (PawnRelationDef relationDef in hasOneOfRelations)
				{
					if (pawn.relations?.DirectRelationExists(relationDef, partner) == true)
					{
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}

			if (!hasNoneOfRelations.NullOrEmpty() && pawn.relations != null)
			{
				foreach (PawnRelationDef relationDef in hasNoneOfRelations)
				{
					if (pawn.relations.DirectRelationExists(relationDef, partner))
						return false;
				}
			}

			return true;
		}
	}
}
