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
            if (!Contains(p)) return false;
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
            Point bestPoint = new Point(int.MaxValue, int.MaxValue);
            float minDist = float.MaxValue;
            while (offset < 200)// TODO parempaa
            {
                for (int x = p.X - offset; x < p.X + offset; x++)
                {
                    for (int y = p.Y - offset; y < p.Y + offset; y++)
                    {
                        if (IsTraversable(new Point(x, y)))
                        {
                            int dx = x - p.X, dy = y - p.Y;
                            float dist = dx * dx + dy * dy;
                            if (dist < minDist)
                            {
                                minDist = dist;
                                bestPoint = new Point(x, y);
                            }
                        }
                    }
                }
                if (minDist != float.MaxValue) break;
                offset += 2;
            }
            return bestPoint;
        }
    }
    public struct MapNode
    {
        public bool IsTraversable;
    }
}
