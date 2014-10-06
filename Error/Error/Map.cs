using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Error
{
    public class Map
    {
        public int SizeX, SizeY;
        MapNode[,] _mapData;

        public MapNode this[int x, int y]
        {
            get { return _mapData[x, y]; }
            set { _mapData[x, y] = value; }
        }

        public Map(int sizeX, int sizeY)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            _mapData = new MapNode[SizeX, SizeY];
        }
        public bool Contains(Point p)
        {
            return p.X >= 0 && p.Y >= 0 && p.X < SizeX && p.Y < SizeY;
        }
        public bool IsTraversable(Point p)
        {
            return _mapData[p.X, p.Y].IsTraversable;
        }
    }
    public struct MapNode
    {
        public bool IsTraversable;
        public string Data;

        //MapNode sisältää ainakin: 
        //  -tiedon voiko kulkea
        //  -hyllykoodin tms
        //  -lista tuotteista (+inventaariostatus?)
    }
}
