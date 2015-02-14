#region

using System;
using System.IO;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace Notifications
{
    /// <summary>
    ///     Basic Notification
    /// </summary>
    public class Notification : INotification
    {
        #region Other

        public enum NotificationState
        {
            Idle,
            AnimationMove,
            AnimationShowUpdate
        }

        #endregion

        /// <summary>
        ///     Notification Constructor
        /// </summary>
        /// <param name="text">Display Text</param>
        /// <param name="duration">Duration (-1 for Infinite)</param>
        public Notification(string text, int duration = -0x1)
        {
            // Setting GUID
            id = Guid.NewGuid().ToString("N");

            // Setting main values
            Text = text;
            state = NotificationState.Idle;

            // Calling Show
            Show(duration);
        }

        #region Functions

        /// <summary>
        ///     Show an inactive Notification, returns boolean if successful or not.
        /// </summary>
        /// <param name="newDuration">Duration (-1 for Infinite)</param>
        /// <returns></returns>
        public bool Show(int newDuration = -0x1)
        {
            if (draw || update)
            {
                // TODO: Beaving's fancy animation.
                return false;
            }

            var yAxis = Notifications.GetLocation();
            if (yAxis != -0x1)
            {
                handler = Notifications.Reserve(GetId());
                if (handler != null)
                {
                    duration = newDuration;

                    TextColor.A = 0xff;
                    BoxColor.A = 0xff;
                    BorderColor.A = 0xff;

                    position = new Vector2(Drawing.Width - 200f, yAxis);

                    decreasementTick = GetNextDecreasementTick();

                    draw = update = true;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Calculate the next decreasement tick.
        /// </summary>
        /// <returns>Decreasement Tick</returns>
        private int GetNextDecreasementTick()
        {
            return Environment.TickCount + ((duration / 0xff));
        }

        /// <summary>
        ///     Calculate the border into vertices
        /// </summary>
        /// <param name="x">X axis</param>
        /// <param name="y">Y axis</param>
        /// <param name="w">Width</param>
        /// <param name="h">Height</param>
        /// <returns>Vector2 Array</returns>
        private static Vector2[] GetBorder(float x, float y, float w, float h)
        {
            return new[] { new Vector2(x + w / 0x2, y), new Vector2(x + w / 0x2, y + h) };
        }

        #endregion

        #region Public Fields

        /// <summary>
        ///     Notification's Text
        /// </summary>
        public string Text;

        #region Colors

        /// <summary>
        ///     Notification's Text Color
        /// </summary>
        public ColorBGRA TextColor = new ColorBGRA(255f, 255f, 255f, 255f);

        /// <summary>
        ///     Notification's Box Color
        /// </summary>
        public ColorBGRA BoxColor = new ColorBGRA(0f, 0f, 0f, 255f);

        /// <summary>
        ///     Notification's Border Color
        /// </summary>
        public ColorBGRA BorderColor = new ColorBGRA(255f, 255f, 255f, 255f);

        /// <summary>
        ///     Notification's Font
        /// </summary>
        public Font Font = new Font(
            Drawing.Direct3DDevice, 0xe, 0x0, FontWeight.Bold, 0x0, false, FontCharacterSet.Default,
            FontPrecision.Default, FontQuality.Antialiased, FontPitchAndFamily.DontCare | FontPitchAndFamily.Decorative,
            "Tahoma");

        #endregion

        #endregion

        #region Private Fields

        /// <summary>
        ///     Locally saved Global Unique Identification (GUID)
        /// </summary>
        private readonly string id;

        /// <summary>
        ///     Locally saved Notification's Duration
        /// </summary>
        private int duration;

        /// <summary>
        ///     Locally saved bool, indicating if OnDraw should be executed/processed.
        /// </summary>
        private bool draw;

        /// <summary>
        ///     Locally saved bool, indicating if OnUpdate should be executed/processed.
        /// </summary>
        private bool update;

        /// <summary>
        ///     Locally saved handler for FileStream.
        /// </summary>
        private FileStream handler;

        /// <summary>
        ///     Locally saved position
        /// </summary>
        private Vector2 position;

        /// <summary>
        ///     Locally saved update position
        /// </summary>
        private Vector2 updatePosition;

        /// <summary>
        ///     Locally saved Notification State
        /// </summary>
        private NotificationState state;

        /// <summary>
        ///     Locally saved value, indicating when next decreasment tick should happen.
        /// </summary>
        private int decreasementTick;

        /// <summary>
        ///     Locally saved Line
        /// </summary>
        private readonly Line line = new Line(Drawing.Direct3DDevice);

        /// <summary>
        ///     Locally saved Sprite
        /// </summary>
        private readonly Sprite sprite = new Sprite(Drawing.Direct3DDevice);

        #endregion

        #region Required Functions

        /// <summary>
        ///     Called for Drawing onto screen
        /// </summary>
        public void OnDraw()
        {
            if (!draw)
            {
                return;
            }

            #region Box

            line.Begin();
            var vertices = new[]
            { new Vector2(position.X + 190f / 0x2, position.Y), new Vector2(position.X + 190f / 0x2, position.Y + 25f) };
            line.Draw(vertices, BoxColor);
            line.End();

            #endregion

            #region Outline

            var x = position.X;
            var y = position.Y;
            const float w = 190f;
            const float h = 25f;
            const float px = 1f;

            line.Begin();
            line.Draw(GetBorder(x, y, w, px), BorderColor); // TOP
            line.End();

            var oWidth = line.Width;
            line.Width = px;

            line.Begin();
            line.Draw(GetBorder(x, y, px, h), BorderColor); // LEFT
            line.Draw(GetBorder(x + w, y, 1, h), BorderColor); // RIGHT
            line.End();

            line.Width = oWidth;

            line.Begin();
            line.Draw(GetBorder(x, y + h, w, 1), BorderColor); // BOTTOM
            line.End();

            #endregion

            #region Text

            sprite.Begin();

            var textDimension = Font.MeasureText(sprite, Text, 0x0);
            var rectangle = new Rectangle((int) position.X, (int) position.Y, 0xbe, 0x19);

            Font.DrawText(
                sprite, Text, rectangle.TopLeft.X + (rectangle.Width - textDimension.Width) / 0x2,
                rectangle.TopLeft.Y + (rectangle.Height - textDimension.Height) / 0x2, TextColor);

            sprite.End();

            #endregion
        }

        /// <summary>
        ///     Called per game tick for update
        /// </summary>
        public void OnUpdate()
        {
            if (!update)
            {
                return;
            }

            switch (state)
            {
                case NotificationState.Idle:
                {
                    #region Duration End Handler

                    if (duration > 0x0 && TextColor.A == 0x0 && BoxColor.A == 0x0 && BorderColor.A == 0x0)
                    {
                        update = false;
                        draw = false;
                        Notifications.Free(handler);
                        return;
                    }

                    #endregion

                    #region Decreasement Tick

                    if (duration > 0x0 && Environment.TickCount - decreasementTick > 0x0)
                    {
                        if (TextColor.A > 0x0)
                        {
                            TextColor.A--;
                        }
                        if (BoxColor.A > 0x0)
                        {
                            BoxColor.A--;
                        }
                        if (BorderColor.A > 0x0)
                        {
                            BorderColor.A--;
                        }

                        decreasementTick = GetNextDecreasementTick();
                    }

                    #endregion

                    #region Movement

                    var location = Notifications.GetLocation();
                    if (location != -0x1 && position.Y > location)
                    {
                        if (Notifications.IsFirst((int) position.Y))
                        {
                            var b = Notifications.Reserve(GetId());
                            if (b != null)
                            {
                                Notifications.Free(handler);
                                handler = b;
                                updatePosition = new Vector2(position.X, location);
                                state = NotificationState.AnimationMove;
                            }
                        }
                    }

                    #endregion

                    break;
                }
                case NotificationState.AnimationMove:
                {
                    #region Movement

                    if (Math.Abs(position.Y - updatePosition.Y) > float.Epsilon)
                    {
                        var value = (updatePosition.Distance(new Vector2(position.X, position.Y - 0x1)) <
                                     updatePosition.Distance(new Vector2(position.X, position.Y + 0x1)))
                            ? -0x1
                            : 0x1;
                        position.Y += value;
                    }
                    else
                    {
                        updatePosition = Vector2.Zero;
                        state = NotificationState.Idle;
                    }

                    #endregion

                    break;
                }
            }
        }

        /// <summary>
        ///     Returns the notification's global unique identification (GUID)
        /// </summary>
        /// <returns>GUID</returns>
        public string GetId()
        {
            return id;
        }

        #endregion
    }
}