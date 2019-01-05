using System.Collections.Generic;

namespace KrogerScrape.Client
{
    public class ItemDetail
    {
        public string Upc { get; set; }
        public string CustomerFacingSize { get; set; }
        public string Description { get; set; }
        public string FamilyTreeCommodityCode { get; set; }
        public string FamilyTreeDepartmentCode { get; set; }
        public string FamilyTreeSubCommodityCode { get; set; }
        public string MainImage { get; set; }
        public bool? SoldInStore { get; set; }
        public decimal? AverageWeight { get; set; }
        public List<ItemCategory> Categories { get; set; }
    }
}
