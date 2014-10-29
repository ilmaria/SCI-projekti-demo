using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Error
{
    public class DataBase
    {
        public List<DataBaseEntry> Items; // don't access with index, they change
        public List<BoundingBox> Obstacles;
        // voxels. loc, size, type jne

        public DataBase(int count)
        {
            Items = new List<DataBaseEntry>(count);
        }
        public void Add(DataBaseEntry entry)
        {
            Items.Add(entry);
        }
        public void Remove(DataBaseEntry entry)
        {
            // lineaarinen haku, on hidas
            Items.Remove(entry);
        }

        public List<DataBaseEntry> GetByProductCode(string code)
        {
            return (from item in Items where item.ProductCode == code select item).ToList();
        }
        //public DataBaseEntry FindNearest(List<DataBaseEntry> items, Vector3 location)
        //{
        //    // todo asiat eri kerroksissa, painotus korkeudella?
        //    // ei ole
        //    items.OrderBy(item => Vector3.Distance(location, item.BoundingBox.Center()));
        //    return items[0];
        //}
        public void Collect(DataBaseEntry item, int amount)
        {
            item.Amount -= amount;
            item.CollectionTimes.Add(DateTime.Now);
            item.ModifiedDate = DateTime.Now;

            if (item.Amount <= 0)
            {
                // TODO
            }
        }
    }

    // saapuu lavallinen tavaraa -> new DataBaseEntry()
    public class DataBaseEntry
    {
        public string ProductCode;// asdfsadfsf26565ddsa
        public string ProductDescription;//"ruuvi sinkitty 5x70"
        public string PalletCode;// 1005, E21B3 == lavapaikka
        public string ShelfCode;// 1005/6? E21B3
        public int Amount;// num_packets = amount/PacketSize
        public int PackageSize;
        public DateTime ProductionDate; // when product was manufactured
        public DateTime InsertionDate; // milloin saapunut varastolle / laitettu hyllyyn. Vanhimmat ensin asiakkaalle?
        public DateTime ModifiedDate; // when anything in this DataBaseEntry has (physically) changed
        // when the product has been picked from warehouse for delivery to customers
        public List<DateTime> CollectionTimes = new List<DateTime>(0);
        // public Location: warehouse, production
        //näistä saa nopeasti ja helposti tehtyä vaikka 3d kuvan...
        public BoundingBox BoundingBox;//fyysinen sijainti, xmin ymin zmin xmax ymax zmax. z korkeus, 1.kerroksen lattia z=0
        public string ExtraNotes;
    }
}
