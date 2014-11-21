using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System.Globalization;

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
 * tekstihakuun myös tilaukset?
 * entäs kun kesken keräyksen joku on napannut viimeiset varastosta?
 * optimointi niin, että kaikki työntekijöt eivät ruuhkassa samassa läjässä
 * keräily monesta sijainnista kun yhdessä ei tarpeeksi
 * tilaukset ei järjesty päivämäärän mukaan vaikka pitäis
 * reitti ei näy kartalla kun on just siirrytty seuraavaan riviin
*/

//isoja puuttuvia ominaisuuksia:
//Hyllyjen optimaalinen täyttö tuotteilla
// tilausten välinen järjestely

// varastopaikka = shelfCode = 1005
// lavapaikka = palletCode = 1005E/3

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
        Screen orderInfoScreen;
        Screen productInfoScreen;
        Screen showOrdersScreen;
        Texture2D mapIcon, listIcon, changeIcon, searchIcon, locationIcon;

        // tästä eteenpäin loputkin kentät voisi siivota
        Color[] mapColors;// --> map-luokkaan
        Texture2D mapTexture;
        int lastKey = 0;
        static App _app;
        public static App Instance
        {
            get { return _app; }
        }
        CultureInfo fin = new CultureInfo("fi-FI");
        public const int INVALID_KEY = int.MinValue;

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
            locationIcon = Content.Load<Texture2D>("locationIcon");
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

            //UI.ForegroundColor = Color.BurlyWood; jne
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
                    Save("autosave");
                    this.Exit();
                }
            }

            while (TouchPanel.IsGestureAvailable)
            {
                navigationStack.Peek().ProcessInput(TouchPanel.ReadGesture());
            }
            navigationStack.Peek().Update((float)TargetElapsedTime.TotalSeconds);

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
            searchScreen = new Screen("search", "Hakutulokset");
            searchScreen.IsScrollable = true;
            mapScreen = new MapScreen();
            collectingScreen = new CollectingScreen();
            orderInfoScreen = new Screen("orderInfo", "Tilauksen tuotteet");
            orderInfoScreen.IsScrollable = true;
            productInfoScreen = new Screen("productInfo", "Tuotetiedot");
            showOrdersScreen = new Screen("showOrders", "Kaikki tilaukset");
            showOrdersScreen.IsScrollable = true;

            #region Buttons
            Button readDataButton = new Button
            {
                Text = "Lue data",
                Name = "readData",
                TouchArea = new Rectangle(60, 300, 360, 90),
                Click = delegate()
                {
                    Storage = ReadMapFromTextFile(Error.IO.ReadAllLines(@"InputFiles/map.txt"));
                    ReadProductsFromTextFile(Storage, Error.IO.ReadAllLines(@"InputFiles/products.txt"));
                    OrderManager = new OrderManager();
                    ReadOrdersFromTextFile(Error.IO.ReadAllLines(@"InputFiles/orders.txt"));
                    IsDataImported = true;
                    ShowMessage("Data luettu");
                }
            };
            // lue tallennettu data
            Button loadButton = new Button
            {
                Text = "Avaa",
                Name = "load",
                TouchArea = new Rectangle(60, 500, 180, 90),
                Click = delegate()
                {
                    Error.IO.ShowKeyboard("tiedoston nimi", "", "");
                    string filename = Error.IO.GetTypedText();
                    Load(filename);
                }
            };
            // tallenna data
            Button saveButton = new Button
            {
                Text = "Tallenna",
                Name = "save",
                TouchArea = new Rectangle(240, 500, 180, 90),
                Click = delegate()
                {
                    Error.IO.ShowKeyboard("tiedoston nimi", "", "");
                    string filename = Error.IO.GetTypedText();
                    Save(filename);
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
                    collectingScreen.ClickableElements["collected"].Visible = true;
                    collectingScreen.ClickableElements["nextLine"].Visible = false;
                    collectingScreen.ClickableElements["nextOrder"].Visible = false;
                    collectingScreen.ClickableElements["packOrder"].Visible = false;
                    collectingScreen.ClickableElements["packed"].Visible = false;
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
                    collectingScreen.ClickableElements["packOrder"].Visible = false;
                    collectingScreen.ClickableElements["packed"].Visible = true;
                    collectingScreen.ClickableElements["collected"].Visible = false;
                    collectingScreen.ClickableElements["nextOrder"].Visible = false;
                    collectingScreen.ClickableElements["nextLine"].Visible = false;
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
                    CollectingData.CurrentLocation_AStar = Storage.PackingLocation_AStar;
                    // check if there are orders to collect
                    if (OrderManager.IsOrderAvailable())
                    {
                        collectingScreen.ClickableElements["nextOrder"].Visible = true;
                        collectingScreen.ClickableElements["packed"].Visible = false;
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
                    collectingScreen.ClickableElements["collected"].Visible = false;

                    if (CollectingData.CurrentOrder.State == STATE.COLLECTED)
                    {
                        collectingScreen.ClickableElements["packOrder"].Visible = true;
                        // check if there are orders to collect
                        if (OrderManager.IsOrderAvailable())
                        {
                            collectingScreen.ClickableElements["nextOrder"].Visible = true;
                        }
                        else
                        {
                            collectingScreen.ClickableElements["nextOrder"].Visible = false;
                        }
                    }
                    else
                    {
                        collectingScreen.ClickableElements["nextLine"].Visible = true;
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
                    collectingScreen.ClickableElements["nextOrder"].Visible = false;
                    collectingScreen.ClickableElements["packOrder"].Visible = false;
                    collectingScreen.ClickableElements["nextLine"].Visible = false;
                    collectingScreen.ClickableElements["packed"].Visible = false;
                    collectingScreen.ClickableElements["collected"].Visible = true;
                }
            };
            Button infoButton = new Button
            {
                Name = "info",
                Text = "tiedot",
                Icon = listIcon,
                TouchArea = new Rectangle(120, 680, 120, 120),
                Click = delegate()
                {
                    if (!IsDataImported)
                    {
                        ShowMessage("Virhe : Dataa ei luettu");
                        return;
                    }
                    showOrderInfo(CollectingData.CurrentOrder);
                }
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
                        points.Add(Storage.Map.PhysicalToInternalCoordinates(Storage.GetProduct(p).BoundingBox.Center()));
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
                Click = delegate() { SearchStorage(); }
            };
            Button startCollectingButton = new Button
            {
                Text = "Aloita keräily",
                Name = "startCollecting",
                TouchArea = new Rectangle(60, 400, 360, 90),
                Click = delegate()
                {
                    if (!IsDataImported)
                    {
                        ShowMessage("Virhe : Dataa ei luettu");
                        return;
                    }
                    Message = null;
                    navigationStack.Push(collectingScreen);

                    if (!OrderManager.IsOrderAvailable())
                    {
                        ShowMessage("Ei kerättävissä olevia tilauksia");
                        return;
                    }
                    //Error.IO.ShowKeyboard("numero","","");
                    //int n = int.Parse(Error.IO.GetTypedText());
                    //var order = OrderManager.Orders[n];
                    CollectingData = new CollectingData();
                    CollectingData.CurrentLocation_AStar = Storage.PackingLocation_AStar;
                    //OptimizeOrder(order, Storage.Map.InternalToPhysicalCoordinates(Storage.PackingLocation_AStar),
                    //    Storage.Map.InternalToPhysicalCoordinates(Storage.PackingLocation_AStar));
                    //CollectingData.SetOrder(order);
                        CollectingData.SetOrder(OrderManager.GetNextToCollect(
                        Storage.Map.InternalToPhysicalCoordinates(CollectingData.CurrentLocation_AStar),
                        Storage.Map.InternalToPhysicalCoordinates(Storage.PackingLocation_AStar)));

                    collectingScreen.ClickableElements["nextLine"].Visible = false;
                    collectingScreen.ClickableElements["nextOrder"].Visible = false;
                    collectingScreen.ClickableElements["packOrder"].Visible = false;
                    collectingScreen.ClickableElements["collected"].Visible = true;
                    collectingScreen.ClickableElements["packed"].Visible = false;
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
                Click = ShowOrders
            };
            // tää nappi näyttää varaston kaikki samaa tuotekoodia olevat tuotteet
            Button showProductsButton = new Button
            {
                Name = "showProducts",
                Text = "Kaikki varastopaikat",
                TouchArea = new Rectangle(10, 680, 460, 110),
                IsFixedPosition = true,
                Click = delegate() { SearchStorage(productInfoScreen.productCode); }
            };
            #endregion

            startScreen.Add(readDataButton, startCollectingButton, searchButton, showOrdersButton, loadButton,saveButton);
            collectingScreen.Add(nextLineButton, mapButton, searchButton,
                infoButton, changeButton, nextOrderButton,
                packOrderButton, collectedButton, packedButton);
            productInfoScreen.Add(showProductsButton);
            //showOrdersScreen.Add(infoButton);
        }
        void AppDeactivated(object sender, DeactivatedEventArgs e)
        {
            Save("autosave");
        }
        void AppActivated(object sender, ActivatedEventArgs e)
        {
        }
        void Save(string filename)
        {
            var orderdat = WriteOrdersToTextFile(OrderManager.Orders);
            Error.IO.SaveText(filename + "_orders", orderdat);
            var pdat = WriteProductsToTextFile(Storage._____products);
            Error.IO.SaveText(filename + "_products", pdat);
            ShowMessage("Data tallennettu");
        }
        void Load(string filename)
        {
            // kartta ei muutu
            Storage = ReadMapFromTextFile(Error.IO.ReadAllLines(@"InputFiles/map.txt"));

            // tilaukset ja varastossa olevat asiat muuttuu
            ReadProductsFromTextFile(Storage, Error.IO.LoadText(filename + "_products"));
            OrderManager = new OrderManager();
            ReadOrdersFromTextFile(Error.IO.LoadText(filename + "_orders"));
            IsDataImported = true;
            ShowMessage("Data luettu");
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

            if (path != null)
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
            GraphicsDevice.Clear(Color.Gray);
            SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, PointSampler, DepthStencilState.None, RasterizerState.CullNone);
            SpriteBatch.Draw(mapTexture, new Rectangle(0, 160, 480, 480), Color.White);
            Point location = CollectingData.CurrentLocation_AStar;
            int size = (int)(5f / Storage.Map.ResolutionInMetres);
            Rectangle r = new Rectangle(location.X - size / 2, location.Y - size, size, size);
            r.X = r.X * screenWidth / Storage.Map.SizeX;
            r.Y = r.Y * screenWidth / Storage.Map.SizeY + 160;
            r.Width = r.Width * screenWidth / Storage.Map.SizeX;
            r.Height = r.Height * screenWidth / Storage.Map.SizeY;
            SpriteBatch.Draw(locationIcon, r, Color.Red);
            SpriteBatch.End();
        }
        public void SearchStorage(params string[] text)
        {
            if (!IsDataImported)
            {
                ShowMessage("Virhe : Dataa ei luettu");
                return;
            }
            navigationStack.Push(searchScreen);

            List<int> foundProducts;
            if (text.Length > 0)
            {
                foundProducts = App.Instance.Storage.SearchExactText(text[0]);
            }
            else
            {
                IO.ShowKeyboard("Hae varastosta", "", "ruuvi");
                foundProducts = App.Instance.Storage.SearchPartialText(IO.GetTypedText(), 100);
            }

            searchScreen.Offset = 0;
            int index = 1;
            Point offset = new Point(0, 100);
            int itemHeight = 50;
            foreach (var k in foundProducts)
            {
                var p = Storage.GetProduct(k);
                var item = new ListItem(index, offset,
                    p.PalletCode,
                    new List<string> 
                    { 
                        p.Description + "  " + p.Code,
                        p.Amount + " kpl hyllyssä"
                    },
                    delegate() { showProductInfo(p); },
                    index.ToString(),
                    false);

                itemHeight = item.TouchArea.Height + 10;
                searchScreen.Add(item);
                offset.Y += itemHeight;
                index++;
            }
            searchScreen.Height = 100 + foundProducts.Count * itemHeight;
        }
        public void ShowOrders()
        {
            if (!IsDataImported)
            {
                ShowMessage("Virhe : Dataa ei luettu");
                return;
            }
            navigationStack.Push(showOrdersScreen);
            showOrdersScreen.Offset = 0;
            OrderManager.EnsureSort();
            var orders = OrderManager.Orders;
            int index = 1;
            Point offset = new Point(0, 100);
            int itemHeight = 50;
            foreach (var o in orders)
            {
                var item = new ListItem(index, offset,
                    o.Customer,
                    new List<string> 
                            { 
                                o.RequestedShippingDate.Date.ToShortDateString(),
                                o.Lines.Count.ToString() + " riviä" 
                            },
                    delegate() { showOrderInfo(o); },
                    index.ToString(),
                    false);

                itemHeight = item.TouchArea.Height + 10;
                showOrdersScreen.Add(item);
                offset.Y += itemHeight;
                index++;
            }
            showOrdersScreen.Height = 100 + orders.Count * itemHeight;
        }
        /*
         * Näytä yksittäisen tilauksen tiedot. Voi myös tarkastella yksittäisiä tuotteita.
         */
        public void showOrderInfo(Order order)
        {
            navigationStack.Push(orderInfoScreen);

            if (order != null)
            {
                int index = 1;
                Point offset = new Point(0, 100);
                int itemHeight = 0;
                Point currentLocation = Storage.PackingLocation_AStar;
                if (CollectingData != null)
                {
                    currentLocation = CollectingData.CurrentLocation_AStar;
                }

                // vanhat täytyy poistaa
                var itemstoremove = (from i in orderInfoScreen.ClickableElements where (i.Value as ListItem != null) select i.Key).ToList();
                foreach (var key in itemstoremove)
                {
                    orderInfoScreen.ClickableElements.Remove(key);
                }
                
                foreach (var line in order.Lines)
                {
                    var item = new ListItem(
                        index,
                        offset,
                        string.Empty,
                        new List<string>
                                {
                                    //"Lähimmän tuotteen tiedot",
                                    "Tuotekoodi: " + line.ProductCode,
                                    //"Tuote: " + product.Description,
                                    //"Lavapaikka: " + product.PalletCode,    // oikea nimi?
                                    //"Hyllypaikka: " + product.ShelfCode,    // oikea nimi?
                                    "Määrä: " + line.Amount.ToString(),
                                    //"Pakettikoko: " + product.PackageSize.ToString(),
                                    //"Valmistuspäivä: " + product.ProductionDate.ToShortDateString(),
                                    //"Saapunut varastoon: " + product.InsertionDate.ToShortDateString(),
                                    //"Muokattu viimeksi: " + product.ModifiedDate.ToShortDateString(),    // onko parempaa nimeä?
                                    //"Lisätiedot: " + product.ExtraNotes
                                    /*"Sijainti: " + product.BoundingBox*/
                                },
                        delegate() { },
                        index.ToString(),
                        false);

                    int productKey = Storage.FindNearestToCollect(line.ProductCode, 0, currentLocation);
                    if (productKey == INVALID_KEY)
                    { // tuotetta ei varastossa
                        item.Title = "Tuotetta ei varastossa";
                    }
                    else
                    {
                        Product product = Storage.GetProduct(productKey);
                        item.Title = product.Description;
                        item.Click = delegate() { showProductInfo(product); };
                    }

                    itemHeight = item.TouchArea.Height + 10;
                    orderInfoScreen.Add(item);
                    offset.Y += itemHeight;
                    index++;
                }
                orderInfoScreen.Height = 100 + (index - 1) * itemHeight;
            }

        }
        public void showProductInfo(Product product)
        {
            if (navigationStack.Contains(productInfoScreen))
            {
                while (navigationStack.Pop() != productInfoScreen) { continue; }
            }
            navigationStack.Push(productInfoScreen);
            productInfoScreen.productCode = product.Code;

            int index = 1;
            Point offset = new Point(0, 100);
            int itemHeight = 0;

            var item = new ListItem(
                index,
                offset,
                product.Description,
                new List<string>
                        {
                            "Lähimmän tuotteen tiedot",
                            "Tuotekoodi: " + product.Code,
                            "Tuote: " + product.Description,
                            "Lavapaikka: " + product.PalletCode,    // oikea nimi?
                            "Varastopaikka: " + product.ShelfCode,    // oikea nimi?
                            "Määrä: " + product.Amount.ToString(),
                            "Pakettikoko: " + product.PackageSize.ToString(),
                            "Valmistuspäivä: " + product.ProductionDate.ToShortDateString(),
                            "Saapunut varastoon: " + product.InsertionDate.ToShortDateString(),
                            "Muokattu viimeksi: " + product.ModifiedDate.ToShortDateString(),    // onko parempaa nimeä?
                            "Lisätiedot: " + product.ExtraNotes
                            /*"Sijainti: " + product.BoundingBox*/
                        },
                null,
                index.ToString(),
                false);

            itemHeight = item.TouchArea.Height + 10;
            productInfoScreen.Add(item);

            // säädä "showProducts" -napin sijainti sopivaksi
            var showProductsButton = productInfoScreen.ClickableElements["showProducts"];
            //showProductsButton.TouchArea.Y = itemHeight + 120;

            productInfoScreen.Height = 100 + itemHeight + showProductsButton.TouchArea.Height;
        }
        public int GetUniqueKey()
        {
            int key = lastKey;
            lastKey++;
            return key;
        }
        Storage ReadMapFromTextFile(string[] textLines)
        {
            float scale = 0.001f;
            Storage storage = new Storage(0);
            string[] args;
            float x, y, z, dx, dy, dz;
            Vector3 packingPosition = Vector3.Zero;
            foreach (string line in textLines)
            {
                string key, value;
                if (ParseLine(line, out key, out value))
                {
                    switch (key)
                    {
                        case "VARASTO":
                            args = value.Split(' ');
                            x = int.Parse(args[0]) * scale;
                            y = int.Parse(args[1]) * scale;
                            z = int.Parse(args[2]) * scale;
                            dx = int.Parse(args[3]) * scale;
                            dy = int.Parse(args[4]) * scale;
                            dz = int.Parse(args[5]) * scale;
                            storage.BoundingBox = new BoundingBox(new Vector3(x, y, z), new Vector3(x + dx, y + dy, z + dz));
                            break;
                        case "PAKKAUSPISTE":
                            args = value.Split(' ');
                            x = int.Parse(args[0]) * scale;
                            y = int.Parse(args[1]) * scale;
                            z = int.Parse(args[2]) * scale;
                            packingPosition = new Vector3(x, y, z);
                            break;
                        case "SHELF":
                        case "HYLLY":
                            args = value.Split(' ');
                            string shelfCode = args[0];
                            string corridor = args[1];
                            float x0 = int.Parse(args[2]) * scale;
                            float y0 = int.Parse(args[3]) * scale;
                            float z0 = int.Parse(args[4]) * scale;
                            int nx = int.Parse(args[5]);
                            int ny = int.Parse(args[6]);
                            int nz = int.Parse(args[7]);

                            // lisätään kaikki tässä varastopaikassa olevat lavapaikat
                            for (int ix = 0; ix < nx; ix++)
                            {
                                for (int iy = 0; iy < ny; iy++)
                                {
                                    for (int iz = 0; iz < nz; iz++)
                                    {
                                        Pallet pallet = new Pallet();
                                        pallet.ShelfCode = shelfCode; // 1005
                                        pallet.PalletCode = shelfCode + corridor + "/" + (iz + 1); // 1005D/3
                                        x = x0 + ix * Pallet.EUR_PALLET_LONG_SIDE_METERS;
                                        y = y0 + iy * (Pallet.EUR_PALLET_SHORT_SIDE_METERS + 0.2f);
                                        z = z0 + iz * Pallet.EUR_PALLET_HEIGHT_METERS;

                                        pallet.BoundingBox = new BoundingBox(new Vector3(x, y, z),
                                            new Vector3(x + Pallet.EUR_PALLET_LONG_SIDE_METERS,
                                                y + (Pallet.EUR_PALLET_SHORT_SIDE_METERS + 0.2f),
                                                z + Pallet.EUR_PALLET_HEIGHT_METERS));

                                        storage.Pallets.Add(pallet);
                                    }
                                }
                            }
                            break;
                        case "ESTE":
                            args = value.Split(' ');
                            x = int.Parse(args[0]) * scale;
                            y = int.Parse(args[1]) * scale;
                            z = int.Parse(args[2]) * scale;
                            dx = int.Parse(args[3]) * scale;
                            dy = int.Parse(args[4]) * scale;
                            dz = int.Parse(args[5]) * scale;
                            var b = new BoundingBox(new Vector3(x, y, z), new Vector3(x + dx, y + dy, z + dz));
                            storage.Add(b);
                            break;
                    }
                }
            }
            storage.CreateMap(0.25f);
            storage.PackingLocation_AStar = storage.Map.PhysicalToInternalCoordinates(packingPosition);
            return storage;
        }
        void ReadProductsFromTextFile(Storage storage, string[] textLines)
        {
            Product product = new Product(); // assign value only to suppress compiler error

            foreach (string line in textLines)
            {
                string key, value;
                if (ParseLine(line, out key, out value))
                {
                    switch (key)
                    {
                        case "BEGIN":
                            product = new Product();
                            break;
                        case "END":
                            // todo assert validity

                            // find physical location of product
                            // first try to search with exact lavakoodi
                            var pallets = (from p in storage.Pallets where p.PalletCode == product.PalletCode select p);
                            if (pallets.Count() == 0)
                            {
                                pallets = (from p in storage.Pallets where p.ShelfCode == product.ShelfCode select p);
                            }
                            product.BoundingBox = new BoundingBox();
                            if (pallets.Count() != 0) // muuten huonompi juttu
                            {
                                product.BoundingBox = pallets.ToList()[0].BoundingBox;
                            }
                            storage.AddProduct(product);
                            break;
                        case "NAME":
                            product.Description = value;
                            break;
                        case "PRODUCTCODE":
                            product.Code = value;
                            break;
                        case "SHELFCODE":
                            product.ShelfCode = value;
                            break;
                        case "PALLETCODE":
                            product.PalletCode = value;
                            break;
                        case "NOTES":
                            product.ExtraNotes = value;
                            break;
                        case "AMOUNT":
                            product.Amount = int.Parse(value);
                            break;
                        case "PRODUCTIONDATE":
                            product.ProductionDate = DateTime.Parse(value, fin);
                            break;
                        case "PACKAGESIZE":
                            product.PackageSize = int.Parse(value);
                            break;
                    }
                }
            }
        }
        string[] WriteProductsToTextFile(Dictionary<int,Product> products)
        {
            List<string> lines = new List<string>(10 * products.Count);
            foreach (var p in products.Values)
            {
                lines.Add("#BEGIN");
                lines.Add("#NAME=" + p.Description);
                lines.Add("#PRODUCTCODE=" + p.Code);
                lines.Add("#SHELFCODE=" + p.ShelfCode);
                lines.Add("#PALLETCODE=" + p.PalletCode);
                lines.Add("#NOTES=" + p.ExtraNotes);
                lines.Add("#AMOUNT=" + p.Amount.ToString());
                lines.Add("#PRODUCTIONDATE=" + p.ProductionDate.ToString(fin));
                lines.Add("#PACKAGESIZE=" + p.PackageSize.ToString());
                lines.Add("#END");
            }
            return lines.ToArray();
        }
        static bool ParseLine(string line, out string key, out string value)
        {
            key = null;
            value = null;
            line = line.Trim();

            if (!line.StartsWith("#")) return false;

            line = line.Remove(0, 1); // remove #
            var parts = line.Split('=');
            if (parts.Length == 0) return false;
            key = parts[0];
            if (parts.Length > 1)
                value = parts[1];
            return true;
        }
        void ReadOrdersFromTextFile(string[] textLines)
        {
            Order order = new Order();

            foreach (string line in textLines)
            {
                string key, value;
                if (ParseLine(line, out key, out value))
                {
                    switch (key)
                    {
                        case "BEGIN_ORDER":
                            order = new Order();
                            break;
                        case "END_ORDER":
                            // todo assert validity
                            OrderManager.Add(order);
                            break;
                        case "CUSTOMER":
                            order.Customer = value;
                            break;
                        case "SHIPPINGDATE":
                            order.RequestedShippingDate = DateTime.Parse(value, fin);
                            break;
                        case "LINE":
                            if (order.Lines == null)
                                order.Lines = new List<OrderLine>();
                            var parts = value.Split(' ');
                            order.Lines.Add(new OrderLine { ProductCode = parts[0], Amount = int.Parse(parts[1]) });
                            break;
                        case "STATE":
                            order.State = uint.Parse(value);
                            break;
                    }
                }
            }
        }
        string[] WriteOrdersToTextFile(List<Order> orders)
        {
            List<string> lines = new List<string>(10 * orders.Count);
            foreach (Order o in orders)
            {
                lines.Add("#BEGIN_ORDER");
                lines.Add("#CUSTOMER=" + o.Customer);
                lines.Add("#SHIPPINGDATE=" + o.RequestedShippingDate.ToString(fin));
                lines.Add("#STATE=" + o.State.ToString());
                foreach (var ln in o.Lines)
                {
                    lines.Add("#LINE=" + ln.ProductCode + " " + ln.Amount.ToString());
                }
                lines.Add("#END_ORDER");
            }
            return lines.ToArray();
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
                    int productKey = Storage.FindNearestToCollect(line.ProductCode, line.Amount, currentLocation);

                    // esteetön sijanti josta tuotetta voidaan kerätä, ts. varastopaikan vieressä
                    Point collectingLocation = Storage.Map.FindCollectingPoint(Storage.GetProduct(productKey).BoundingBox);

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
        public int CurrentProductKey;
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

        // samat eri koordinaateissa
        public Point CurrentLocation_AStar;
        public Point CurrentDestination_AStar;
        public Point DropOffPoint_AStar;

        public void CollectCurrentLine()
        {
            CurrentLine.State = STATE.COLLECTED;
            App.Instance.Storage.Collect(CurrentProductKey, CurrentLine.Amount);
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
            CurrentProductKey = App.Instance.Storage.FindNearestToCollect(line.ProductCode, line.Amount, CurrentLocation_AStar);
            CurrentDestination_AStar = App.Instance.Storage.Map.FindCollectingPoint(App.Instance.Storage.GetProduct(CurrentProductKey).BoundingBox);
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
