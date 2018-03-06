using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web
{

    public class ListTrie
    {
        private readonly HybridNode _root;

        public ListTrie()
        {
            _root = new HybridNode('^', new List<string>(), new Dictionary<char, HybridNode>());
        }

        public List<string> GetRoot()
        {
            return _root.Suffixs;
        }

        public void Add(string s)
        {
            var currentNode = _root;
            currentNode.Add(s);
        }


        public List<string> GetSearchResults(string searchTerm)
        {
            if (searchTerm.Length > 0)
            {
                return this.GetSearchResults(this._root, searchTerm);
            }
            return new List<string>();
        }

        private List<string> GetSearchResults(HybridNode node, string searchTerm)
        {
            List<string> results = new List<string>();
            searchTerm = searchTerm.Replace(' ', '_');
            string lowercaseSearch = searchTerm.ToLower();

            // searchTerm = he
            HybridNode resultsNode = node;
            string level = "";

            foreach (char c in lowercaseSearch)
            {

                if (resultsNode.useDict && resultsNode.Children.ContainsKey(c))
                {
                    level = level + c;
                    resultsNode = resultsNode.Children[c];
                }
            }

            while (resultsNode.Suffixs == null)
            {
                var first = resultsNode.Children.First().Value;
                level = level + first.Value;
                resultsNode = first;
            }

            if (resultsNode != null)
            {
                foreach (string s in resultsNode.Suffixs)
                {
                    string complete = level + s;

                    if (complete.StartsWith(lowercaseSearch)) // add in check for 10 count
                    {
                        results.Add(complete);
                    }
                    if (results.Count >= 10)
                    {
                        break;
                    }
                }
            }


            List<string> resultsNoUnderscore = new List<string>();
            foreach (string s in results)
            {
                string noUnderscore = s.Replace('_', ' ');
                resultsNoUnderscore.Add(noUnderscore);
            }
            return resultsNoUnderscore;
        }
    }
}