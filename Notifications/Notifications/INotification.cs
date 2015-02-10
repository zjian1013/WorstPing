using System;

namespace Notifications
{
    /// <summary>
    ///     INotification :: Dynamic Notifications
    /// </summary>
    public interface INotification : IDisposable, ICloneable
    {
        /// <summary>
        ///     IsValid
        /// </summary>
        bool IsValid { get; set; }

        /// <summary>
        ///     Drawing
        /// </summary>
        void Draw();
    }
}