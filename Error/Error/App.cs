using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

//ilmari : ui, datan tuonti
//henri: varaston tietorakenne, järjestyksen optimointi

/*
    Ilmari ks ReadWareHouseData(), ReadOrders(), class Product, class Order.cs
    sovellukseen tarvittaneen mahdollisuus hakea tuotteita tuotekoodilla. Data saadaan Storage.GetByProductCode(), UI puuttuu
*/

/* TODO
 esteet, jotka eivät tuotteita
 lista mahdollisista hyllypaikoista varastossa tuotteiden sijaintien optimointia varten
 esim.
*/

/* Toimivat ominaisuudet:
 * 
 */


namespace Error
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class App : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        Random random;

        Texture2D blankTexture;
        Screen currentScreen = Screen.StartScreen;
        Color buttonColor1 = Color.CornflowerBlue;
        Rectangle startButton = new Rectangle(100, 500, 280, 100);

        Color[] mapColors;
        Texture2D mapTexture;
        List<Point> path;
        Point start = new Point(int.MinValue, int.MinValue);
        Point goal = new Point(int.MinValue, int.MinValue);
        SamplerState pointSampler;

        Storage _storage;
        List<Order> _orders;

        // pakkauspöydän sijainti fyysisissä koordinaateissa (ei A* - koordinaateissa)
        Vector3 _packingPosition;


        public App()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = true;
            graphics.SupportedOrientations = DisplayOrientation.Portrait;
            graphics.PreferredBackBufferFormat = SurfaceFormat.Color;
            graphics.PreferredDepthStencilFormat = DepthFormat.None;
            graphics.PreferredBackBufferWidth = 480;
            graphics.PreferredBackBufferHeight = 800;
            graphics.ApplyChanges();

            Content.RootDirectory = "Content";

            // Frame rate is 30 fps by default for Windows Phone.
            TargetElapsedTime = TimeSpan.FromTicks(333333);
            IsFixedTimeStep = true;

            // Extend battery life under lock.
            InactiveSleepTime = TimeSpan.FromSeconds(1);

            // event handlers
            PhoneApplicationService.Current.Activated += AppActivated;
            PhoneApplicationService.Current.Deactivated += AppDeactivated;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            TouchPanel.EnabledGestures = GestureType.FreeDrag | GestureType.Tap;

            blankTexture = new Texture2D(GraphicsDevice, 1, 1);
            blankTexture.SetData(new[] { Color.White });

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("SegoeWP");
            random = new Random();
            pointSampler = new SamplerState();
            pointSampler.AddressU = TextureAddressMode.Clamp;
            pointSampler.AddressV = TextureAddressMode.Clamp;
            pointSampler.Filter = TextureFilter.Point;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            switch (currentScreen)
            {
                case Screen.StartScreen:
                    // Allows the game to exit
                    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                        this.Exit();

                    while (TouchPanel.IsGestureAvailable)
                    {
                        GestureSample gesture = TouchPanel.ReadGesture();
                        switch (gesture.GestureType)
                        {
                            case GestureType.Tap:
                                if (startButton.Contains(gesture.Position))
                                {
                                    currentScreen = Screen.NavigationScreen;

                                    _storage = ReadWareHouseData();
                                    _orders = ReadOrders();
                                    OptimizeOrders(_orders);

                                    while (true)
                                    {
                                        start = new Point(random.Next(_storage.Map.SizeX), random.Next(_storage.Map.SizeY));
                                        goal = new Point(random.Next(_storage.Map.SizeX), random.Next(_storage.Map.SizeY));
                                        if (!_storage.Map[start.X, start.Y].IsTraversable) continue;
                                        if (!_storage.Map[goal.X, goal.Y].IsTraversable) continue;
                                        if (_storage.Map.Contains(start) && _storage.Map.Contains(goal) && start != goal) break;
                                    }
                                    float time;
                                    path = _storage.PathFinder.FindPath(start, goal, out time);
                                    UpdateMapTexture(_storage.Map);
                                }

                                break;
                        }
                    }
                    break;
                case Screen.NavigationScreen:
                    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                        currentScreen = Screen.StartScreen;

                    while (TouchPanel.IsGestureAvailable)
                    {
                        GestureSample gesture = TouchPanel.ReadGesture();
                        switch (gesture.GestureType)
                        {
                            case GestureType.Tap:

                                break;
                        }
                    }
                    break;
            }

            base.Update(gameTime);
        }
        void AppDeactivated(object sender, DeactivatedEventArgs e)
        { 
        
        }
        void AppActivated(object sender, ActivatedEventArgs e)
        {

        }
        void UpdateMapTexture(Map map)
        {
            if (mapColors == null || mapTexture == null || mapColors.Length != map.SizeX * map.SizeY)
            {
                mapColors = new Color[map.SizeX * map.SizeY];
                mapTexture = new Texture2D(GraphicsDevice, map.SizeX, map.SizeY);
            }

            for (int x = 0; x < map.SizeX; x++)
            {
                for (int y = 0; y < map.SizeY; y++)
                {
                    mapColors[x + map.SizeX * y] = map[x, y].IsTraversable ? Color.White : Color.Black;
                }
            }

            if(map.Contains(start))
                mapColors[start.X + start.Y * map.SizeX] = Color.Green;
            if(map.Contains(goal))
                mapColors[goal.X + goal.Y * map.SizeX] = Color.Red;

            if(path != null)
            {
                for(int i=0;i<path.Count;i++)
                {
                    Point p = path[i];
                    mapColors[p.X + p.Y * map.SizeX] = Color.Lerp(Color.Green,Color.Red, ((float)i)/path.Count);
                }
            }

            mapTexture.SetData(mapColors);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            switch (currentScreen)
            {
                case Screen.StartScreen:
                    GraphicsDevice.Clear(Color.WhiteSmoke);
                    spriteBatch.Begin();
                    spriteBatch.DrawStringCentered(font, "Error", new Rectangle(0, 0, 480, 120), Color.Black, 1f);

                    spriteBatch.Draw(blankTexture, startButton, buttonColor1);
                    spriteBatch.DrawStringCentered(font, "Aloita", startButton, Color.Black, 1f);

                    spriteBatch.End();
                    break;
                case Screen.NavigationScreen:
                    GraphicsDevice.Clear(Color.SlateGray);
                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, pointSampler, DepthStencilState.None, RasterizerState.CullNone);
                    spriteBatch.Draw(mapTexture, new Rectangle(0, 160, 480, 480), Color.White);
                    spriteBatch.End();
                    break;
            }           
            base.Draw(gameTime);
        }





        Storage ReadWareHouseData()
        {
            // esimerkki ja testausta varten
            Storage storage = new Storage(100);
            for (int i = 0; i < 200; i++)
            {
                var product = new Product();
                product.ProductCode = i.ToString();
                product.ProductDescription = "ruuvi";
                product.ShelfCode = "1005"; // hyllypaikka, jossa monta lavaa
                product.Amount = 20000;
                product.InsertionDate = DateTime.Now;
                product.ModifiedDate = DateTime.Now;
                product.ProductionDate = new DateTime(2014, 7, 15);

                // tuotteen fyysinen sijainti metreissä
                float x = random.Next(64);
                float y = random.Next(64);
                float z = 0f;
                float width = random.Next(4);// yleensä eurolavan koko
                float height = 1f;
                product.BoundingBox = new BoundingBox(new Vector3(x, y, z), new Vector3(x + width, y + width, z + height));

                storage.Add(product);
            }
            storage.CreateMap(1f);
            return storage;

            // .xml?
        }
        List<Order> ReadOrders()
        {
            List<Order> orders = new List<Order>(1);

            Order order = new Order();
            order.Lines = new List<OrderLine>(2);
            order.Lines.Add(new OrderLine { ProductCode = "27", Amount = 473 });
            order.Lines.Add(new OrderLine { ProductCode = "35", Amount = 3473 });
            orders.Add(order);

            return orders;
        }
        void OptimizeOrders(List<Order> orders)
        {
            
        }
        void OptimizeOrders(ref List<Order> orders, Vector3 startPosition, Vector3 dropoffPosition)
        {
            // optimize order of products in orders
            for (int iOrder = 0; iOrder < orders.Count; iOrder++)
            {
                OptimizeOrder(orders[iOrder], startPosition, dropoffPosition);
            }

            // todo optimize order of orders
            // deadlines
            // other priorities
        }
        void OptimizeOrder(Order order, Vector3 startPosition, Vector3 dropoffPosition)
        {
            // jos kerätään kerralla monta tilausta, ei dropoff-sijainnilla ole väliä
            // create all possible orders in which products can be picked (num_products!)
            var permutations = (OrderLine[][])GetPermutations<OrderLine>(order.Lines, order.Lines.Count);

            // calculate traversal times for all permutations
            float[] costs = new float[Factorial(order.Lines.Count)];
            for (int iPermutation = 0; iPermutation < permutations.GetLength(0); iPermutation++)
            {
                // start at current physical location
                Point currentLocation = _storage.Map.PhysicalToInternalCoordinates(startPosition);
                float time;

                for (int iLine = 0; iLine < order.Lines.Count; iLine++)
                {
                    OrderLine line = permutations[iPermutation][iLine];

                    // find the nearest item in storage that has enough this product
                    Product product = FindNearestToCollect(line.ProductCode, line.Amount, currentLocation);

                    // esteetön sijanti josta tuotetta voidaan kerätä, ts. varastopaikan vieressä
                    Point collectingLocation = _storage.Map.FindCollectingPoint(product.BoundingBox);

                    _storage.PathFinder.FindPath(currentLocation, collectingLocation, out time);
                    costs[iPermutation] += time;
                    currentLocation = collectingLocation;
                }
            }

            // find permutation with lowest traversal time
            float minCost = costs.Min();
            int minIndex = costs.ToList().IndexOf(minCost);
            order.Lines = permutations[minIndex].ToList();
        }

        Product FindNearestToCollect(string productCode, int amount, Point location)
        {
            var items = _storage.GetByProductCode(productCode);
            items = (from item in items where item.Amount >= amount select item).ToList();

            // TODO
            //if(items.Count == 0) tuotetta ei varastossa

            // find nearest product
            BinaryHeap<ComparableIndex> indices = new BinaryHeap<ComparableIndex>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                Point collectionPoint = _storage.Map.FindCollectingPoint(items[i].BoundingBox);
                float time;
                _storage.PathFinder.FindPath(location, collectionPoint, out time);
                indices.Add(new ComparableIndex { Index = i, Cost = time });
            }
            return items[indices.Peek().Index];
        }


        //int[][] CreatePermutations(int[] items)
        //{
        //    int[][] permutations = new int[Factorial(items.Length)][];
        //    for (int perm = 0; perm < permutations.GetLength(0); perm++)
        //    {
        //        permutations[perm] = new int[items.Length];
        //    }

        //    return permutations;
        //}


        // all length! possible orders
        static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return (IEnumerable<IEnumerable<T>>)list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1).SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }
        int Factorial(int x)
        {
            if (x <= 0) return 1;
            int factorial = 1;
            while (x > 1)
            {
                factorial *= x;
                x--;
            }
            return factorial;
        }
    }

    public enum Screen
    {
        StartScreen,
        NavigationScreen
    }
    public struct ComparableIndex : IComparable<ComparableIndex>
    {
        public int Index;
        public float Cost;

        int IComparable<ComparableIndex>.CompareTo(ComparableIndex other)
        {
            return Cost.CompareTo(other.Cost);
        }
    }
}
