using System;

namespace NGEOS.Desktop.Core.Entities
{
    public class Cause
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Education";
        public decimal TargetAmount { get; set; }
        public decimal RaisedAmount { get; set; }
        public int BeneficiariesCount { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public double ProgressPercentage => TargetAmount > 0 
            ? Math.Min(100.0, (double)(RaisedAmount / TargetAmount) * 100.0) 
            : 0;
    }
}
