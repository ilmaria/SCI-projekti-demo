using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Error
{
    public class DataBase
    {
        public  List<DataBaseEntry> Items; // don't access with index, they change

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
            // LINQ
            return (from item in Items where item.ProductCode == code select item).ToList();
        }
    }

    // saapuu lavallinen tavaraa -> new DataBaseEntry()
    public class DataBaseEntry
    {
        //byte[] _blob ja get/set accessors... TAI .xml
        public string ProductCode;// asdfsadfsf26565ddsa
        public string ProductDescription;//"ruuvi sinkitty 5x70"
        public string PalletCode;// terästarvike: 1005, Wurth: E21B3 == lavapaikka
        public string ShelfCode;// terästarvike: 1005/6? wurth E21B3
        public int Amount;// num_packets = amount/PacketSize
        public int PackageSize;
        public string ProductionDate;
        public string InsertionDate;// milloin saapunut varastolle / laitettu hyllyyn. Vanhimmat ensin asiakkaalle?
        public string ModifiedDate;//"10.5.2014 13.50" ehkä turha
        // public Location: warehouse, production
        //näistä saa nopeasti ja helposti tehtyä vaikka 3d kuvan...
        public BoundingBox BoundingBox;//fyysinen sijainti, xmin ymin zmin xmax ymax zmax. z korkeus, 1.kerroksen lattia z=0
        public string ExtraNotes; // jaakko kaato nämä lattialle, ovat vähän huonoja nyt. ei parhaille asiakkailla
        // jotain käytön aktiivisuuden laskentaa
    }
}
