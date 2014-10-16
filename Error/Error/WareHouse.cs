using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Error
{
    public class WareHouse
    {
        // x, y fyysinen sijainti
        //public bool IsTraversable(float x, float y)
        //{
        //    bool result = true;
        //    foreach (Shelf shelf in _shelves)
        //    {
        //        float xmin = shelf.BoundingBox.Min.X;
        //        float ymin = shelf.BoundingBox.Min.Y;
        //        float xmax = shelf.BoundingBox.Max.X;
        //        float ymax = shelf.BoundingBox.Max.Y;

        //        result |= (x >= xmin && x <= xmax && y<= ymax && y >= ymin);
        //    }
        //    return result;
        //}

        //input vaikka hyllypaikkojen / pohjapiirrustuksen osalta:
        // A : 1001 1002 1003 1004 1005 1006 1007 1008 1009 1010 ehkä: xmin ymin zmin xmax ymax zmax
        // B : ....

        // tuotteet
        // asdgdg268sfs6f : 1005, 256 kpl, "terassiruuvi 5x60 sinkitty"

        // fyysinen sijainti miten? riippunee minkälaista dataa saadaan

        DataBase productDataBase;
        Map AStarMap;

        public class DataBase
        {
            readonly List<DataBaseEntry> _items;

            public DataBase(int count)
            {
                _items = new List<DataBaseEntry>(count);
            }

            public List<DataBaseEntry> GetByProductCode(string code)
            {
                // LINQ
                return (from item in _items where item.ProductCode == code select item).ToList();
            }
            public void Add(DataBaseEntry entry)
            {
                _items.Add(entry);
            }
            public void Remove(DataBaseEntry entry)
            {
                // lineaarinen haku, on hidas
                _items.Remove(entry);
            }

            //public Map CreateAStarMap()
            //{

            //}
        }
        // saapuu lavallinen tavaraa -> new DataBaseEntry()
        public class DataBaseEntry
        {
            //byte[] _blob ja get/set accessors...
            public string ProductCode;// asdfsadfsf26565ddsa
            public string PalletCode;// terästarvike: 1005, Wurth: E21B3
            public string ShelfCode;// terästarvike: 1005/6? wurth E21B3
            public int Amount;// num_packets = amount/PacketSize
            public int PacketSize;
            public string ProductionDate;
            public string InsertionDate;
            public string ModifiedDate;//tai time+date "10.5.2014 13.50"
            // public Location: warehouse, production
            public BoundingBox BoundingBox;//fyysinen sijainti, xmin ymin zmin xmax ymax zmax. z korkeus, 1.kerroksen lattia z=0
            public string ProductDescription;//"ruuvi sinkitty 5x70"
            public string ExtraNotes; // jaakko kaato nämä lattialle, ovat vähän huonoja nyt. ei parhaille asiakkailla
            // jotain käytön aktiivisuuden laskentaa
        }


        //asiakaskohtaiset ProductCode,PalletCode,ShelfCode esitykset ja muutokset
        // DBEntry.GetLocationDescription: terästarvike: shelf + product+desc, wurth pallet+product+desc
    }
}
