using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace WordWallGenerator
{
    public class Node
    {
        public string Value { get; set; }
        public HashSet<Node> Edges { get; } = new();
        public HashSet<Node> BackEdges { get; } = new();

        public override int GetHashCode()
        {
            return Value.ToLower().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Node node)
            {
                return Value.ToLower().Equals(node.Value.ToLower());
            }

            return false;
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public static class Program
    {
        static void Main(string[] args)
        {
            //Construct a multi-root acyclic digraph with unique nodes. 
            //This structure is then traversed in a breadth-first fashion to ensure every sentence is representable 
            
            //TODO: prefix words.... don't need cook and cooks as separate words
            
            var nodes = ReadLines("sentances.txt")
                //tokenize
                .Select(line => line.Split(" "))
                .ToDigraph()
                .Flatten();

            var builder = new StringBuilder();
            int lineLength = 0;
            foreach (var node in nodes)
            {
                lineLength += node.Length;
                if (lineLength > 34)
                {
                    builder.Append("\n");
                    lineLength = node.Length;
                }
                builder.Append(node);
            }

            var str = builder.ToString();
            Console.WriteLine(str);
            Console.WriteLine("count: " + str.Length);
        }

        public static HashSet<Node> ToDigraph(this IEnumerable<string[]> matrix)
        {
            //For quick access to each node 
            var nodes = new Dictionary<string, Node>();

            Node AddNewNode(string token)
            {
                var node = new Node()
                {
                    Value = token 
                };
                        
                nodes.Add(token.ToLower(), node);

                return node;
            }

            bool TryGetNode(string token, out Node? node)
            {
                return nodes.TryGetValue(token.ToLower(), out node);
            }

            bool IsNodeInPath(Node child, Node node)
            {
                if (child == node)
                {
                    return true;
                }
                if (!child.BackEdges.Any())
                {
                    return false;
                }

                foreach (var back in child.BackEdges)
                {
                    if (IsNodeInPath(back, node))
                    {
                        return true;
                    }
                }

                return false;
            }
            
            
            //Make sure we identify the root nodes
            var roots = new HashSet<Node>();
                
            foreach (var line in matrix)
            {
                Node? node = null;
                var token = line[0];
                //Haven't seen this word yet, initialize the edge list
                if (!TryGetNode(token, out node))
                {
                    node = AddNewNode(token);
                        
                    //each of the first words in a sentence might be a root word....
                    roots.Add(node);
                } 
                
                for (var i = 1; i < line.Length; ++i)
                {
                    //grab the next node and add an edge to the edge list if there is one
                   
                    var next = line[i];
                    //Haven't seen this word yet, initialize the edge list
                    if (!TryGetNode(next, out var node2))
                    {
                        node2 = AddNewNode(next);
                    }

                    //We want to be careful about cycles.... if adding this node will create a cycle, we're going to create a new node instead
                    var index = 2;
                    while (IsNodeInPath(node, node2))
                    {
                        //Does the node exist already?
                        if (!TryGetNode(next + index, out node2))
                        {
                            //If not, create it
                            node2 = AddNewNode(next + index);
                        }

                        index += 1;
                    }
                   
                    //Add the edge
                    node.Edges.Add(node2);
                    node2.BackEdges.Add(node);

                    node = node2;
                }
            }

            return roots;
        }
        public static IEnumerable<string> Flatten(this IEnumerable<Node> roots)
        {
            //Breadth  first ordered traversal of a multi-root acyclic digraph
            var queue = new Queue<Node>();

            var visitCount = new Dictionary<Node, int>();
            
            //Enqueue the roots first
            foreach (var root in roots)
            {
                queue.Enqueue(root);
                //Enqueuing a root here DOES NOT count as a visit. Sometimes roots have backedges, and those should not be processed immediately
                visitCount[root] = -1;
            }
            
            while (queue.Count > 0)
            {
                var word = queue.Dequeue();

                //nodes might have multiple parents, and we don't want to output them until all the parentpaths have been traversed
                //so keep count 
                if (!visitCount.TryGetValue(word, out int count))
                {
                    visitCount[word] = 0;
                }
                
                visitCount[word] = count + 1;
                if (visitCount[word] < word.BackEdges.Count)
                {
                    continue;
                }

                yield return word.Value;

                //Naively no sorting
                foreach (var next in word.Edges)
                {
                    queue.Enqueue(next);
                }
            } 
        }
        public static IEnumerable<string> ReadLines(string filename)
        {
            using (var file = File.OpenText(filename))
            {
                string line = string.Empty;
                while ((line = file.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        yield return line;
                    }
                }
            } 
        }

    }
}