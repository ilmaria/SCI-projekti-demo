using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Error
{
    // A* pathfinding algorithm
    public class AStar
    {
        #region fields and properties
        // G = cost from start to this position
        // H = approximate cost from this position to goal
        // F = G + H
        Map _map;
        float[,] _G; // metres. TODO: --> seconds, using vehicle/worker properties
        byte[,] _whichList;
        Point[,] _parentPositions;
        BinaryHeap<Node> _openList;
        byte LIST_NONE, LIST_OPEN, LIST_CLOSED;

        static readonly Point[] _neighbors = 
        { 
            new Point(-1, -1), new Point(-1, 0), new Point(-1, 1),
            new Point(0, -1),                    new Point(0, 1),
            new Point(1, -1),  new Point(1, 0),  new Point(1, 1)
        };
        #endregion

        public AStar(Map map)
        {
            _map = map;
            _G = new float[_map.SizeX, _map.SizeY];
            _whichList = new byte[_map.SizeX, _map.SizeY];
            _parentPositions = new Point[_map.SizeX, _map.SizeY];
            _openList = new BinaryHeap<Node>(_map.SizeX * _map.SizeY);
        }        
        public List<Point> FindPath(Point startPosition, Point goalPosition, out float time)
        {
            time = 0f;
            if (startPosition == goalPosition)
                return new List<Point>(0);
            if (!(_map.Contains(startPosition) && _map.Contains(goalPosition)))
                return new List<Point>(0);

            LIST_NONE += 4;
            if (LIST_NONE >= 250)
            {
                LIST_NONE = 0;
                for (int x = 0; x < _whichList.GetLength(0); x++)
                {
                    for (int y = 0; y < _whichList.GetLength(1); y++)
                        _whichList[x, y] = LIST_NONE;
                }
            }
            LIST_OPEN = (byte)(LIST_NONE + 1);
            LIST_CLOSED = (byte)(LIST_NONE + 2);

            //add startpoint
            Node startNode = new Node() { Position = startPosition, F = 0f };
            _openList.Add(startNode);
            _whichList[startPosition.X, startPosition.Y] = LIST_OPEN;

            // actual pathfinding
            while (true)
            {
                if (_openList.Count == 0) return new List<Point>(0); // did not find path

                // get node with smallest F-cost
                Node parentNode = _openList.Remove();
                // add to closed list
                _whichList[parentNode.Position.X, parentNode.Position.Y] = LIST_CLOSED;
                ProcessNeighbors(parentNode, goalPosition);

                // path is found
                if (_whichList[goalPosition.X, goalPosition.Y] == LIST_OPEN) break;
            }
            // time it takes to traverse the path
            time = _G[goalPosition.X, goalPosition.Y];

            // construct path by traversing from goal to start, and reverse order in the end
            Point currentPosition = goalPosition;
            List<Point> path = new List<Point>(16);
            while (currentPosition != startPosition)
            {
                path.Add(currentPosition);
                currentPosition = _parentPositions[currentPosition.X, currentPosition.Y];
            }
            path.TrimExcess();
            path.Reverse();
            return path;
        }
        void ProcessNeighbors(Node parentNode, Point goalPosition)
        {
            for (int i = 0; i < 8; i++)
            {
                Point position = parentNode.Position;// +_neighbors[i];
                position.X += _neighbors[i].X;
                position.Y += _neighbors[i].Y;
                if (!_map.Contains(position)) continue;
                if (!_map.IsTraversable(position)) continue;
                if (_whichList[position.X, position.Y] == LIST_CLOSED) continue;
                if (_whichList[position.X, position.Y] == LIST_OPEN)
                {//check if route to this node is shorter via parentNode and not it's current _parentPosition
                    float tmpG = _G[parentNode.Position.X, parentNode.Position.Y] + CalcG(_neighbors[i]);
                    if (tmpG < _G[position.X, position.Y])
                    {//change parent node
                        _G[position.X, position.Y] = tmpG;
                        _parentPositions[position.X, position.Y] = parentNode.Position;

                        // find node in openList with position
                        // update its F to be tmpG + calcH (to match new parent)
                        // update openList heap
                        for (int j = 0; j < _openList.Count; j++)
                        {
                            if (_openList[j].Position == position)
                            {
                                Node n = _openList[j];
                                n.F = tmpG + CalcH(position, goalPosition);
                                _openList[j] = n;
                                _openList.UpHeap(j); // F was changed to smaller, so the node needs to go up in the heap
                                break;
                            }
                        }
                    }
                    continue;
                }

                // add this neighbor to open list
                float g = _G[parentNode.Position.X, parentNode.Position.Y] + CalcG(_neighbors[i]);
                float h = CalcH(position, goalPosition);
                _openList.Add(new Node { Position = position, F = g + h });
                _whichList[position.X, position.Y] = LIST_OPEN;

                // update stuff
                _G[position.X, position.Y] = g;
                _parentPositions[position.X, position.Y] = parentNode.Position;
            }
        }
        static float CalcH(Point current, Point goal)
        {
            int dx = System.Math.Abs(goal.X - current.X);
            int dy = System.Math.Abs(goal.Y - current.Y);
            // orthogonal distance + diagonal distance
            return System.Math.Abs(dx - dy) + 1.41421356f * System.Math.Min(dx, dy);
        }
        static float CalcG(Point ds)
        {
            return (float)System.Math.Sqrt(ds.X * ds.X + ds.Y * ds.Y);
        }
    }

    public struct Node : IComparable<Node>
    {
        public Point Position;
        public float F;

        // implement interface
        int IComparable<Node>.CompareTo(Node other)
        {
            return F.CompareTo(other.F);
        }
    }
}


//Summary of the A* Method

//1) Add the starting square (or node) to the open list.

//2) Repeat the following:

//a) Look for the lowest F cost square on the open list. We refer to this as the current square.

//b) Switch it to the closed list.

//c) For each of the 8 squares adjacent to this current square …

//    If it is not walkable or if it is on the closed list, ignore it. Otherwise do the following.           

//    If it isn’t on the open list, add it to the open list. Make the current square the parent of this square. Record the F, G, and H costs of the square. 

//    If it is on the open list already, check to see if this path to that square is better, using G cost as the measure. A lower G cost means that this is a better path. 
//If so, change the parent of the square to the current square, and recalculate the G and F scores of the square. If you are keeping your open list 
//sorted by F score, you may need to resort the list to account for the change.

//d) Stop when you:

//    Add the target square to the closed list, in which case the path has been found (see note below), or
//    Fail to find the target square, and the open list is empty. In this case, there is no path.   

//3) Save the path. Working backwards from the target square, go from each square to its parent square until you reach the starting square. That is your path. 

//Note: In earlier versions of this article, it was suggested that you can stop when the target square (or node) has been added to the open list, rather than the closed list.  
//Doing this will be faster and it will almost always give you the shortest path, but not always.  Situations where doing this could make a difference are when the movement 
//cost to move from the second to the last node to the last (target) node can vary significantly -- as in the case of a river crossing between two nodes, for example.
