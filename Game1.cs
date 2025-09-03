using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Optional;

namespace Programming;

static class Config
{
    public const int SQUARE_SIZE = 70;
    public const int SCR_WID = 14;
    public const int SCR_HEI = 12;

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
    Texture2D closed;
    Texture2D open_top;
    Texture2D open_top_left;
    Texture2D open_top_bottom;
    Texture2D open_top_left_bottom;
    Texture2D open;
    

    public Block(ContentManager content, List<Point> points) : base(points)
    {
        closed = content.Load<Texture2D>("block/closed");
        open_top = content.Load<Texture2D>("block/open_top");
        open_top_left = content.Load<Texture2D>("block/open_top_left");
        open_top_bottom = content.Load<Texture2D>("block/open_top_bottom");
        open_top_left_bottom = content.Load<Texture2D>("block/open_top_left_bottom");
        open = content.Load<Texture2D>("block/open");
    }
    public override void Draw(SpriteBatch g)
    {
        foreach (Point point in points)
        {
            var directions = new[] { Direction.Left, Direction.Right, Direction.Down, Direction.Up };
            var bits = directions.Select(d => d.OffsetPoint(point) is Point p && points.Contains(p)).ToList();
            int pattern = bits.Select((b, i) => b ? 1 << i : 0).Sum();

            // up down right left
            var texture_rot = pattern switch
            {
                0b0000 => (closed, 0),
                0b1000 => (open_top, 0),
                0b0100 => (open_top, 2),
                0b0010 => (open_top, 3),
                0b0001 => (open_top, 1),
                0b1100 => (open_top_bottom, 0),
                0b0011 => (open_top_bottom, 1),
                0b1010 => (open_top_left, 3),
                0b1001 => (open_top_left, 0),
                0b0110 => (open_top_left, 2),
                0b0101 => (open_top_left, 1),
                0b1110 => (open_top_left_bottom, 2),
                0b1101 => (open_top_left_bottom, 0),
                0b1011 => (open_top_left_bottom, 3),
                0b0111 => (open_top_left_bottom, 1),
                0b1111 => (open, 0),
                _ => throw new UnreachableException(),
            };


            Texture2D texture = texture_rot.Item1;
            int rot = texture_rot.Item2;


            // Use position + scale instead of Rectangle
            Vector2 position = new Vector2(
                point.X * Config.SQUARE_SIZE + Config.SQUARE_SIZE/2,  // Center of square
                point.Y * Config.SQUARE_SIZE + Config.SQUARE_SIZE/2
            );
            
            Vector2 scale = new Vector2(
                (float)Config.SQUARE_SIZE / texture.Width,   // Scale to fit square
                (float)Config.SQUARE_SIZE / texture.Height
            );
            
            g.Draw(
                texture,
                // new Rectangle(point.X * Config.SQUARE_SIZE, point.Y * Config.SQUARE_SIZE, Config.SQUARE_SIZE, Config.SQUARE_SIZE),
                position,
                null,
                Color.White,
                MathHelper.ToRadians(-rot * 90),
                new Vector2(texture.Width/2, texture.Height/2),
                scale,
                SpriteEffects.None,
                0f
            );
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
        sprites = new List<Sprite> {
            character,
            new Block(Content, new List<Point> {
                new Point(4, 3),
                new Point(4, 4),
                new Point(5, 4)
            }),
            new Block(Content, new List<Point> {
                new Point(7, 2),
                new Point(7, 3)
            }),

            new Block(Content, new List<Point> {
                new Point(6, 7),
                new Point(7, 7),
                new Point(8, 7),
                new Point(8, 8),
                new Point(8, 9),
                new Point(9, 9),
                new Point(10, 9),
                new Point(11, 9),
                new Point(11, 8),
                new Point(11, 7),
            })
        };
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