using System;

namespace EdcMgmt.Models
{
    /// <summary>
    /// Represents an energy transfer between two stations for a specific time period.
    /// </summary>
    public class EnergyTransfer
    {
        public int Id { get; set; }
        public int FromStationId { get; set; }
        public EnergyStation? FromStation { get; set; }
        public int ToStationId { get; set; }
        public EnergyStation? ToStation { get; set; }
        public int TimePeriodId { get; set; }
        public TimePeriod? TimePeriod { get; set; }
        public decimal Value { get; set; }
        public string? Unit { get; set; }
    }
}