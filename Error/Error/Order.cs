using System;
using System.Collections.Generic;

namespace Error
{
    public class Order
    {
        public List<OrderLine> Lines;
        public string Customer;
        public DateTime RequestedShippingDate;
    }
    public class OrderLine
    {
        public string ProductCode; // asfasg4s53sdgsa
        public int Amount; // 500 = toimitettava m‰‰r‰
        //public DataBaseEntry OptimalDBEntry = null; // t‰‰lt‰ ker‰t‰‰n
    }
}
