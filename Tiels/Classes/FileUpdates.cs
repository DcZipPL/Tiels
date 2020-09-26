using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Tiels.Classes
{
    public class FileUpdates
    {
        protected static Dictionary<string, ChangedType> fileupdates = new Dictionary<string, ChangedType>();
        private static int nextid = 0;
        private static bool isCopying = false;

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

        public static void CreateEntry(string entry) => _ = AsyncCreateEntry(entry);

        public static void ChangeEntry(string entry) => _ = AsyncChangeEntry(entry);

        public static void DeleteEntry(string entry) => _ = AsyncDeleteEntry(entry);

        public async static Task AsyncCreateEntry(string entry)
        {
            while (true)
            {
                if (!isCopying)
                {
                    fileupdates.Add(entry, ChangedType.CREATED);
                    break;
                }
            }
        }

        public async static Task AsyncChangeEntry(string entry)
        {
            while (true)
            {
                if (!isCopying)
                {
                    fileupdates.Add(entry, ChangedType.CHANGED);
                    break;
                }
            }
        }

        public async static Task AsyncDeleteEntry(string entry)
        {
            while (true)
            {
                if (!isCopying)
                {
                    fileupdates.Add(entry, ChangedType.DELETED);
                    break;
                }
            }
        }

        public async Task UpdateEntryTask()
        {
            while (true)
            {
                await Task.Delay(10000); //300000 5 min
                UpdateEntries();
            }
        }

        public void UpdateEntries()
        {
            try
            {
                MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                if (!mw.isLoading)
                {
                    isCopying = true;
                    Dictionary<string, ChangedType> fileupdates_copy = new Dictionary<string, ChangedType>(fileupdates);
                    fileupdates.Clear();
                    isCopying = false;
                    string[] lines = File.ReadAllLines(App.config_path + "\\Cache\\iconcache.db");
                    Dictionary<string, Dictionary<int, string>> iconcaches = new Dictionary<string, Dictionary<int, string>>();
                    List<string> tilesToRefresh = new List<string>();
                    string toWrite = "";

                    foreach (var line in lines)
                    {
                        iconcaches.Add(line.Split('*')[0], new Dictionary<int, string>());
                        iconcaches[line.Split('*')[0]].Add(0, line.Split('*')[1]);
                    }
                    foreach (var fileupdate in fileupdates_copy.Keys)
                    {
                        //ErrorHandler.Info(fileupdate + " + " + fileupdates_copy[fileupdate]);
                        switch (fileupdates_copy[fileupdate])
                        {
                            case ChangedType.CREATED:
                                //Pseudo-random icon id
                                if (true)
                                {
                                    int ri = new Random().Next(0, 1000000);
                                    Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                    string icon_name = GenerateIcons(fileupdate.Split('\t')[0], App.config_path, ri, unixTimestamp, true);
                                    if (iconcaches.ContainsKey(fileupdate.Split('\t')[0]))
                                        iconcaches[fileupdate.Split('\t')[0]].Add(iconcaches[fileupdate.Split('\t')[0]].Count, ChangedType.CREATED + "*" + icon_name);
                                    else
                                    {
                                        iconcaches.Add(fileupdate.Split('\t')[0], new Dictionary<int, string>());
                                        iconcaches[fileupdate.Split('\t')[0]].Add(0, icon_name);
                                    }

                                    ErrorHandler.Info(fileupdate + " [CASE CREATED]");
                                }
                                break;
                            case ChangedType.CHANGED:
                                ErrorHandler.Info(fileupdate + " [CASE CHANGED]");
                                if (true)
                                {
                                    if (iconcaches.ContainsKey(fileupdate.Split('\t')[0]))
                                        iconcaches[fileupdate.Split('\t')[0]].Add(iconcaches[fileupdate.Split('\t')[0]].Count, ChangedType.DELETED.ToString());
                                    else
                                    {
                                        File.AppendAllText(App.config_path + "\\Error.log", "\r\n[Warning: " + DateTime.Now + "] " + "Deleting not existing entry. " + fileupdate.Split('\t')[0]);
                                        iconcaches.Add(fileupdate.Split('\t')[0], new Dictionary<int, string>());
                                        iconcaches[fileupdate.Split('\t')[0]].Add(0, ChangedType.DELETED.ToString());
                                    }
                                    int ri = new Random().Next(0, 1000000);
                                    Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                    string icon_name = GenerateIcons(fileupdate.Split('\t')[0], App.config_path, ri, unixTimestamp, true);
                                    if (iconcaches.ContainsKey(fileupdate.Split('\t')[0]))
                                        iconcaches[fileupdate.Split('\t')[0]].Add(iconcaches[fileupdate.Split('\t')[0]].Count, ChangedType.CREATED + "*" + icon_name);
                                    else
                                    {
                                        iconcaches.Add(fileupdate.Split('\t')[0], new Dictionary<int, string>());
                                        iconcaches[fileupdate.Split('\t')[0]].Add(0, icon_name);
                                    }
                                }
                                /*if (iconcaches.ContainsKey(fileupdate.Split('\t')[0]))
                                    iconcaches[fileupdate.Split('\t')[0]].Add(iconcaches[fileupdate.Split('\t')[0]].Count, ChangedType.DELETED.ToString());
                                else
                                {
                                    File.AppendAllText(App.config_path + "\\Error.log", "\r\n[Warning: " + DateTime.Now + "] " + "Deleting not existing entry. " + fileupdate.Split('\t')[0]);
                                    iconcaches.Add(fileupdate.Split('\t')[0], new Dictionary<int, string>());
                                    iconcaches[fileupdate.Split('\t')[0]].Add(0, ChangedType.DELETED.ToString());
                                }

                                int ri0 = new Random().Next(0, 1000000);
                                Int32 unixTimestamp0 = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                string icon_name0 = GenerateIcons(fileupdate.Split('\t')[0], App.config_path, ri0, unixTimestamp0, true);
                                if (iconcaches.ContainsKey(fileupdate.Split('\t')[0]))
                                    iconcaches[fileupdate.Split('\t')[0]].Add(iconcaches[fileupdate.Split('\t')[0]].Count, ChangedType.CREATED + "*" + icon_name0);
                                else
                                {
                                    iconcaches.Add(fileupdate.Split('\t')[0], new Dictionary<int, string>());
                                    iconcaches[fileupdate.Split('\t')[0]].Add(0, icon_name0);
                                }*/
                                break;
                            case ChangedType.DELETED:
                                if (iconcaches.ContainsKey(fileupdate.Split('\t')[0]))
                                    iconcaches[fileupdate.Split('\t')[0]].Add(iconcaches[fileupdate.Split('\t')[0]].Count, ChangedType.DELETED.ToString());
                                else
                                {
                                    File.AppendAllText(App.config_path + "\\Error.log", "\r\n[Warning: " + DateTime.Now + "] " + "Deleting not existing entry. " + fileupdate.Split('\t')[0]);
                                    iconcaches.Add(fileupdate.Split('\t')[0], new Dictionary<int, string>());
                                    iconcaches[fileupdate.Split('\t')[0]].Add(0, ChangedType.DELETED.ToString());
                                }

                                ErrorHandler.Info(fileupdate + " [CASE DELETED]");
                                break;
                        }

                        string tileDirectoryName = Path.GetDirectoryName(fileupdate.Split('\t')[0].Replace(mw.path + "\\", ""));
                        if (!tilesToRefresh.Contains(tileDirectoryName))
                        {
                            tilesToRefresh.Add(tileDirectoryName);
                        }
                    }
                    //ErrorHandler.Info("Foreach");
                    Dictionary<string, string> new_iconcache = new Dictionary<string, string>();
                    //File.Copy(App.config_path + "\\Cache\\iconcache.db", App.config_path + "\\Cache\\iconcache_" + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + ".db", true);
                    File.Delete(App.config_path + "\\Cache\\iconcache.db");
                    File.WriteAllText(App.config_path + "\\Cache\\iconcache.db", "");
                    foreach (var iconcache in iconcaches.Keys)
                    {
                        foreach (var entry in iconcaches[iconcache].Keys)
                        {
                            if (entry == 0)
                            {
                                new_iconcache.Add(iconcache, iconcaches[iconcache][entry]);
                            }
                            else
                            {
                                string[] entry_splitted = iconcaches[iconcache][entry].Split('*');
                                if (entry_splitted[0] == ChangedType.CREATED.ToString())
                                {
                                    new_iconcache.Add(iconcache, iconcaches[iconcache][entry]);
                                }
                                else if (entry_splitted[0] == ChangedType.DELETED.ToString())
                                {
                                    new_iconcache.Remove(iconcache);
                                }
                            }
                        }
                    }
                    foreach (TileWindow tw in mw.tilesw)
                    {
                        if (tilesToRefresh.Contains(tw.name))
                            tw.ReadElements(tw.collumns, tw.rows);
                    }

                    foreach (var f_new_iconcache in new_iconcache.Keys)
                    {
                        try
                        {
                            if (new_iconcache[f_new_iconcache].Contains('*'))
                                toWrite += f_new_iconcache + "*" + new_iconcache[f_new_iconcache].Split('*')[1] + "\r\n";
                            else
                                toWrite += f_new_iconcache + "*" + new_iconcache[f_new_iconcache] + "\r\n";
                        }
                        catch (Exception ex0)
                        {
                            File.AppendAllText(App.config_path + "\\Error.log", "\r\n[Error: " + DateTime.Now + "] " + ex0.ToString());
                        }
                    }
                    File.AppendAllText(App.config_path + "\\Cache\\iconcache.db", toWrite);
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(App.config_path + "\\Error.log", "\r\n[Error: " + DateTime.Now + "] " + ex.ToString());
            }
        }

        public static string GenerateIcons(string path, string cfg_path, int random_integer, int integer, bool skip_entry = false)
        {
            string num = "";
            double proportion;
            System.Drawing.Bitmap bitmap = null;
            System.Drawing.Bitmap bitmap1 = null;
            System.Drawing.Bitmap bitmap2 = null;
            if (path.ToLower().Contains(".png") || path.ToLower().Contains(".jpg") || path.ToLower().Contains(".jpeg"))
            {
                try
                {
                    try
                    {
                        bitmap = new System.Drawing.Bitmap(path);
                    }
                    catch (Exception ex0)
                    {
                        byte[] buffer = new byte[12];
                        try
                        {
                            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                            {
                                fs.Read(buffer, 0, buffer.Length);
                                fs.Close();
                            }
                            if (buffer[0] == 82 && buffer[1] == 73 && buffer[2] == 70 && buffer[3] == 70 && buffer[8] == 87 && buffer[9] == 69 && buffer[10] == 66 && buffer[11] == 80)
                            {
                                //Webp
                                Imazen.WebP.SimpleDecoder decoder = new Imazen.WebP.SimpleDecoder();
                                bitmap = decoder.DecodeFromBytes(File.ReadAllBytes(path), new System.IO.FileInfo(path).Length);
                            }
                        }
                        catch (System.UnauthorizedAccessException ex1)
                        {
                            File.AppendAllText(cfg_path + "\\Error.log", "\r\n[Error: " + DateTime.Now + "] " + ex0.ToString());
                        }
                    }
                    bitmap1 = bitmap.Clone(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.PixelFormat.Format32bppPArgb);//BitmapHandler.CloneImage(ConvertToBitmap((BitmapSource)bitmap));
                    if (bitmap.Width > bitmap.Height)
                    {
                        proportion = bitmap.Width / 128;
                    }
                    else
                    {
                        proportion = bitmap.Height / 128;
                    }
                    if (bitmap1.Height > 128 && bitmap1.Width > 128)
                        bitmap2 = new System.Drawing.Bitmap(bitmap1, (int)(bitmap1.Width / proportion), (int)(bitmap1.Height / proportion));
                    else
                        bitmap2 = bitmap1;
                    bitmap2.Save(cfg_path + "\\Cache\\thumbnail_" + random_integer + integer, System.Drawing.Imaging.ImageFormat.Png);
                    if (!skip_entry)
                        File.AppendAllText(cfg_path + "\\Cache\\iconcache.db", path + "*" + "\\Cache\\thumbnail_" + random_integer + integer + "\r\n");
                    num = "\\Cache\\thumbnail_" + random_integer + integer;
                    bitmap = null;
                    bitmap1 = null;
                    bitmap2 = null;
                }
                catch (Exception ex)
                {

                }
                //bitmap2 = null;
            }
            else
            {
                IntPtr hIcon = Util.GetJumboIcon(Util.GetIconIndex(path));

                // from native to managed
                try
                {
                    using (System.Drawing.Icon ico = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone())
                    {
                        // save to file (or show in a picture box)
                        if (!IsSmallIcon(ico.ToBitmap()))
                        {
                            ico.ToBitmap().Save(cfg_path + "\\Cache\\icon_" + random_integer + integer, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else
                        {
                            System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(0, 0, 48, 48);
                            System.Drawing.Image img = ico.ToBitmap();
                            Util.CropImage(img, cropRect).Save(cfg_path + "\\Cache\\icon_" + random_integer + integer, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        if (!skip_entry)
                            File.AppendAllText(cfg_path + "\\Cache\\iconcache.db", path + "*" + "\\Cache\\icon_" + random_integer + integer + "\r\n");
                        num = "\\Cache\\icon_" + random_integer + integer;
                    }
                    Util.Shell32.DestroyIcon(hIcon); // don't forget to cleanup
                }
                catch (Exception ex)
                {

                }
            }
            return num;
        }

        public static bool IsSmallIcon(System.Drawing.Bitmap img)
        {
            int solidPixels = 0;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    if (i >= 48 && j >= 48)
                    {
                        System.Drawing.Color pixel = img.GetPixel(i, j);
                        if (pixel.A != 0)
                        {
                            solidPixels++;
                        }
                    }
                }
            }
            if (solidPixels <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}