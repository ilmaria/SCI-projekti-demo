using System;
using System.Collections.Generic;

namespace Error
{
    // todo order ja line avaimet, eik‰ pointtereita
    public class Order
    {
        // information that needs to be imported
        public List<OrderLine> Lines;
        public string Customer;
        public DateTime RequestedShippingDate;

        //information that is created and managed by the app
        public uint State = STATE.DEFAULT;
        public float Priority; // nimi vaihtunee, mutta arvo on "monenko sekunnin p‰‰st‰ tilauksen t‰ytyy olla valmis"
    }
    public class OrderLine
    {
        // information that needs to be imported
        public string ProductCode;
        public int Amount;

        //information that is created and managed by the app
        public uint State = STATE.DEFAULT;//ehk‰ pois
    }
}
