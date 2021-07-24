using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace ConsoleApp5
{
    public partial class Program
    {
        //-------------------------------------------------------------------------------------------//
        //---------------------------------АНАЛИЗ СТЕНЫ (Главный цикл)-----------------------------//
        //-------------------------------------------------------------------------------------------//
        static void Analyze(VkApi api, long? id, ulong postsToAnalyze, bool loger, string dictionary)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo("userphotos/");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            string[] key_words;
            using (FileStream fstream = File.OpenRead(dictionary))
            {
                byte[] array = new byte[fstream.Length];
                fstream.Read(array, 0, array.Length);
                key_words = System.Text.Encoding.UTF8.GetString(array).Split(',').Select(x => x.ToLower()).ToArray();
            }

            log = loger;

            quantity = new int[key_words.Length];
            quantity.Initialize();

            var timer = new System.Timers.Timer(1000);
            int i = 0;
            ulong offset = 0;
            int n = 0;
            if (log == false)
            {
                timer.Elapsed += delegate {
                    int remained_posts = (int)postsToAnalyze - i - (int)offset;
                    Console.Write("\r Processing: {0} seconds. There are {1} to analyze...", n++, remained_posts);
                };
                timer.AutoReset = true;
                timer.Enabled = true;
                timer.Start();
            }

            while (offset < postsToAnalyze)
            {
                if (offset + (ulong)i >= postsToAnalyze)
                    break;

                var records = api.Wall.Get(new WallGetParams
                {
                    OwnerId = id,
                    Count = postsToAnalyze - offset,
                    Offset = offset
                });

                if (offset == 0)
                {
                    Console.WriteLine("У пользователя {0} постов", records.TotalCount);
                }

                i = 0;
                foreach (var post in records.WallPosts)
                {
                    if (log)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.White;
                        int nmb_of_post = (int)offset + i;
                        Console.WriteLine("Пост " + nmb_of_post + " от " + post.Date);
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.WriteLine("\n" + post.Text);
                    }
                    AnalyzeKeyWords(key_words, post.Text, "Анализ текста поста:");

                    List<string> photos = new List<string>();
                    foreach (var attachment in post.Attachments)
                    {
                        if (attachment.Type.Name == "Photo")
                        {
                            photos.Add(attachment.Instance.Id.ToString());
                        }
                    }
                    Thread.Sleep(500);

                    if (photos.Count > 0)
                    {
                        GetAndAnalyzePhotos(api, id, PostRepostComment.Post, key_words, photos);
                        Thread.Sleep(1000);
                    }
                    i++;
                    photos.Clear();
                    if (log)
                        Console.WriteLine("\t" + "_____________________________________________________________________________________________________");

                    AnalyzeReposts(api, post, key_words);
                    if (log)
                        Console.WriteLine("\t" + "_____________________________________________________________________________________________________");


                    GetAndAnalyzeComments(api, id, key_words, post);
                    if (log)
                        Console.WriteLine("\t" + "_____________________________________________________________________________________________________");

                }
                offset += 100;
                if (offset + (ulong)i == postsToAnalyze)
                {
                    break;
                }
            }


            timer.Stop();

            List<ConcusionPair> conclusion = new List<ConcusionPair>();
            for (int j = 0; j < key_words.Length; j++)
            {
                conclusion.Add(new ConcusionPair(key_words[j], quantity[j]));
            }

            ConcusionPair[] conclusion_as_array = conclusion.ToArray();
            Array.Sort(conclusion_as_array, new ConclusionComparer());

            Console.WriteLine($"\n\n\n\n\n\t\t\t\t\t ЗАКЛЮЧЕНИЕ. Выполнено за {n} секунд");

            foreach (var pair in conclusion_as_array)
            {
                Console.WriteLine("\t\t\t\t\t\tКлюч {0}. Найдено {1}", pair.key, pair.value);
            }
        }



        //-------------------------------------------------------------------------------------------//
        //---------------------------------АНАЛИЗ РЕПОСТОВ------------------------------------------//
        //-------------------------------------------------------------------------------------------//
        static void AnalyzeReposts(VkApi api, Post post, string[] key_words)
        {
            if (log)
            {

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("\t" + post.CopyHistory.Count.ToString() + " репостов(а)");
                Console.ForegroundColor = ConsoleColor.White;
            }

            foreach (var repost in post.CopyHistory)
            {
                if (log)
                    Console.WriteLine("\t" + repost.Text);

                AnalyzeKeyWords(key_words, repost.Text, "\t" + "Анализ текста репоста:");

                List<string> repostPhotos = new List<string>();
                foreach (var attachment in repost.Attachments)
                {
                    if (attachment.Type.Name == "Photo")
                    {
                        repostPhotos.Add(attachment.Instance.Id.ToString());
                    }
                }

                if (repostPhotos.Count > 0)
                {
                    try
                    {
                        GetAndAnalyzePhotos(api, repost.OwnerId, PostRepostComment.Repost, key_words, repostPhotos);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("\t" + exception.Message);
                    }
                }
            }
        }


        //---------------------------------------------------------------------------------------//
        //---------------------------------Анализ вхождения ключевых слов-----------------------------//
        //---------------------------------------------------------------------------------------//
        static string AnalyzeKeyWords(string[] keys, string post, string messageAnalyze)
        {
            post.Replace('ё', 'е');
            post = post.ToLower();

            string conclusion = string.Empty;

            int i = 0;
            foreach (var key in keys)
            {
                int matches = AllMatches(key, post);
                quantity[i] += matches;
                if (post.Length > 0 && matches > 0)
                {
                    conclusion += string.Format("\tКлюч: {0} количество {1}\n", key.ToLower(), matches);
                }
                i++;
            }
            if (log)
            {
                if (conclusion.Length > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(messageAnalyze);
                    Console.WriteLine("\n\t" + conclusion);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(messageAnalyze);
                    Console.WriteLine("\t Совпадения не найдены");
                }
                Console.ForegroundColor = ConsoleColor.White;
            }

            return conclusion;
        }
    }
}
