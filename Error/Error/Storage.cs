using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Error
{
    public class Storage
    {
        public List<Product> Products; // don't access with index, they change
        public List<BoundingBox> Obstacles; // octree/bsp tree obstacles tms
        public BoundingBox BoundingBox;
        public Map Map;
        public AStar PathFinder;

        public Storage(int count)
        {
            Products = new List<Product>(count);
            Obstacles = new List<BoundingBox>(0);
            BoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
        }
        // call this after adding obstacles and products
        public void CreateMap(float resolution_in_metres)
        {
            Map = new Map(BoundingBox, resolution_in_metres);
            for (int x = 0; x < Map.SizeX; x++)
            {
                for (int y = 0; y < Map.SizeY; y++)
                {
                    MapNode mapNode = new MapNode();
                    mapNode.IsTraversable = IsTraversable(Map.InteralToPhysicalCoordinates(new Point(x,y)));
                    Map[x, y] = mapNode;
                }
            }
            PathFinder = new AStar(Map);
        }
        public bool IsTraversable(Vector3 position)
        {
            BoundingBox b = new BoundingBox(position - new Vector3(0.5f, 0.5f, 0f), position + new Vector3(0.5f, 0.5f, 2f));
            if (!BoundingBox.Intersects(b)) return false;

            foreach (var obstacle in Obstacles)
            {
                if (obstacle.Intersects(b)) return false;
            }
            foreach (var product in Products)
            {
                if (product.BoundingBox.Intersects(b)) return false;
            }
            return true;
        }
        public void Add(BoundingBox obstacle)
        {
            BoundingBox = BoundingBox.CreateMerged(BoundingBox, obstacle);
            Obstacles.Add(obstacle);
        }
        public void Add(Product product)
        {
            BoundingBox = BoundingBox.CreateMerged(BoundingBox, product.BoundingBox);
            Products.Add(product);
        }
        public void Remove(Product entry)
        {
            // lineaarinen haku, on hidas
            Products.Remove(entry);
        }

        public List<Product> GetByProductCode(string code)
        {
            return (from item in Products where item.ProductCode == code select item).ToList();
        }
        public void Collect(Product item, int amount)
        {
            item.Amount -= amount;
            item.CollectionTimes.Add(DateTime.Now);
            item.ModifiedDate = DateTime.Now;

            if (item.Amount <= 0)
            {
                // TODO
            }
        }
        public Product FindNearestToCollect(string productCode, int amount, Point location)
        {
            var items = GetByProductCode(productCode);
            items = (from item in items where item.Amount >= amount select item).ToList();

            // TODO
            //if(items.Count == 0) tuotetta ei varastossa

            // find nearest product
            int minIndex = 0;
            float minTime = float.MaxValue;
            for (int i = 0; i < items.Count; i++)
            {
                Point collectionPoint = Map.FindCollectingPoint(items[i].BoundingBox);
                float time;
                PathFinder.FindPath(location, collectionPoint, out time);
                if (time < minTime)
                {
                    minTime = time;
                    minIndex = i;
                }
            }
            return items[minIndex];
        }
        public List<Product> SearchText(string txt)
        {
            var products = from p in Products where p.ProductCode == txt select p;
            products = products.Union(from p in Products where p.ProductDescription == txt select p);
            products = products.Union(from p in Products where p.PalletCode == txt select p);
            products = products.Union(from p in Products where p.ShelfCode == txt select p);
            // remove duplicates
            return products.Distinct().ToList();
        }
        public List<Product> SearchPartialText(string txt)
        {
            var products = from p in Products where p.ProductCode.Contains(txt) select p;
            products = products.Union(from p in Products where p.ProductDescription.Contains(txt) select p);
            products = products.Union(from p in Products where p.PalletCode.Contains(txt) select p);
            products = products.Union(from p in Products where p.ShelfCode.Contains(txt) select p);
            // remove duplicates
            return products.Distinct().ToList();
        }
    }

    // saapuu lavallinen tavaraa -> new DataBaseEntry()
    public class Product // product ei viel‰k‰‰n hyv‰ nimi
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
        //n‰ist‰ saa nopeasti ja helposti tehty‰ vaikka 3d kuvan...
        public BoundingBox BoundingBox;//fyysinen sijainti, xmin ymin zmin xmax ymax zmax. z korkeus, 1.kerroksen lattia z=0
        public string ExtraNotes;
    }
}
