using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using LeagueSharp;

namespace Notifications
{
    internal class Memory
    {
        /// <summary>
        ///     Map Name
        /// </summary>
        private const string MappedName = "notificationmap2d6daa0b-e6fe-4e89-85c4-1c6389ee7e1d";

        /// <summary>
        ///     Mutex Map Name
        /// </summary>
        private const string MappedMutexName = "mutexnotificationmap2d6daa0b-e6fe-4e89-85c4-1c6389ee7e1d";

        /// <summary>
        ///     MMF instance
        /// </summary>
        public static MemoryMappedFile MemoryMappedFile;

        /// <summary>
        ///     Mutex Instance
        /// </summary>
        public static Mutex Mutex;

        /// <summary>
        ///     Static Memory (MMF creator/loader)
        /// </summary>
        static Memory()
        {
            try
            {
                MemoryMappedFile = MemoryMappedFile.OpenExisting(MappedName);
                Mutex = Mutex.OpenExisting(MappedMutexName);
            }
            catch (Exception)
            {
                MemoryMappedFile = MemoryMappedFile.CreateNew(
                    MappedName, (Drawing.Height / 25) * Marshal.SizeOf(typeof(int)) * 2);
                Mutex = new Mutex(true, MappedMutexName);
                Write(80);
            }
        }

        /// <summary>
        ///     Write to Memory
        /// </summary>
        /// <param name="data">Data to Write</param>
        public static void Write(int data)
        {
            MemoryMappedFile = MemoryMappedFile.OpenExisting(MappedName);
            Mutex = Mutex.OpenExisting(MappedMutexName);
            using (var mmf = MemoryMappedFile)
            {
                var mutex = Mutex;
                mutex.WaitOne();

                using (var stream = mmf.CreateViewStream())
                {
                    var writer = new BinaryWriter(stream);
                    writer.Write(data);
                }

                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///     Read from Memory
        /// </summary>
        /// <returns>Read Data</returns>
        public static int Read()
        {
            MemoryMappedFile = MemoryMappedFile.OpenExisting(MappedName);
            Mutex = Mutex.OpenExisting(MappedMutexName);
            using (var mmf = MemoryMappedFile)
            {
                var mutex = Mutex;
                mutex.WaitOne();

                int returnValue;
                using (var stream = mmf.CreateViewStream())
                {
                    var reader = new BinaryReader(stream);
                    returnValue = reader.ReadInt16();
                }

                mutex.ReleaseMutex();
                return returnValue;
            }
        }
    }
}