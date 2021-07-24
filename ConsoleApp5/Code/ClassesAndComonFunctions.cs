using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    public partial class Program
    {
        //Поисх всех вхождений подстроки
        static int AllMatches(string key, string soup)
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
    }
    enum PostRepostComment
    {
        Post = 0,
        Repost = 1,
        Comment = 2
    }

    public class ConclusionComparer : IComparer<ConcusionPair>
    {
        public int Compare(ConcusionPair val1, ConcusionPair val2)
        {
            if (val1.value < val2.value)
                return 1;
            else if (val1.value > val2.value)
                return -1;
            else
                return 0;
        }
    }

    public class ConcusionPair
    {
        public ConcusionPair(string key, int value)
        {
            this.key = key;
            this.value = value;
        }
        public string key { get; set; }
        public int value { get; set; }
    }

}
