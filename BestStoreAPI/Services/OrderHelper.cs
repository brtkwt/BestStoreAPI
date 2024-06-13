namespace BestStoreAPI.Services
{
    public class OrderHelper
    {
        public static decimal ShippingFee { get; } = 5;

        public static Dictionary<string, string> PaymentMethods { get; } = new()
        {
            { "Cash","Cash on Delivery" },
            { "Paypal","Paypal" },
            { "Credit Card","Credit Card" }
        };

        public static List<string> PaymentStatuses { get; } = new()
        {
            "Pending", "Accepted", "Canceled"
        };

        public static List<string> OrderStatuses { get; } = new()
        {
            "Created", "Accepted", "Canceled", "Shipped", "Delivered", "Returned"
        };

        /*
         *  Recives a string of product identifires, separated by '-'
         *  Example: 9-9-7-9-6
         * 
         *  Returns a list of pairs (dictionary):
         *      -the pair name is the product ID
         *      -the pair value is the quantity
         *  Example:
         * {
         *      9:3,
         *      7:1,
         *      6:1
         * }
         * 
         */
        public static Dictionary<int, int> GetProductDictionary(string productIdentifires)
        {
            Dictionary<int, int> productDictionary = new Dictionary<int, int>();

            if(productIdentifires.Length > 0)
            {
                string[] productIdArray = productIdentifires.Split('-');
                foreach (var productId in productIdArray)
                {
                    try
                    {
                        int id = int.Parse(productId);

                        if(productDictionary.ContainsKey(id))
                        {
                            productDictionary[id] += 1;
                        }
                        else
                        {
                            productDictionary.Add(id, 1);
                        }
                    }
                    catch(Exception) { }
                    
                }
            }



            return productDictionary;
        }
    }
}
