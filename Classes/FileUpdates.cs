using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Tiels.Classes
{
    class FileUpdates
    {
        protected static Dictionary<string, ChangedType> fileupdates = new Dictionary<string, ChangedType>();
        private static int nextid = 0;

        public static int NextID()
        {
            return nextid++;
        }

        protected enum ChangedType
        {
            CREATED,
            CHANGED,
            DELETED
        }

        public static void CreateEntry(string entry)
        {
            fileupdates.Add(entry, ChangedType.CREATED);
            /*foreach (var fileupdate in fileupdates.Keys)
            {
                Console.WriteLine(fileupdate + " + " + fileupdates[fileupdate]);
            }*/
        }

        public static void ChangeEntry(string entry)
        {
            fileupdates.Add(entry, ChangedType.CHANGED);
            /*foreach (var fileupdate in fileupdates.Keys)
            {
                Console.WriteLine(fileupdate + " + " + fileupdates[fileupdate]);
            }*/
        }

        public static void DeleteEntry(string entry)
        {
            fileupdates.Add(entry, ChangedType.DELETED);
            /*foreach (var fileupdate in fileupdates.Keys)
            {
                Console.WriteLine(fileupdate + " + " + fileupdates[fileupdate]);
            }*/
        }

        public async Task UpdateEntry()
        {
            while (true)
            {
                await Task.Delay(5000);
                MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                if (!mw.isLoading)
                {
                    foreach (var fileupdate in fileupdates.Keys)
                    {
                        Console.WriteLine(fileupdate + " + " + fileupdates[fileupdate]);
                    }
                }
            }
        }
    }
}