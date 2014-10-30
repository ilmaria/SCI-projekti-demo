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
        Color dataStateColor = Color.Red;
        Rectangle testButton = new Rectangle(100, 600, 280, 90);
        Rectangle readDataButton = new Rectangle(100, 400, 280, 90);
        Rectangle startCollectingButton = new Rectangle(100, 500, 280, 90);
        Rectangle nextLineButton = new Rectangle(100, 500, 280, 90);
        Rectangle goPackButton = new Rectangle(100, 600, 280, 90);
        Rectangle showMapButton = new Rectangle(0, 700, 100, 100);
        string errorText = null;

        Texture2D mapIcon;
        Color[] mapColors;
        Texture2D mapTexture;
        List<Point> path;
        Point start = new Point(int.MinValue, int.MinValue);
        Point goal = new Point(int.MinValue, int.MinValue);
        SamplerState pointSampler;

        public Storage Storage { get; private set; }
        List<Order> _orders;

        // pakkauspöydän sijainti fyysisissä koordinaateissa (ei A* - koordinaateissa)
        Vector3 _packingPosition;
        CollectingData _collectingData;

        static App _app;
        public static App Instance
        {
            get { return _app; }
        }


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
            _app = this;
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
            mapIcon = Content.Load<Texture2D>("mapIcon");
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
                                if (testButton.Contains(gesture.Position))
                                {
                                    if (_orders == null || Storage == null)
                                    {
                                        ShowError("Dataa ei luettu");
                                        return;
                                    }
                                    currentScreen = Screen.Test;
                                    dataStateColor = Color.Red;

                                    while (true)
                                    {
                                        start = new Point(random.Next(Storage.Map.SizeX), random.Next(Storage.Map.SizeY));
                                        goal = new Point(random.Next(Storage.Map.SizeX), random.Next(Storage.Map.SizeY));
                                        if (!Storage.Map[start.X, start.Y].IsTraversable) continue;
                                        if (!Storage.Map[goal.X, goal.Y].IsTraversable) continue;
                                        if (Storage.Map.Contains(start) && Storage.Map.Contains(goal) && start != goal) break;
                                    }
                                    float time;
                                    path = Storage.PathFinder.FindPath(start, goal, out time);
                                    UpdateMapTexture(Storage.Map, path);
                                }
                                else if (startCollectingButton.Contains(gesture.Position))
                                {
                                    if (_orders == null || Storage == null)
                                    {
                                        ShowError("Dataa ei luettu");
                                        return;
                                    }
                                    currentScreen = Screen.CollectingScreen;
                                    dataStateColor = Color.Red;
                                    Point dropoffAstar;
                                    while (true)
                                    {
                                        dropoffAstar = new Point(random.Next(Storage.Map.SizeX), random.Next(Storage.Map.SizeY));
                                        if (!Storage.Map[dropoffAstar.X, dropoffAstar.Y].IsTraversable) continue;
                                        if (Storage.Map.Contains(dropoffAstar)) break;
                                    }
                                    // oikeasti tämä on tietysti tiedossa etukäteen
                                    _packingPosition = Storage.Map.InteralToPhysicalCoordinates(dropoffAstar);

                                    //OptimizeOrders(_orders, _packingPosition, _packingPosition);

                                    _collectingData = new CollectingData();
                                    // TODO check
                                    _collectingData.CurrentOrder = _orders[0];
                                    _collectingData.CurrentLocation_AStar = dropoffAstar;
                                    _collectingData.CurrentLineIndex = -1;//...
                                    _collectingData.SetNextLine();
                                }
                                else if (readDataButton.Contains(gesture.Position))
                                {
                                    Storage = ReadWareHouseData();
                                    _orders = ReadOrders();
                                    dataStateColor = Color.Green;
                                }
                                break;
                        }
                    }
                    break;
                case Screen.Test:
                    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                        currentScreen = Screen.StartScreen;
                    break;
                case Screen.CollectingScreen:
                    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                        currentScreen = Screen.StartScreen;

                    while (TouchPanel.IsGestureAvailable)
                    {
                        GestureSample gesture = TouchPanel.ReadGesture();
                        switch (gesture.GestureType)
                        {
                            case GestureType.Tap:
                                if (nextLineButton.Contains(gesture.Position))
                                {
                                    _collectingData.CollectCurrentLine();
                                    _collectingData.SetNextLine();
                                }
                                else if (goPackButton.Contains(gesture.Position))
                                {
                                    // TODO
                                }
                                else if (showMapButton.Contains(gesture.Position))
                                {
                                    currentScreen = Screen.Map;
                                    UpdateMapTexture(Storage.Map, _collectingData.Path,
                                        Storage.Map.PhysicalToInternalCoordinates(_collectingData.CurrentProduct.BoundingBox.Center()),
                                        _collectingData.CurrentLocation_AStar);
                                }
                                break;
                        }
                    }
                    break;
                case Screen.Map:
                    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                        currentScreen = Screen.CollectingScreen;
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
        void UpdateMapTexture(Map map, List<Point> path, params Point[] highlights)
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

            if(path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    Point p = path[i];
                    mapColors[p.X + p.Y * map.SizeX] = Color.Lerp(Color.Green, Color.Red, ((float)i) / path.Count);
                }
            }
            foreach (Point p in highlights)
            {
                mapColors[p.X + p.Y * map.SizeX] = Color.Gold;
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

                    spriteBatch.Draw(blankTexture, testButton, buttonColor1);
                    spriteBatch.DrawStringCentered(font, "testi", testButton, Color.Black, 1f);

                    spriteBatch.Draw(blankTexture, startCollectingButton, buttonColor1);
                    spriteBatch.DrawStringCentered(font, "Aloita keräily", startCollectingButton, Color.Black, 1f);

                    spriteBatch.Draw(blankTexture, readDataButton, dataStateColor);
                    spriteBatch.DrawStringCentered(font, "Lue tiedot", readDataButton, Color.Black, 1f);

                    if (errorText != null)
                    {
                        var btn = new Rectangle(50, 200, 380, 60);
                        spriteBatch.DrawStringCentered(font, "Virhe: " + errorText, btn, Color.Black, 0.6f);
                    }

                    spriteBatch.End();
                    break;
                case Screen.Test:
                    GraphicsDevice.Clear(Color.Purple);
                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, pointSampler, DepthStencilState.None, RasterizerState.CullNone);
                    spriteBatch.Draw(mapTexture, new Rectangle(0, 160, 480, 480), Color.White);
                    spriteBatch.End();
                    break;
                case Screen.Map:
                    GraphicsDevice.Clear(Color.White);
                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, pointSampler, DepthStencilState.None, RasterizerState.CullNone);
                    spriteBatch.Draw(mapTexture, new Rectangle(0, 160, 480, 480), Color.White);
                    spriteBatch.End();
                    break;
                case Screen.CollectingScreen:
                    GraphicsDevice.Clear(Color.White);
                    spriteBatch.Begin();
                    Order order = _collectingData.CurrentOrder;
                    if (order != null)
                    {
                        spriteBatch.DrawStringCentered(font, order.Customer, new Rectangle(0, 0, 240, 50), Color.Black, 0.5f);
                        spriteBatch.DrawStringCentered(font, order.RequestedShippingDate.ToString(), new Rectangle(240, 0, 240, 50), Color.Black, 0.5f);
                    }
                    OrderLine line = _collectingData.CurrentLine;
                    Product product = _collectingData.CurrentProduct;
                    if (line != null && product != null)
                    {
                        spriteBatch.DrawStringCentered(font, product.ProductDescription, new Rectangle(40, 200, 400, 100), Color.Black, 1f);
                        spriteBatch.DrawStringCentered(font, line.Amount + " kpl, " + line.Amount/product.PackageSize + " pakettia", new Rectangle(40, 300, 400, 100), Color.Black, 1f);
                        spriteBatch.DrawStringCentered(font, "Tuotekoodi: " + product.ProductCode, new Rectangle(40, 400, 400, 100), Color.Black, 1f);
                    }

                    spriteBatch.Draw(blankTexture, nextLineButton, buttonColor1);
                    spriteBatch.DrawStringCentered(font, "Seuraava rivi", nextLineButton, Color.Black, 1f);

                    spriteBatch.Draw(blankTexture, goPackButton, buttonColor1);
                    spriteBatch.DrawStringCentered(font, "Pakkaamaan", goPackButton, Color.Black, 1f);

                    spriteBatch.Draw(blankTexture, showMapButton, new Color(200, 200, 200, 255));
                    var rect = showMapButton;
                    rect.Inflate(-15, -15);
                    spriteBatch.Draw(mapIcon, rect, Color.DarkSlateGray);

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
                product.PackageSize = 150;
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
            order.Customer = "Oy Asiakas Ab";
            order.RequestedShippingDate = DateTime.Today;
            order.Lines = new List<OrderLine>(2);
            order.Lines.Add(new OrderLine { ProductCode = "27", Amount = 473 });
            order.Lines.Add(new OrderLine { ProductCode = "35", Amount = 3473 });
            order.Lines.Add(new OrderLine { ProductCode = "5", Amount = 3373 });
            orders.Add(order);

            return orders;
        }
        void OptimizeOrders(List<Order> orders)
        {
            
        }
        void OptimizeOrders(List<Order> orders, Vector3 startPosition, Vector3 dropoffPosition)
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
                Point currentLocation = Storage.Map.PhysicalToInternalCoordinates(startPosition);
                float time;

                for (int iLine = 0; iLine < order.Lines.Count; iLine++)
                {
                    OrderLine line = permutations[iPermutation][iLine];

                    // find the nearest item in storage that has enough this product
                    Product product = Storage.FindNearestToCollect(line.ProductCode, line.Amount, currentLocation);

                    // esteetön sijanti josta tuotetta voidaan kerätä, ts. varastopaikan vieressä
                    Point collectingLocation = Storage.Map.FindCollectingPoint(product.BoundingBox);

                    Storage.PathFinder.FindPath(currentLocation, collectingLocation, out time);
                    costs[iPermutation] += time;
                    currentLocation = collectingLocation;
                }
            }

            // find permutation with lowest traversal time
            float minCost = costs.Min();
            int minIndex = costs.ToList().IndexOf(minCost);
            order.Lines = permutations[minIndex].ToList();
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
        static int Factorial(int x)
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
        public void ShowError(string message)
        {
            errorText = message;
            currentScreen = Screen.StartScreen;
        }
    }

    public enum Screen
    {
        StartScreen,
        Test,
        CollectingScreen,
        Map
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
    public class CollectingData
    {
        public List<Point> Path;
        public Product CurrentProduct;
        public Order CurrentOrder;
        public int CurrentLineIndex;
        public OrderLine CurrentLine
        {
            get
            {
                if (CurrentOrder == null) return null;
                if (CurrentLineIndex >= CurrentOrder.Lines.Count) return null;
                return CurrentOrder.Lines[CurrentLineIndex];
            }
        }

        public Vector3 CurrentLocation;
        public Vector3 CurrentDestination;
        public Vector3 DropOffPoint;
        // samat eri koordinaateissa
        public Point CurrentLocation_AStar;
        public Point CurrentDestination_AStar;
        public Point DropOffPoint_AStar;

        public void CollectCurrentLine()
        {
            OrderLine line = CurrentLine;
            line.State = LineState.Collected;
            App.Instance.Storage.Collect(CurrentProduct, line.Amount);
            CurrentLocation_AStar = CurrentDestination_AStar;

            if (CurrentLineIndex >= CurrentOrder.Lines.Count)
            {
                CurrentOrder.State = OrderState.Collected;
            }
        }
        public void SetNextLine()
        {
            CurrentLineIndex++;
            OrderLine nextLine = CurrentOrder.Lines[CurrentLineIndex];
            CurrentProduct = App.Instance.Storage.FindNearestToCollect(nextLine.ProductCode, nextLine.Amount, CurrentLocation_AStar);
            CurrentDestination_AStar = App.Instance.Storage.Map.FindCollectingPoint(CurrentProduct.BoundingBox);
            float time;
            Path = App.Instance.Storage.PathFinder.FindPath(CurrentLocation_AStar, CurrentDestination_AStar, out time);
        }
    }
}
