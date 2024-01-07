using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTCG.Data.Models
{
    public class TradeOfferJsonModel
    {
        public string? Id { get; set; }
        public string? CardToTrade { get; set; }
        public string? Type { get; set; }
        public int? MinimumDamage { get; set; }
    }
}