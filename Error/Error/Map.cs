using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Error
{
    public class Map // physical coordinates are Vector3, internal (A*) coordinates are Point
    {
        public int SizeX, SizeY;
        BoundingBox _boundingBox;
        MapNode[,] _mapData;
        float _resolutionInMetres = 1f;

        public MapNode this[int x, int y]
        {
            get { return _mapData[x, y]; }
            set { _mapData[x, y] = value; }
        }

        public Map(BoundingBox b, float resolutionInMetres)
        {
            _resolutionInMetres = resolutionInMetres;
            SizeX = (int)((b.Max.X - b.Min.X) / resolutionInMetres);
            SizeY = (int)((b.Max.Y - b.Min.Y) / resolutionInMetres);
            _boundingBox = b;
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
        public Point PhysicalToInternalCoordinates(Vector3 position)
        {
            return new Point
            {
                X = (int)((position.X - _boundingBox.Min.X) / _resolutionInMetres),
                Y = (int)((position.Y - _boundingBox.Min.Y) / _resolutionInMetres)
            };
        }
        public Vector3 InteralToPhysicalCoordinates(Point p)
        {
            return new Vector3(p.X * _resolutionInMetres + _boundingBox.Min.X,
                p.Y * _resolutionInMetres + _boundingBox.Min.Y, 0f);
        }
        // return the A*-coordinate from which the product in b can be collected
        public Point FindCollectingPoint(BoundingBox b)
        {
            Point p = PhysicalToInternalCoordinates(b.Center());
            int offset = 2;
            List<Point> possiblePoints = new List<Point>(25);
            while (true)
            {
                for (int x = p.X - offset; x < p.X + offset; x++)
                {
                    for (int y = p.Y - offset; y < p.Y + offset; x++)
                    {
                        if (IsTraversable(new Point(x, y)))
                        {
                            possiblePoints.Add(new Point(x, y));
                        }
                    }
                }
                if (possiblePoints.Count == 0)
                {
                    if (offset * _resolutionInMetres > 100f) return Point.Zero;// error
                    offset++;
                    continue;
                }
                //order by distance
                possiblePoints.OrderBy(pt => (pt.X - p.X) * (pt.X - p.X) + (pt.Y - p.Y) * (pt.Y - p.Y));
                return possiblePoints[0];
            }
        }
    }
    public struct MapNode
    {
        public bool IsTraversable;
    }
}
