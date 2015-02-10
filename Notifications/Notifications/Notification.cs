using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LeagueSharp;
using SharpDX;
using SharpDX.Direct3D9;

namespace Notifications
{
    /// <summary>
    ///     Notification Class
    /// </summary>
    public class Notification
    {
        private static readonly List<INotification> Notifications = new List<INotification>();

        internal static readonly Font Font = new Font(
            Drawing.Direct3DDevice, 0xE, 0x0, FontWeight.DoNotCare, 0x0, false, FontCharacterSet.Default,
            FontPrecision.Default, FontQuality.Antialiased, FontPitchAndFamily.DontCare | FontPitchAndFamily.Decorative,
            "Tahoma");

        /// <summary>
        ///     Notification static
        /// </summary>
        static Notification()
        {
            Drawing.OnDraw += args =>
            {
                if (Notifications.Count == 0)
                {
                    return;
                }

                foreach (var n in Notifications)
                {
                    if (!n.IsValid)
                    {
                        n.Dispose();
                        Notifications.Remove(n);
                        break;
                    }
                    n.Draw();
                }
            };
        }

        /// <summary>
        ///     Adding a new notification into the notification center
        /// </summary>
        /// <param name="notification">Notification Instance</param>
        public static void AddNotification(INotification notification)
        {
            if (notification != null && notification.IsValid)
            {
                Notifications.Add(notification);
            }
        }

        /// <summary>
        ///     Removing an existing notification.
        /// </summary>
        /// <param name="notification">Notification Instance</param>
        public static void RemoveNotification(INotification notification)
        {
            if (notification != null && notification.IsValid)
            {
                notification.Dispose();
            }
        }
    }

    #region ToastNotification

    public class ToastNotification : INotification
    {
        /// <summary>
        ///     Toast Notification Constructor
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="textColor">Text Color</param>
        /// <param name="duration">Duration</param>
        public ToastNotification(string text, ColorBGRA textColor, int duration)
        {
            TextColor = textColor;
            Position = new Vector2(Drawing.Width - 200f, Memory.Read());
            ToastColor = new ColorBGRA(0f, 0f, 0f, 255f);
            Duration = duration;
            EndTime = Environment.TickCount + duration + 1500;

            if (text.Length > 26)
            {
                var regex = new Regex(@".{26}");
                var result = regex.Matches(text);
                Text = (from object r in result select r.ToString()).ToArray();
            }
            Text = new[] { text };

            Width = 190f;
            Height = 25f * Text.ToList().Count;
            Line = new Line(Drawing.Direct3DDevice) { Width = Width, GLLines = true, Antialias = false };
            if (duration > 0)
            {
                NextTickAnim = Environment.TickCount + duration / 255;
                Fadeable = true;
            }

            IsValid = true;

            Memory.Write((int) (Position.Y + 30 * Text.ToList().Count));
        }

        /// <summary>
        ///     Notification Text
        /// </summary>
        public string[] Text { get; set; }

        /// <summary>
        ///     Notification Text Color
        /// </summary>
        public ColorBGRA TextColor { get; set; }

        /// <summary>
        ///     Notification Position
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        ///     Notification Background Color
        /// </summary>
        public ColorBGRA ToastColor { get; set; }

        /// <summary>
        ///     Notification Duration
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        ///     Notification EndTime
        /// </summary>
        public int EndTime { get; set; }

        /// <summary>
        ///     Box Width
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        ///     Box Height
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        ///     Line
        /// </summary>
        public Line Line { get; set; }

        /// <summary>
        ///     Next Tick to decrease A value
        /// </summary>
        private int NextTickAnim { get; set; }

        /// <summary>
        ///     Is a fadeable toast
        /// </summary>
        public bool Fadeable { get; set; }

        /// <summary>
        ///     Dispose function
        /// </summary>
        public void Dispose()
        {
            Memory.Write((int) (Position.Y - 30 * Text.ToList().Count));

            Text = null;
            TextColor = new ColorBGRA();
            Position = Vector2.Zero;
            ToastColor = new ColorBGRA();
            Duration = 0;
            EndTime = 0;
            Width = 0f;
            Height = 0f;
            Line.Dispose();
            NextTickAnim = 0;
            Fadeable = false;
            IsValid = false;
        }

        /// <summary>
        ///     Clone function
        /// </summary>
        /// <returns>Cloned ToastNotification</returns>
        public object Clone()
        {
            var str = Text[0];
            foreach (var st in Text)
            {
                str += st;
                str += ' ';
            }

            return new ToastNotification(str, TextColor, Duration);
        }

        /// <summary>
        ///     Drawing Function
        /// </summary>
        public void Draw()
        {
            if (!IsValid)
            {
                return;
            }

            if (Fadeable && Environment.TickCount - EndTime > 0)
            {
                Dispose();
                return;
            }

            Line.Begin();
            var vertices = new[]
            {
                new Vector2(Position.X + Width / 2, Position.Y), new Vector2(Position.X + Width / 2, Position.Y + Height)
            };
            Line.Draw(vertices, ToastColor);
            Line.End();

            var sprite = new Sprite(Drawing.Direct3DDevice);
            sprite.Begin();
            for (var i = 0; i < Text.ToList().Count; ++i)
            {
                Notification.Font.DrawText(
                    sprite, Text[i], (int) (Position.X + 6f),
                    (int) (Position.Y + 6f + (Height / Text.ToList().Count) * i), TextColor);
            }
            sprite.End();
            sprite.Dispose();

            if (Fadeable && Environment.TickCount - NextTickAnim > 0)
            {
                if (TextColor.A > 0)
                {
                    var color = TextColor;
                    color.A--;
                    TextColor = color;
                }
                if (ToastColor.A > 0)
                {
                    var color = ToastColor;
                    color.A--;
                    ToastColor = color;
                }
                NextTickAnim = Environment.TickCount + (Duration / 255);
            }
        }

        /// <summary>
        ///     IsValid
        /// </summary>
        public bool IsValid { get; set; }
    }

    #endregion

    #region AnimatedNotification

    public class AnimatedNotification : INotification
    {
        /// <summary>
        ///     Toast Notification Constructor
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="textColor">Text Color</param>
        /// <param name="duration">Duration</param>
        /// <param name="animationRate">Animation Speed</param>
        /// <param name="useFadeOut">Use Fade-Out animation</param>
        public AnimatedNotification(string text,
            ColorBGRA textColor,
            int duration,
            int animationRate = 1,
            bool useFadeOut = true)
        {
            TextColor = textColor;
            Position = new Vector2(Drawing.Width - 200f, Memory.Read());
            ToastColor = new ColorBGRA(0f, 0f, 0f, 255f);
            Duration = duration;
            EndTime = Environment.TickCount + duration + 1500;

            if (text.Length > 26)
            {
                var regex = new Regex(@".{26}");
                var result = regex.Matches(text);
                Text = (from object r in result select r.ToString()).ToArray();
            }
            Text = new[] { text };

            Width = 190f;
            Height = 25f * Text.ToList().Count;
            Line = new Line(Drawing.Direct3DDevice) { Width = Width, GLLines = true, Antialias = false };
            if (duration > 0)
            {
                NextTickAnim = Environment.TickCount + duration / 255;
                Fadeable = true;
            }
            AnimationRate = animationRate;
            IsUsingFadeOut = useFadeOut;

            IsValid = true;

            Memory.Write((int) (Position.Y + 30 * Text.ToList().Count));
        }

        /// <summary>
        ///     Notification Text
        /// </summary>
        public string[] Text { get; set; }

        /// <summary>
        ///     Notification Text Color
        /// </summary>
        public ColorBGRA TextColor { get; set; }

        /// <summary>
        ///     Notification Position
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        ///     Notification Background Color
        /// </summary>
        public ColorBGRA ToastColor { get; set; }

        /// <summary>
        ///     Notification Duration
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        ///     Notification EndTime
        /// </summary>
        public int EndTime { get; set; }

        /// <summary>
        ///     Box Width
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        ///     Box Height
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        ///     Line
        /// </summary>
        public Line Line { get; set; }

        /// <summary>
        ///     Next Tick to decrease A value
        /// </summary>
        private int NextTickAnim { get; set; }

        /// <summary>
        ///     Is a fadeable toast
        /// </summary>
        public bool Fadeable { get; set; }

        /// <summary>
        ///     Animation Stage
        /// </summary>
        public int AnimationStage { get; set; }

        /// <summary>
        ///     Animation Fade In Prefix
        /// </summary>
        public int AnimationPrefix { get; set; }

        /// <summary>
        ///     Animation Rate (Speed)
        /// </summary>
        public int AnimationRate { get; set; }

        /// <summary>
        ///     Is Using Fade Out
        /// </summary>
        public bool IsUsingFadeOut { get; set; }

        /// <summary>
        ///     Dispose function
        /// </summary>
        public void Dispose()
        {
            Memory.Write((int) (Position.Y - 30 * Text.ToList().Count));

            Text = null;
            TextColor = new ColorBGRA();
            Position = Vector2.Zero;
            ToastColor = new ColorBGRA();
            Duration = 0;
            EndTime = 0;
            Width = 0f;
            Height = 0f;
            Line.Dispose();
            NextTickAnim = 0;
            Fadeable = false;
            IsValid = false;
            AnimationStage = 0;
            AnimationPrefix = 0;
            AnimationRate = 0;
        }

        /// <summary>
        ///     Clone function
        /// </summary>
        /// <returns>Cloned ToastNotification</returns>
        public object Clone()
        {
            var str = Text[0];
            foreach (var st in Text)
            {
                str += st;
                str += ' ';
            }

            return new AnimatedNotification(str, TextColor, Duration, AnimationRate, IsUsingFadeOut);
        }

        /// <summary>
        ///     Drawing Function
        /// </summary>
        public void Draw()
        {
            if (!IsValid)
            {
                return;
            }

            #region Animation

            if (AnimationStage == 0)
            {
                /* FADE IN*/

                #region Animation Fade In

                Line.Begin();
                var aVertices = new[]
                {
                    new Vector2((Position.X + 200f - AnimationPrefix) + Width / 2, Position.Y),
                    new Vector2((Position.X + 200f - AnimationPrefix) + Width / 2, Position.Y + Height)
                };
                Line.Draw(aVertices, ToastColor);
                Line.End();

                AnimationPrefix += AnimationRate;

                var aSprite = new Sprite(Drawing.Direct3DDevice);
                aSprite.Begin();
                for (var i = 0; i < Text.ToList().Count; ++i)
                {
                    Notification.Font.DrawText(
                        aSprite, Text[i], (int) ((Position.X + 200f - AnimationPrefix) + 6f),
                        (int) (Position.Y + 6f + (Height / Text.ToList().Count) * i), TextColor);
                }
                aSprite.End();
                aSprite.Dispose();

                if (AnimationPrefix >= 200)
                {
                    if (AnimationPrefix > 200)
                    {
                        --AnimationPrefix;
                        return;
                    }

                    AnimationStage++;
                    if (Fadeable)
                    {
                        EndTime = Environment.TickCount + Duration;
                    }
                }

                #endregion

                return;
            }
            if (AnimationStage == 1 && Fadeable && Environment.TickCount - EndTime > 0 && IsUsingFadeOut)
            {
                /* FADE OUT */

                #region Animation Fade Out

                Line.Begin();
                var aVertices = new[]
                {
                    new Vector2((Position.X + 200f - AnimationPrefix) + Width / 2, Position.Y),
                    new Vector2((Position.X + 200f - AnimationPrefix) + Width / 2, Position.Y + Height)
                };
                Line.Draw(aVertices, ToastColor);
                Line.End();

                AnimationPrefix -= AnimationRate;

                var aSprite = new Sprite(Drawing.Direct3DDevice);
                aSprite.Begin();
                for (var i = 0; i < Text.ToList().Count; ++i)
                {
                    Notification.Font.DrawText(
                        aSprite, Text[i], (int) ((Position.X + 200f - AnimationPrefix) + 6f),
                        (int) (Position.Y + 6f + (Height / Text.ToList().Count) * i), TextColor);
                }
                aSprite.End();
                aSprite.Dispose();

                if (AnimationPrefix <= 0)
                {
                    Dispose();
                }

                #endregion

                return;
            }

            #endregion

            if (Fadeable && Environment.TickCount - EndTime > 0 && !IsUsingFadeOut)
            {
                Dispose();
                return;
            }

            Line.Begin();
            var vertices = new[]
            {
                new Vector2(Position.X + Width / 2, Position.Y), new Vector2(Position.X + Width / 2, Position.Y + Height)
            };
            Line.Draw(vertices, ToastColor);
            Line.End();

            var sprite = new Sprite(Drawing.Direct3DDevice);
            sprite.Begin();
            for (var i = 0; i < Text.ToList().Count; ++i)
            {
                Notification.Font.DrawText(
                    sprite, Text[i], (int) (Position.X + 6f),
                    (int) (Position.Y + 6f + (Height / Text.ToList().Count) * i), TextColor);
            }
            sprite.End();
            sprite.Dispose();

            if (Fadeable && Environment.TickCount - NextTickAnim > 0 && !IsUsingFadeOut)
            {
                if (TextColor.A > 0)
                {
                    var color = TextColor;
                    color.A--;
                    TextColor = color;
                }
                if (ToastColor.A > 0)
                {
                    var color = ToastColor;
                    color.A--;
                    ToastColor = color;
                }
                NextTickAnim = Environment.TickCount + (Duration / 255);
            }
        }

        /// <summary>
        ///     IsValid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        ///     Removal of the Notification
        /// </summary>
        public void Remove()
        {
            Dispose();
        }
    }

    #endregion
}