namespace WebApi.DTOs
{
    public class ItemDto
    {
        public string ItemCode { get; set; }
        //public string ItemName { get; set; }
        //public string BarCode { get; set; }
        //public string ManageSerialNumbers { get; set; }
        //public string ManageBatchNumbers { get; set; }

        //public List<ItemPriceDto> ItemPrices { get; set; }
        //public List<ItemWarehouseInfoCollectionDto> ItemWarehouseInfoCollection { get; set; }
    }

    public partial class ItemPriceDto
    {
        public long? PriceList { get; set; }
        public object Price { get; set; }
        public object Currency { get; set; }
    }

    public partial class ItemWarehouseInfoCollectionDto
    {
        public string WarehouseCode { get; set; }
        public long? InStock { get; set; }
    }
}
