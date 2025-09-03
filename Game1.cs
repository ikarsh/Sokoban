using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Optional;

namespace Programming;

static class Config
{
    public const int SQUARE_SIZE = 70;
    public const int SCR_WID = 10;
    public const int SCR_HEI = 8;

    public const double MOVE_DELAY_MS = 30;
}

static class Utils
{
    public static bool InBoard(Point point)
    {
        return point.X >= 0 && point.X <= Config.SCR_WID - 1 && point.Y >= 0 && point.Y <= Config.SCR_HEI - 1;
    }
}

public enum Direction { Up, Down, Right, Left }

static class DirectionExtensions
{
    public static Point? OffsetPoint(this Direction d, Point p)
    {
        Point q = d switch
        {
            Direction.Up => new Point(p.X, p.Y - 1),
            Direction.Down => new Point(p.X, p.Y + 1),
            Direction.Right => new Point(p.X + 1, p.Y),
            Direction.Left => new Point(p.X - 1, p.Y),
            _ => throw new UnreachableException()
        };
        if (q.X >= 0 && q.Y >= 0 && q.X < Config.SCR_WID && q.Y < Config.SCR_HEI)
        {
            return q;
        }
        return null;
    }

    public static Direction? FromKeyboard()
    {
        KeyboardState state = Keyboard.GetState();

        if (state.IsKeyDown(Keys.Down)) return Direction.Down;
        else if (state.IsKeyDown(Keys.Up)) return Direction.Up;
        else if (state.IsKeyDown(Keys.Right)) return Direction.Right;
        else if (state.IsKeyDown(Keys.Left)) return Direction.Left;
        return null;
    }
}

public abstract class Sprite
{
    protected List<Point> points;
    private bool _marked = false;
    public abstract void Draw(SpriteBatch g);

    public Sprite(List<Point> points)
    {
        this.points = points;
    }

    public void Push(Game1 g, Direction d)
    {
        foreach (Sprite sprite in g.sprites)
        {
            sprite._marked = false;
        }

        _Push(g, d);

        foreach (Sprite sprite in g.sprites)
        {
            sprite._marked = false;
        }
    }

    public bool _Push(Game1 g, Direction d)
    {
        _marked = true;
        List<Point> pushedPoints = new List<Point>();
        foreach (Point point in points)
        {
            Point? pushed = d.OffsetPoint(point);
            if (pushed is Point p) pushedPoints.Add(p);
            else return false;
        }
        List<Point> old = points;
        points = pushedPoints;

        foreach (Sprite sprite in g.sprites)
        {
            if (sprite._marked) continue;
            if (sprite.points.Intersect(points).Count() > 0)
            {
                bool res = sprite._Push(g, d);
                if (!res)
                {
                    points = old;
                    return false;
                }
            }
        }
        return true;
    }
}

class Character : Sprite
{
    Texture2D texture;
    public Character(ContentManager content, Point position) : base(new List<Point> { position })
    {
        if (!Utils.InBoard(position))
        {
            throw new ArgumentException();
        }
        texture = content.Load<Texture2D>("allyourbase"); 
    }

    public override void Draw(SpriteBatch g)
    {
        Point position = points[0];
        g.Draw(texture, new Rectangle(position.X * Config.SQUARE_SIZE, position.Y * Config.SQUARE_SIZE, Config.SQUARE_SIZE, Config.SQUARE_SIZE), Color.White);
    }
}

class Block : Sprite
{
    Texture2D texture;

    public Block(ContentManager content, List<Point> points) : base(points)
    {
        texture = content.Load<Texture2D>("allyourbase");
    }
    public override void Draw(SpriteBatch g)
    {
        foreach (Point point in points)
        {
            g.Draw(texture, new Rectangle(point.X * Config.SQUARE_SIZE, point.Y * Config.SQUARE_SIZE, Config.SQUARE_SIZE, Config.SQUARE_SIZE), Color.White);
        }
    }

}

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    public List<Sprite> sprites;

    double lastMoveTime = 0;

    Sprite character;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = Config.SQUARE_SIZE * Config.SCR_WID;
        _graphics.PreferredBackBufferHeight = Config.SQUARE_SIZE * Config.SCR_HEI;
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        sprites = new List<Sprite>();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
        character = new Character(Content, new Point(0, 0));
        sprites.Add(character);
        sprites.Add(new Block(Content, new List<Point>
        {
            new Point(4 , 3),
            new Point(4 , 4 ),
            new Point(5 , 4 )
        }));
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here
        if (DirectionExtensions.FromKeyboard() is Direction d)
        {
            // Exit();
            double time = gameTime.TotalGameTime.TotalMilliseconds;
            if (time - lastMoveTime > Config.MOVE_DELAY_MS) character.Push(this, d);
            lastMoveTime = time;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO: Add your drawing code here
        _spriteBatch.Begin();
        foreach (Sprite sprite in sprites)
        {
            sprite.Draw(_spriteBatch);
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}