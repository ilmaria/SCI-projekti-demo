using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Error
{
    public class WareHouse
    {
        List<Shelf> _shelves;//näistä A* kartta
        Dictionary<string, StoredProduct> _stored_products;// tuotekoodi, varastossa oleva tuote
        // public hyllysijainti(tuotekoodi)
        //  koodi/hyllysijanti/fyysinen sijainti --> koodi/hylly/fyysinen sij/inventaariostatus
        // varastopaikka 1005 tms
        // käytävä kirjaimella
        // tuotekoodi string

        //varastopaikkaa tarkempi, lavakohtainen hyllykoodi?

        //public bool IsTraversable(int xCoordinate, int yCoordinate)
        //{
        //    return false;
        //}

        // kun data on tuotu
        public void Init()
        {
            for (int iShelf = 0; iShelf < _shelves.Count; iShelf++)
            {
                _shelves[iShelf].BoundingBox = _shelves[iShelf].LavaPaikat[0].BoundingBox;
                foreach (var lava in _shelves[iShelf].LavaPaikat)
                {
                    _shelves[iShelf].BoundingBox = BoundingBox.CreateMerged(_shelves[iShelf].BoundingBox, lava.BoundingBox);
                }
            }
        }

        // x, y fyysinen sijainti
        public bool IsTraversable(float x, float y)
        {
            bool result = true;
            foreach (Shelf shelf in _shelves)
            {
                float xmin = shelf.BoundingBox.Min.X;
                float ymin = shelf.BoundingBox.Min.Y;
                float xmax = shelf.BoundingBox.Max.X;
                float ymax = shelf.BoundingBox.Max.Y;

                result |= (x >= xmin && x <= xmax && y<= ymax && y >= ymin);
            }
            return result;
        }
        public int GetProductInventoryStatus(string productCode)
        {
            return _stored_products[productCode].StoredAmount;
        }
        public void SetProductInventoryStatus(string productCode, int newAmount)
        {
            _stored_products[productCode].StoredAmount = newAmount;
        }

        //hylly
        // 3d boundingbox
        // lavapaikat, 3d boundingbox, tuotteet, varastopaikka johon kuuluu, tuotteet
        // 

        //varastopaikka kuuluu hyllyyn

        public class StoredProduct
        {
            public string Code;
            public int StoredAmount;
            public int PacketSize;
            public LavaPaikka LavaPaikka;
        }
        public class LavaPaikka
        {
            public string Code; //1005
            public BoundingBox BoundingBox;
            public List<string> Products;
        }
        public class Shelf
        {
            public char Key; // A,B,...
            public List<LavaPaikka> LavaPaikat;
            public BoundingBox BoundingBox;//union lavapaikkojen boundingboxeista            
        }
        //input vaikka hyllypaikkojen / pohjapiirrustuksen osalta:
        // A : 1001 1002 1003 1004 1005 1006 1007 1008 1009 1010 ehkä: xmin ymin zmin xmax ymax zmax
        // B : ....

        // tuotteet
        // asdgdg268sfs6f : 1005, 256 kpl, "terassiruuvi 5x60 sinkitty"

        // fyysinen sijainti miten? riippunee minkälaista dataa saadaan

    }
}
