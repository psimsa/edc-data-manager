using System;
using System.Collections.Generic;

namespace EdcMgmt.Models
{
    /// <summary>
    /// Represents a configuration for allocation, grouping allocation details for a specific period.
    /// </summary>
    public class AllocationConfiguration
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public ICollection<AllocationDetail> AllocationDetails { get; set; } = new List<AllocationDetail>();
    }
}