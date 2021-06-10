using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace ConsoleApp5
{
    class Program
    {
        //Таймер
        static void Timer()
        {
            int number = 0;
            while (true)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("                                                                                                                       ");

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("{0} s.\tRest photos to analyze: {1}", number, Directory.GetFiles("userphotos", "*.*").Length);

                number++;
                Thread.Sleep(1000);
            }
        }
        //Поисх всех вхождений подстроки
        static int AllMatches(string key, ref string soup)
        {
            var indices = new List<int>();

            int index = soup.IndexOf(key, 0);
            while (index > -1)
            {
                indices.Add(index);
                index = soup.ToLower().IndexOf(key.ToLower(), index + key.Length);
            }
            return indices.Count;
        }
        //Анализ вхождения ключевых слов
        static string AnalyzeKeyWords(string[] keys, string soup)
        {
            string conclusion = string.Empty;
            foreach(var key in keys)
            {
                if (key.Length > 0)
                {
                    conclusion += string.Format("Ключ: {0} количество {1}\n", key, AllMatches(key, ref soup));
                }
            }
            return conclusion;
        }
       //Анализ фотографий
        static string PythonAnalisys()
        {
            string textFromFile = string.Empty;
            try
            {
                string command = "analyzer.py";
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "python";
                start.Arguments = string.Format("{0}", command);
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        Console.Write(result);

                        FileStream file = new FileStream("alalisys_photos.txt", FileMode.Open);
                        byte[] array = new byte[file.Length];
                        file.Read(array, 0, array.Length);
                        textFromFile = System.Text.Encoding.Default.GetString(array);
                        
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return textFromFile;
        }
        //Скачивание текста
        static string GetText(VkApi api, long? id, ulong qWallPosts)
        {
            var records = api.Wall.Get(new WallGetParams
            {
                OwnerId = id,
                Count = qWallPosts
            });

            StringBuilder stringBuilder = new StringBuilder();
            
            foreach (var record in records.WallPosts)
            {
                stringBuilder.Append(record.Text + " ");
            }
            using (FileStream fstream = new FileStream("alalisys_text.txt", FileMode.OpenOrCreate))
            {
                byte[] array = System.Text.Encoding.Default.GetBytes(stringBuilder.ToString());
                fstream.Write(array, 0, array.Length);
            }
            return stringBuilder.ToString();
        }
        //Скачивание фотографий
        static void GetPhotos(VkApi api, long? id, ulong qWallPosts)
        {
            List<string> urls = new List<string>();
            var photos = api.Photo.Get(new PhotoGetParams
            {
                OwnerId = id,
                AlbumId = VkNet.Enums.SafetyEnums.PhotoAlbumType.Wall,
                Count =  qWallPosts
            });
            foreach (var photo in photos)
            {
                ulong height = 0;
                string url = string.Empty;
                foreach (var size in photo.Sizes)
                {
                    if (size.Height > height)
                    {
                        height = size.Height;
                        url = size.Url.AbsoluteUri;
                    }
                }
                urls.Add(url);
            }
            int count = 0;
            foreach (var url in urls)
            {
                using (var client = new WebClient())
                {
                    try
                    {
                        if (url.Length > 0)
                        {
                            client.DownloadFile(url, "userphotos/" + "count" + Convert.ToString(count) + ".jpg");
                        }
                    }
                    catch
                    {

                    }
                }
                count++;
            }
        }
        static void Main(string[] args)
        {
            long? id = 2436112;
            string access_token = "4fc507a59d36dce191e6ebe4892ae198506ac28e4d2b191981d711a28390bca51d30e9de0f0d296a59030";
            string[] key_words;
            //Console.WriteLine("ID пользователя для анализа:");
            //access_token = Console.ToInt32(Console.ReadLine());
            //Console.WriteLine("access_token вашего аккаунта:");
            //id = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Ключевые слова через пробел:");
            key_words = Console.ReadLine().Split(' ');

            var api = new VkApi();

            api.Authorize(new ApiAuthParams
            {
                AccessToken = access_token
            });

            Thread timer = new Thread(Timer);
            timer.Start();

            string soup = GetText(api, id, 1000);

            GetPhotos(api, id, 200);

            soup += PythonAnalisys();
            string conclusion = AnalyzeKeyWords(key_words, soup);
            Console.WriteLine("\n\n\n");
            Console.WriteLine(conclusion);
            timer.Abort();
            Console.ReadLine();
        }
    }
}
