using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

/* Toimivat ominaisuudet:
 * Jos tuotetta useissa sijainneissa, lähimmän sijainnin löytäminen jossa riittävästi tuotetta
 * Tilausten rivien järjestäminen siten, että kerätessä koko tilaus kerralla on kokonaismatka lyhin mahdollinen
 * Tekstihaku varastosta toimii periaatteessa, myös osittaisilla teksteillä
 * Keräily sovelluksen ohjaamana toimii
 * Tilausten järjestely alkaa ehkä osittain hahmottua
 */

//ilmari : ui, datan tuonti
//henri: varaston tietorakenne, järjestyksen optimointi

/* TODO
 esteet, jotka eivät tuotteita
 lista mahdollisista hyllypaikoista varastossa tuotteiden sijaintien optimointia varten
 * tekstihakuun myös tilaukset? haku ja muutkin 4 alarivin toimintoa myös aloitussivulle? hakutulosten scrollaaminen,
 * nyt osa ei näy
 * entäs kun kesken keräyksen joku on napannut viimeiset varastosta?
 * optimointi niin, että kaikki työntekijöt eivät ruuhkassa samassa läjässä
 * keräily monesta sijainnista kun yhdessä ei tarpeeksi
 * tilaukset ei järjesty päivämäärän mukaan vaikka pitäis
 * reitti ei näy kartalla kun on just siirrytty seuraavaan riviin
*/

//isoja puuttuvia ominaisuuksia:
//datan tuonti
//Hyllyjen optimaalinen täyttö tuotteilla
// ui osia
// tilausten välinen järjestely

namespace Error
{
    public class App : Microsoft.Xna.Framework.Game
    {
        #region fields and properties
        // frequently needed stuff is now public and static in Error-namespace
        internal static Random Random { get; private set; }
        internal static SpriteFont Font { get; private set; }
        internal static Texture2D Pixel { get; private set; }
        internal static SpriteBatch SpriteBatch { get; private set; }
        internal static SamplerState PointSampler { get; private set; }
        internal static string Message;
        internal static Boolean IsDataImported;
        internal static int screenHeight;
        internal static int screenWidth;

        public OrderManager OrderManager { get; private set; }
        public Storage Storage { get; private set; }
        public CollectingData CollectingData { get; private set; }

        GraphicsDeviceManager graphics;
        Stack<Screen> navigationStack;
        Screen collectingScreen;
        Screen startScreen;
        Screen searchScreen;
        Screen mapScreen;
        Screen showOrdersScreen;
        Texture2D mapIcon, listIcon, changeIcon, searchIcon;

        // tästä eteenpäin loputkin kentät voisi siivota
        Color[] mapColors;// --> map-luokkaan
        Texture2D mapTexture;
        // pakkauspöydän sijainti fyysisissä koordinaateissa (ei A* - koordinaateissa)
        Vector3 _packingPosition;// --> storage-luokkaan

        static App _app;
        public static App Instance
        {
            get { return _app; }
        }
        #endregion

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

            screenHeight = graphics.PreferredBackBufferHeight;
            screenWidth = graphics.PreferredBackBufferWidth;

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
            TouchPanel.EnabledGestures = 
                GestureType.FreeDrag |
                GestureType.Tap |
                GestureType.Flick;
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Font = Content.Load<SpriteFont>("SegoeWP");
            mapIcon = Content.Load<Texture2D>("mapIcon");
            listIcon = Content.Load<Texture2D>("listIcon");
            changeIcon = Content.Load<Texture2D>("changeIcon");
            searchIcon = Content.Load<Texture2D>("searchIcon");
            Random = new Random();
            PointSampler = new SamplerState();
            PointSampler.AddressU = TextureAddressMode.Clamp;
            PointSampler.AddressV = TextureAddressMode.Clamp;
            PointSampler.Filter = TextureFilter.Point;
            Pixel = new Texture2D(GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });

            SetupScreens();
            navigationStack = new Stack<Screen>();
            navigationStack.Push(startScreen);
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
            // back arrow
            if ((!Guide.IsVisible) && GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                if (navigationStack.Count > 1)
                {
                    navigationStack.Pop();
                }
                else
                {
                    this.Exit();
                }
            }

            while (TouchPanel.IsGestureAvailable)
            {
                navigationStack.Peek().Update(TouchPanel.ReadGesture());
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            navigationStack.Peek().Draw();

            base.Draw(gameTime);
        }

        void SetupScreens()
        {
            startScreen = new StartScreen();
            searchScreen = new SearchScreen();
            mapScreen = new MapScreen();
            collectingScreen = new CollectingScreen();
            showOrdersScreen = new SearchScreen();

            #region Buttons
            Button readDataButton = new Button
            {
                Text = "Lue data",
                Name = "readData",
                TouchArea = new Rectangle(60, 400, 360, 90),
                Click = delegate()
                {
                    Storage = ReadStorageData();
                    ReadOrders();

                    // todo
                    Point dropoffAstar;
                    while (true)
                    {
                        dropoffAstar = new Point(Random.Next(Storage.Map.SizeX), Random.Next(Storage.Map.SizeY));
                        if (!Storage.Map[dropoffAstar.X, dropoffAstar.Y].IsTraversable) continue;
                        if (Storage.Map.Contains(dropoffAstar)) break;
                    }
                    // oikeasti tämä on tietysti tiedossa etukäteen
                    _packingPosition = Storage.Map.InternalToPhysicalCoordinates(dropoffAstar);
                    Storage.PackingLocation_AStar = dropoffAstar;

                    IsDataImported = true;
                    ShowMessage("Data luettu");
                }
            };
            Button nextLineButton = new Button
            {
                Name = "nextLine",
                Text = "Seuraava rivi",
                TouchArea = new Rectangle(60, 500, 360, 75),
                Click = delegate()
                {
                    CollectingData.ShowLineInfo = true;
                    CollectingData.ShowOrderInfo = true;
                    CollectingData.SetNextLine();
                    collectingScreen.Buttons["collected"].Visible = true;
                    collectingScreen.Buttons["nextLine"].Visible = false;
                    collectingScreen.Buttons["nextOrder"].Visible = false;
                    collectingScreen.Buttons["packOrder"].Visible = false;
                    collectingScreen.Buttons["packed"].Visible = false;
                }
            };
            Button packOrderButton = new Button
            {
                Name = "packOrder",
                Text = "vie pakattavaksi",
                TouchArea = new Rectangle(60, 580, 360, 75),
                Click = delegate()
                {
                    CollectingData.ShowLineInfo = false;
                    CollectingData.ShowOrderInfo = true;
                    collectingScreen.Buttons["packOrder"].Visible = false;
                    collectingScreen.Buttons["packed"].Visible = true;
                    collectingScreen.Buttons["collected"].Visible = false;
                    collectingScreen.Buttons["nextOrder"].Visible = false;
                    collectingScreen.Buttons["nextLine"].Visible = false;
                }
            };
            Button packedButton = new Button
            {
                Name = "packed",
                Text = "valmis",//viety pakattavaksi/pakkauspisteelle
                TouchArea = new Rectangle(60, 500, 360, 75),
                Click = delegate()
                {
                    CollectingData.ShowLineInfo = false;
                    CollectingData.ShowOrderInfo = false;
                    CollectingData.CurrentLocation_AStar = Storage.Map.PhysicalToInternalCoordinates(_packingPosition);
                    // check if there are orders to collect
                    if(OrderManager.IsOrderAvailable())
                    {
                        collectingScreen.Buttons["nextOrder"].Visible = true;
                        collectingScreen.Buttons["packed"].Visible = false;
                    }
                    else
                    {
                        ShowMessage("Ei kerättävissä olevia tilauksia");
                        return;
                    }
                }
            };
            Button collectedButton = new Button
            {
                Name = "collected",
                Text = "valmis",
                TouchArea = new Rectangle(60, 500, 360, 75),
                Click = delegate()
                {
                    CollectingData.ShowLineInfo = false;
                    CollectingData.ShowOrderInfo = true;
                    CollectingData.CollectCurrentLine();
                    collectingScreen.Buttons["collected"].Visible = false;

                    if (CollectingData.CurrentOrder.State == STATE.COLLECTED)
                    {
                        collectingScreen.Buttons["packOrder"].Visible = true;
                        // check if there are orders to collect
                        if(OrderManager.IsOrderAvailable())
                        {
                            collectingScreen.Buttons["nextOrder"].Visible = true;
                        }
                        else
                        {
                            collectingScreen.Buttons["nextOrder"].Visible = false;
                        }
                    }
                    else
                    {
                        collectingScreen.Buttons["nextLine"].Visible = true;
                        CollectingData.ShowLineInfo = false;
                    }
                }
            };
            Button nextOrderButton = new Button
            {
                Name = "nextOrder",
                Text = "seuraava tilaus",
                TouchArea = new Rectangle(60, 500, 360, 75),
                Click = delegate()
                {
                    CollectingData.ShowLineInfo = true;
                    CollectingData.ShowOrderInfo = true;
                    CollectingData.SetOrder(
                        OrderManager.GetNextToCollect(
                        Storage.Map.InternalToPhysicalCoordinates(CollectingData.CurrentLocation_AStar),
                        Storage.Map.InternalToPhysicalCoordinates(Storage.PackingLocation_AStar)
                        )
                        );
                    collectingScreen.Buttons["nextOrder"].Visible = false;
                    collectingScreen.Buttons["packOrder"].Visible = false;
                    collectingScreen.Buttons["nextLine"].Visible = false;
                    collectingScreen.Buttons["packed"].Visible = false;
                    collectingScreen.Buttons["collected"].Visible = true;
                }
            };
            Button infoButton = new Button
            {
                Name = "info",
                Text = "tiedot",
                Icon = listIcon,
                TouchArea = new Rectangle(120, 680, 120, 120),
                Click = delegate() { /* TODO */}
            };
            Button changeButton = new Button
            {
                Name = "change",
                Text = "muuta",
                Icon = changeIcon,
                TouchArea = new Rectangle(240, 680, 120, 120),
                Click = delegate() { /* TODO */}
            };
            Button mapButton = new Button
            {
                Name = "map",
                Text = "kartta",
                Icon = mapIcon,
                TouchArea = new Rectangle(0, 680, 120, 120),
                Click = delegate()
                {
                    navigationStack.Push(mapScreen);
                    var points = new List<Point>(0);
                    var products = Storage.GetByProductCode(CollectingData.CurrentLine.ProductCode);
                    foreach (var p in products)
                    {
                        points.Add(Storage.Map.PhysicalToInternalCoordinates(p.BoundingBox.Center()));
                    }
                    UpdateMapTexture(Storage.Map, CollectingData.Path, points.ToArray());
                }
            };
            Button searchButton = new Button
            {
                Name = "search",
                Text = "Etsi",
                Icon = searchIcon,
                TouchArea = new Rectangle(360, 680, 120, 120),
                Click = delegate()
                {
                    if (!IsDataImported)
                    {
                        ShowMessage("Virhe : Dataa ei luettu");
                        return;
                    }
                    navigationStack.Push(searchScreen);
                    (searchScreen as SearchScreen).SearchResult = Search(/* TODO args: orders, products, all */);
                }
            };
            Button startCollectingButton = new Button
            {
                Text = "Aloita keräily",
                Name = "startCollecting",
                TouchArea = new Rectangle(60, 500, 360, 90),
                Click = delegate()
                {
                    if (!IsDataImported)
                    {
                        ShowMessage("Virhe : Dataa ei luettu");
                        return;
                    }
                    Message = null;
                    navigationStack.Push(collectingScreen);

                    if(!OrderManager.IsOrderAvailable())
                    {
                        ShowMessage("Ei kerättävissä olevia tilauksia");
                        return;
                    }
                    CollectingData = new CollectingData();
                    CollectingData.CurrentLocation_AStar = Storage.PackingLocation_AStar;
                    CollectingData.SetOrder(
                        OrderManager.GetNextToCollect(
                        Storage.Map.InternalToPhysicalCoordinates(CollectingData.CurrentLocation_AStar),
                        Storage.Map.InternalToPhysicalCoordinates(Storage.PackingLocation_AStar)));

                    collectingScreen.Buttons["nextLine"].Visible = false;
                    collectingScreen.Buttons["nextOrder"].Visible = false;
                    collectingScreen.Buttons["packOrder"].Visible = false;
                    collectingScreen.Buttons["collected"].Visible = true;
                    collectingScreen.Buttons["packed"].Visible = false;
                    CollectingData.ShowLineInfo = true;
                    CollectingData.ShowOrderInfo = true;
                }
            };
            Button showOrdersButton = new Button
            {
                Name = "showOrders",
                Text = "tilaukset",
                Icon = listIcon,
                TouchArea = new Rectangle(240, 680, 120, 120),
                Click = delegate()
                {
                    if (!IsDataImported)
                    {
                        ShowMessage("Virhe : Dataa ei luettu");
                        return;
                    }
                    navigationStack.Push(showOrdersScreen);
                    (showOrdersScreen as SearchScreen).SearchResult = GetOrderInfo();
                }
            };
            #endregion

            startScreen.Add(readDataButton, startCollectingButton, searchButton,showOrdersButton);
            collectingScreen.Add(nextLineButton, mapButton, searchButton,
                infoButton, changeButton, nextOrderButton,
                packOrderButton, collectedButton, packedButton);
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
                mapTexture = new Texture2D(GraphicsDevice, map.SizeX, map.SizeY, false, SurfaceFormat.Color);
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
        public void DrawMapScreen()
        {
            SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, PointSampler, DepthStencilState.None, RasterizerState.CullNone);
            SpriteBatch.Draw(mapTexture, new Rectangle(0, 160, 480, 480), Color.White);
            SpriteBatch.End();
        }
        public List<string> Search()
        {
            Input.ShowKeyboard("hae jotain", "", "ruuvi");
            var foundProducts = App.Instance.Storage.SearchPartialText(Input.GetTypedText());
            var searchResult = new List<string>(0);
            int line = 0;
            foreach (var p in foundProducts)
            {
                searchResult.Add(line + "   " + p.Description + " " + p.Code
                    + "  Hylly: " + p.ShelfCode + "  Määrä: " + p.Amount);
                line++;
            }
            return searchResult;
        }
        public List<string> GetOrderInfo()
        {
            // todo järjestys ja tilauksen tilanne
            var searchResult = new List<string>();
            int line = 0;
            foreach (var o in OrderManager.Orders)
            {
                searchResult.Add(line + "  " + o.Customer + "  " + o.Lines.Count + " riviä " + o.RequestedShippingDate.Date.ToShortDateString());
                line++;
            }
            return searchResult;
        }
        Storage ReadStorageData()
        {
            // esimerkki ja testausta varten
            Storage storage = new Storage(100);
            for (int i = 0; i < 200; i++)
            {
                var product = new Product();
                product.Code = "asd"+(i%36).ToString();
                product.Description = "ruuvi";
                product.ShelfCode = Random.Next(43).ToString(); // hyllypaikka, jossa monta lavaa
                product.Amount = 20000;
                product.PackageSize = 150;
                product.InsertionDate = DateTime.Now;
                product.ModifiedDate = DateTime.Now;
                product.ProductionDate = new DateTime(2014, 7, 15);

                // tuotteen fyysinen sijainti metreissä
                float x = Random.Next(64);
                float y = Random.Next(64);
                float z = 0f;
                float width = Random.Next(4);// yleensä eurolavan koko
                float height = 1f;
                product.BoundingBox = new BoundingBox(new Vector3(x, y, z), new Vector3(x + width, y + width, z + height));

                storage.Add(product);
            }
            storage.CreateMap(1f);
            return storage;
        }
        void ReadOrders()
        {
            OrderManager = new OrderManager();

            Order order = new Order();
            order.Customer = "Oy Asiakas Ab 1";
            order.RequestedShippingDate = DateTime.Today + TimeSpan.FromDays(Random.Next(-3, 10));
            order.Lines = new List<OrderLine>(2);
            order.Lines.Add(new OrderLine { ProductCode = "asd" + "27", Amount = 473 });
            order.Lines.Add(new OrderLine { ProductCode = "asd" + "35", Amount = 3473 });
            order.Lines.Add(new OrderLine { ProductCode = "asd" + "5", Amount = 3373 });

            Order o = new Order();
            o.Customer = "Oy Ab 2";
            o.RequestedShippingDate = DateTime.Today + TimeSpan.FromDays(Random.Next(-3, 10));
            o.Lines = new List<OrderLine>(2);
            o.Lines.Add(new OrderLine { ProductCode = "asd" + "14", Amount = 473 });
            o.Lines.Add(new OrderLine { ProductCode = "asd" + "8", Amount = 3473  + Random.Next(60000)});
            o.Lines.Add(new OrderLine { ProductCode = "asd" + "15", Amount = 373 });

            Order oo = new Order();
            oo.Customer = "asgfdhgfjk 3";
            oo.RequestedShippingDate = DateTime.Today + TimeSpan.FromDays(Random.Next(-3, 10));
            oo.Lines = new List<OrderLine>(2);
            oo.Lines.Add(new OrderLine { ProductCode = "asd" + "14", Amount = 473 });
            oo.Lines.Add(new OrderLine { ProductCode = "asd" + "3", Amount = 3473 + Random.Next(50000) });
            oo.Lines.Add(new OrderLine { ProductCode = "asd" + "15", Amount = 373 });

            Order ooo = new Order();
            ooo.Customer = "lknb nm,lkjk 4";
            ooo.RequestedShippingDate = DateTime.Today + TimeSpan.FromDays(Random.Next(-3, 10));
            ooo.Lines = new List<OrderLine>(2);
            ooo.Lines.Add(new OrderLine { ProductCode = "asd" + "14", Amount = 473 });
            ooo.Lines.Add(new OrderLine { ProductCode = "asd" + "2", Amount = 3473 + Random.Next(40000) });
            ooo.Lines.Add(new OrderLine { ProductCode = "asd" + "15", Amount = 373 });

            OrderManager.Add(order, o, oo, ooo);
        }
        public void OptimizeOrder(Order order, Vector3 startPosition, Vector3 dropoffPosition)
        {
            // jos kerätään kerralla monta tilausta, ei dropoff-sijainnilla ole väliä
            // create all possible orders in which products can be picked (num_products!)
            var permutations = Utils.GetPermutations<OrderLine>(order.Lines.ToArray());

            // calculate traversal times for all permutations
            float minTime = float.MaxValue;
            int minIndex = 0;
            for (int iPermutation = 0; iPermutation < permutations.GetLength(0); iPermutation++)
            {
                // start at current physical location
                Point currentLocation = Storage.Map.PhysicalToInternalCoordinates(startPosition);
                float time = 0;
                for (int iLine = 0; iLine < order.Lines.Count; iLine++)
                {
                    OrderLine line = permutations[iPermutation][iLine];

                    // find the nearest item in storage that has enough this product
                    Product product = Storage.FindNearestToCollect(line.ProductCode, line.Amount, currentLocation);

                    // esteetön sijanti josta tuotetta voidaan kerätä, ts. varastopaikan vieressä
                    Point collectingLocation = Storage.Map.FindCollectingPoint(product.BoundingBox);

                    float dt;
                    Storage.PathFinder.FindPath(currentLocation, collectingLocation, out dt);
                    time += dt;
                    currentLocation = collectingLocation;
                }
                if (time < minTime)
                {
                    minTime = time;
                    minIndex = iPermutation;
                }
            }
            order.Lines = permutations[minIndex].ToList();
        }
        public void ShowMessage(string message)
        {
            Message = message;
            navigationStack.Clear();
            navigationStack.Push(startScreen);
        }
    }

    public class CollectingData
    {
        public List<Point> Path;
        public Product CurrentProduct;
        public Order CurrentOrder;
        public int CurrentLineIndex;
        public bool ShowLineInfo = true;
        public bool ShowOrderInfo = true;
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
            CurrentLine.State = STATE.COLLECTED;
            App.Instance.Storage.Collect(CurrentProduct, CurrentLine.Amount);
            CurrentLocation_AStar = CurrentDestination_AStar;

            if (CurrentLineIndex >= CurrentOrder.Lines.Count - 1)
            {
                App.Instance.OrderManager.ChangeState(CurrentOrder, STATE.COLLECTED);
            }
        }
        public void SetNextLine()
        {
            CurrentLineIndex++;
            OrderLine line = CurrentLine;
            // todo check currentproduct
            CurrentProduct = App.Instance.Storage.FindNearestToCollect(line.ProductCode, line.Amount, CurrentLocation_AStar);
            CurrentDestination_AStar = App.Instance.Storage.Map.FindCollectingPoint(CurrentProduct.BoundingBox);
            float time;
            Path = App.Instance.Storage.PathFinder.FindPath(CurrentLocation_AStar, CurrentDestination_AStar, out time);
        }
        public void SetOrder(Order order)
        {
            CurrentOrder = order;
            App.Instance.OrderManager.ChangeState(CurrentOrder, STATE.COLLECTING_STARTED);
            CurrentLineIndex = -1;
            SetNextLine();
        }
    }
}
