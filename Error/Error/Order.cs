using System;
using System.Collections.Generic;

namespace Error
{
    public class Order
    {
        public List<OrderLine> Lines;
        public string Customer;
        public DateTime RequestedShippingDate;
        public OrderState State = OrderState.Default;
    }
    public class OrderLine
    {
        public string ProductCode; // asfasg4s53sdgsa
        public int Amount; // 500 = toimitettava m‰‰r‰
        public LineState State = LineState.Default;
    }
    [Flags]
    public enum OrderState
    {
        Default,
        Received,
        CollectingStarted,
        ProductsMissing,
        Collected,
        Packed,
        Shipped,
        Canceled
    }
    [Flags]
    public enum LineState
    {
        Default,
        ProductMissing,
        Collected
    }
}
