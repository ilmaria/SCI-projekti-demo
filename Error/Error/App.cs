using System;
using System.Collections.Generic;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System.Linq;

//ilmari : ui, datan tuonti
//henri: varaston tietorakenne, järjestyksen optimointi

// HUOMIO !!!!
/*
    Ilmari ks ReadWareHouseData(), ReadOrders(), class DataBaseEntry
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

        Map map = null;
        Color[] mapColors;
        Texture2D mapTexture;
        List<Point> path;
        Point start = new Point(int.MinValue, int.MinValue);
        Point goal = new Point(int.MinValue, int.MinValue);
        SamplerState pointSampler;
        AStar pathFinder;


        DataBase _productDataBase;
        List<Order> _orders;


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
            // TODO: Add your initialization logic here
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
            // TODO: use this.Content to load your game content here
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
            // TODO: Add your update logic here

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

                                    _productDataBase = ReadWareHouseData();
                                    _orders = ReadOrders();
                                    OptimizeOrders(_orders);
                                    CreateOrUpdateAStarMap(ref map, _productDataBase);
                                    //map = CreateMap();


                                    while (true)
                                    {
                                        start = new Point(random.Next(map.SizeX), random.Next(map.SizeY));
                                        goal = new Point(random.Next(map.SizeX), random.Next(map.SizeY));
                                        if (!map[start.X, start.Y].IsTraversable) continue;
                                        if (!map[goal.X, goal.Y].IsTraversable) continue;
                                        if (map.Contains(start) && map.Contains(goal) && start != goal) break;
                                    }
                                    pathFinder = new AStar(map);
                                    path = pathFinder.FindPath(start, goal);
                                    UpdateMapTexture();
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
        void UpdateMapTexture()
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





        DataBase ReadWareHouseData()
        {
            // esimerkki ja testausta varten
            DataBase db = new DataBase(100);
            for (int i = 0; i < 500; i++)
            {
                var entry = new DataBaseEntry();
                entry.ProductCode = i.ToString();
                entry.ProductDescription = "ruuvi";
                entry.ShelfCode = "1005"; // hyllypaikka, jossa monta lavaa
                entry.Amount = 20000;

                float x = random.Next(64);
                float y = random.Next(64);
                float z = 1f; // metrin korkeudella
                float width = random.Next(4);// yleensä eurolavan koko
                float height = 1f;
                entry.BoundingBox = new BoundingBox(new Vector3(x, y, z), new Vector3(x + width, y + width, z + height));

                db.Add(entry);
            }
            return db;

            // .xml?
        }
        List<Order> ReadOrders()
        {
            List<Order> orders = new List<Order>(1);

            Order order = new Order();
            order.Products = new List<Product>(2);
            order.Products.Add(new Product { Code = "27", Amount = 473 });
            order.Products.Add(new Product { Code = "35", Amount = 3473 });
            orders.Add(order);

            return orders;
        }
        void OptimizeOrders(List<Order> orders)
        {

        }
        void CreateOrUpdateAStarMap(ref Map m, DataBase db)
        {
            // oletetaan xmin, ymin = 0 ja 1 metrin resoluutio...
            int preferredSizeX = (int)db.Items.Max(item => item.BoundingBox.Max.X);
            int preferredSizeY = (int)db.Items.Max(item => item.BoundingBox.Max.Y);

            if (m == null || m.SizeX != preferredSizeX || m.SizeY != preferredSizeY)
            {
                m = new Map(preferredSizeX, preferredSizeY);
            }

            for (int x = 0; x < m.SizeX; x++)
            {
                for (int y = 0; y < m.SizeY; y++)
                {
                    MapNode mapNode = new MapNode();
                    mapNode.IsTraversable = true;
                    m[x, y] = mapNode;
                }
            }

            // TODO
            // asioiden ali voi päästä, asiat voivat olla eri kerroksissa
            // --> float floorLevel
            // lisäksi esteet, jotka eivät tuotteita

            foreach (DataBaseEntry entry in db.Items)
            {
                BoundingBox b = entry.BoundingBox;

                for (int x = (int)b.Min.X; x < (int)b.Max.X; x++)
                {
                    for (int y = (int)b.Min.Y; y < (int)b.Max.Y; y++)
                    {
                        if (m.Contains(new Point(x, y)))
                        {
                            var node = m[x, y];
                            node.IsTraversable = false;
                            m[x, y] = node;
                        }
                    }
                }
            }
        }
    }

    public enum Screen
    {
        StartScreen,
        NavigationScreen
    }
}
