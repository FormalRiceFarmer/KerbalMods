﻿// 
//     Kerbal Engineer Redux
// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

#region Using Directives

using KerbalEngineer.Extensions;
using KerbalEngineer.Flight.Sections;
using KerbalEngineer.Helpers;

#endregion

namespace KerbalEngineer.Flight.Readouts.Surface
{
    public class ImpactLatitude : ReadoutModule
    {
        #region Constructors

        public ImpactLatitude()
        {
            this.Name = "Impact Latitude";
            this.Category = ReadoutCategory.GetCategory("Surface");
            this.HelpString = "Latitude of the impact position.";
            this.IsDefault = false;
        }

        #endregion

        #region Methods: public

        public override void Draw(SectionModule section)
        {
            if (ImpactProcessor.ShowDetails)
            {
                this.DrawLine(Units.ToAngleDMS(ImpactProcessor.Latitude) + (ImpactProcessor.Latitude < 0 ? " S" : " N"), section.IsHud);
            }
        }

        public override void Reset()
        {
            FlightEngineerCore.Instance.AddUpdatable(ImpactProcessor.Instance);
        }

        public override void Update()
        {
            ImpactProcessor.RequestUpdate();
        }

        #endregion
    }
}