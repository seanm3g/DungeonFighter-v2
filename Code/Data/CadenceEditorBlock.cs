using System.Collections.Generic;

namespace RPGGame.Data
{
    /// <summary>Single mechanic row within a cadence block in the actions settings editor.</summary>
    public class CadenceMechanicRow
    {
        public string MechanicId { get; set; } = "";
        public double Quantity { get; set; }
        /// <summary>STR, AGI, TECH, or INT when MechanicId is hero_stat_bonus / enemy_stat_bonus.</summary>
        public string StatSubType { get; set; } = "";
    }

    /// <summary>Cadence + duration + mechanics for the actions settings editor.</summary>
    public class CadenceEditorBlock
    {
        public string Cadence { get; set; } = "Turn";
        public int Duration { get; set; } = 1;
        public List<CadenceMechanicRow> Mechanics { get; set; } = new List<CadenceMechanicRow>();
    }
}
