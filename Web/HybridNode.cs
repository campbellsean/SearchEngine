using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web
{
    public class HybridNode
    {
        public char Value { get; set; }

        public List<string> Suffixs { get; set; }

        public bool useDict = false;

        // May try to delete this later and create two seperate node classes
        public Dictionary<char, HybridNode> Children { get; set; }

        public HybridNode(char newValue, List<string> newSuffixs, Dictionary<char, HybridNode> newChildren)
        {
            Value = newValue;
            Suffixs = newSuffixs;
            Children = newChildren;
        }

        // Seems like it might work
        public void Add(string toAdd)
        {
            if (this.useDict)
            {
                if (toAdd.Length >= 1)
                {
                    char firstChar = toAdd[0];
                    string withoutFirstChar = toAdd.Substring(1);

                    if (this.Children.ContainsKey(firstChar))
                    {
                        this.Children[firstChar].Add(withoutFirstChar);
                    }
                    else
                    {
                        List<string> toAddSuffix = new List<string>();
                        toAddSuffix.Add(withoutFirstChar);

                        HybridNode newNode = new HybridNode(firstChar, toAddSuffix, new Dictionary<char, HybridNode>());
                        this.Children.Add(firstChar, newNode);
                    }
                }
            }
            else if (this.Suffixs.Count < 50)
            {
                this.Suffixs.Add(toAdd);
            }
            else if (this.Suffixs.Count == 50)
            {
                this.useDict = true;
                // we want to add "john"

                // for the first letter of every word in the suffix we want to add to the dictionary and
                // create a hybrid node
                foreach (string s in this.Suffixs)
                {
                    this.Add(s);
                }
                // finally we add John and use a dictionary this time
                this.Add(toAdd);

                // to save space
                this.Suffixs = null;
            }
        }
    }
}