using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Svg;

namespace WordWallGenerator
{
    public class Node
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public HashSet<Node> Edges { get; } = new();
        public HashSet<Node> BackEdges { get; } = new();

        public override int GetHashCode()
        {
            return Id.ToLower().GetHashCode();
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
            Option<FileInfo> sentences = new("--sentences")
            {
                Description = "Sentences containing a list of words. These will be guaranteed representable in the word wall"
            };
            Option<FileInfo> svgOutOption = new("--svg")
            {
                Description = "The outputted SVG file."
            };
            Option<float> xSpaceOption= new("--xSpace")
            {
                Description = "The x center-to-center distance between each letter in centimeters"
            };
            Option<float> ySpaceOption= new("--ySpace")
            {
                Description = "The y center-to-center distance between each line in centimeters"
            };
            Option<float> letterSizeOption = new("--letterSize")
            {
                Description = "The font size in centimeters"
            };
            Option<float> maxWidthOption = new("--maxWidth")
            {
                Description = "The maximum width of the word wall in centimeters"
            };
            Option<bool> toUpperOption = new("--toUpper")
            {
                Description = "Uppercase all the letters in the output. Input become case insensitive. Default: False",
                DefaultValueFactory = res => false
            };
            Option<bool> caseInsensitiveOption = new("--caseSensitive")
            {
                Description = "Treats the input as case sensitive. Default: False",
                DefaultValueFactory = res => true 
            };
            Option<string> fontFamilyOption = new("--fontFamily")
            {
                Description = "The name of an installed font on the system. Monospaced fonts works best"
            };
            Option<bool> addFillOption = new("--addFill")
            {
                Description = "Should Filler words be added to the end of each line if it is shorter than the max width. Default: true",
                DefaultValueFactory = res => true
            };
            Option<FileInfo?> fillerWordsFileOption = new("--fillerWords")
            {
                Description = "A file that contains a newline separated list of filler words. Each will beused at random.",
                DefaultValueFactory = res => null
            };

            
            RootCommand rootCommand = new("Generate an SVG file from a list of sentences");
            rootCommand.Options.Add(sentences);
            rootCommand.Options.Add(svgOutOption);
            rootCommand.Options.Add(xSpaceOption);
            rootCommand.Options.Add(ySpaceOption);
            rootCommand.Options.Add(letterSizeOption);
            rootCommand.Options.Add(maxWidthOption);
            rootCommand.Options.Add(toUpperOption);
            rootCommand.Options.Add(caseInsensitiveOption);
            rootCommand.Options.Add(fontFamilyOption); 
            rootCommand.Options.Add(addFillOption);
            rootCommand.Options.Add(fillerWordsFileOption);

            var result = rootCommand.Parse(args);
            if (result.Errors.Count != 0)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine(error);
                }
            }

            var sentenceFile = result.GetRequiredValue<FileInfo>(sentences);
            var svgFile = result.GetRequiredValue(svgOutOption);
            var xSpace= result.GetRequiredValue(xSpaceOption);
            var ySpace= result.GetRequiredValue(ySpaceOption);
            var letterSize= result.GetRequiredValue(letterSizeOption);
            var maxWidth = result.GetRequiredValue(maxWidthOption);
            var toUpper = result.GetValue(toUpperOption);
            var caseSensitive = result.GetValue(caseInsensitiveOption);
            var fontFamily = result.GetValue(fontFamilyOption);
            var addFill = result.GetValue(addFillOption);
            var fillerWordsFile = result.GetValue(fillerWordsFileOption);

            try
            {
                var font = new FontFamily(fontFamily);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not load your chosen font {fontFamily}. Only installed fonts are supported, did you install it?");
            }
                
            //Construct a multi-root acyclic digraph with unique nodes. 
            //This structure is then traversed in a breadth-first fashion to ensure every sentence is representable 
            
            //TODO: prefix words.... don't need cook and cooks as separate words
            
            var lines = ReadLines(sentenceFile.FullName)
                //tokenize
                .Select(line => line.Split(" "))
                .ToDigraph(caseSensitive || toUpper)
                .Flatten()
                .LineBreak((int)(maxWidth/xSpace), addFill, fillerWordsFile != null ? fillerWordsFile.FullName : string.Empty)
                .Select(line => line.Select(node => toUpper ? node.Value.ToUpper() : node.Value).ToArray())
                .ToList();

            SaveToSvg(svgFile.FullName, fontFamily, xSpace, ySpace, letterSize, lines);
            WriteToConsole(lines);
        }

        public static HashSet<Node> ToDigraph(this IEnumerable<string[]> matrix, bool caseSensitive)
        {
            //For quick access to each node 
            var nodes = new Dictionary<string, Node>();

            Node AddNewNode(string token, string value)
            {
                var node = new Node()
                {
                    Id = caseSensitive ? token : token.ToLower(),
                    Value = value
                };
                        
                nodes.Add(caseSensitive ? token : token.ToLower(), node);

                return node;
            }

            bool TryGetNode(string token, out Node? node)
            {
                return nodes.TryGetValue(caseSensitive ? token : token.ToLower(), out node);
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
                    node = AddNewNode(token, token);
                        
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
                        node2 = AddNewNode(next, next);
                    }

                    //We want to be careful about cycles.... if adding this node will create a cycle, we're going to create a new node instead
                    var index = 2;
                    while (IsNodeInPath(node, node2))
                    {
                        //Does the node exist already?
                        if (!TryGetNode(next + index, out node2))
                        {
                            //If not, create it
                            node2 = AddNewNode(next + index, next);
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

        public static IEnumerable<Node[]> LineBreak(this IEnumerable<Node> nodes, int lineWidth, bool addFill, string fillerWordsFileName)
        {
            var words = new[] { "a", "to", "and", "help", "tight", "output", "someone" };

            if (addFill && !string.IsNullOrEmpty(fillerWordsFileName))
            {
                words = File.ReadAllLines(fillerWordsFileName);
            }

            var line = new List<Node>();
            var random = new Random(123);
            foreach(var node in nodes)
            {
                var total = line.Sum(n => n.Value.Length);

                //If this node would make the line too long
                if (total + node.Value.Length > lineWidth)
                {
                    if (addFill)
                    {
                        //Loop adding filler words until we're full
                        var diff = lineWidth - total;
                    
                        while (diff > 0)
                        {
                            var w = words.Where(w => w.Length <= diff).ToList();
                            if (!w.Any())
                            {
                                Console.Error.WriteLine($"no words smaller than {diff} available to fill, skipping line fill");
                                break;
                            }
                            var index = random.Next(w.Count); 
                            diff -= w[index].Length;
                            line.Add(new Node(){Value = w[index]});
                        }
                    }

                    yield return line.ToArray();
                    line.Clear();
                }
               
                line.Add(node);
            }

            //fill out last line
            if (line.Any())
            {
                var total = line.Sum(n => n.Value.Length);
                if (addFill)
                {
                    //Loop adding filler words until we're full
                    var diff = lineWidth - total;
                    
                    while (diff > 0)
                    {
                        var w = words.Where(w => w.Length <= diff).ToList();
                        if (!w.Any())
                        {
                            Console.Error.WriteLine($"no words smaller than {diff} available to fill, skipping line fill");
                            break;
                        }
                        var index = random.Next(w.Count); 
                        diff -= w[index].Length;
                        line.Add(new Node(){Value = w[index]});
                    }
                }

                yield return line.ToArray();
            }
        }

        public static void WriteToConsole(IEnumerable<string[]> lines)
        {
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                foreach (var word in line)
                {
                    builder.Append(word);
                }
                builder.AppendLine();
            }

            Console.WriteLine(builder.ToString());
        }
        public static void SaveToSvg(string filename, string fontFamily, float x_space_cm, float y_space_cm, float fontSize_cm, IEnumerable<string[]> lines)
        {
            var doc = new SvgDocument();
            
            float maxWidth_cm = 0;
            float x = 0;
            float y = 1;
            foreach (var line in lines)
            {
                foreach (var word in line)
                {
                    foreach (var c in word)
                    {
                        var character = new SvgText(c.ToString());
                        character.X = new SvgUnitCollection();
                        character.X.Add(new SvgUnit(SvgUnitType.Centimeter, x));
                        character.Y = new SvgUnitCollection();
                        character.Y.Add(new SvgUnit(SvgUnitType.Centimeter, y));

                        doc.Children.Add(character);

                        x += x_space_cm;
                    }
                }
                maxWidth_cm = Math.Max(maxWidth_cm, x);
                x = 0;
                y += y_space_cm;
            }

            doc.Height = new SvgUnit(SvgUnitType.Centimeter, y);
            doc.Width = new SvgUnit(SvgUnitType.Centimeter, maxWidth_cm);
            doc.FontFamily = fontFamily; 
            doc.FontWeight = SvgFontWeight.Bold;
            doc.FontSize = new SvgUnit(SvgUnitType.Centimeter, fontSize_cm); 
            
            doc.Write(filename);
        }
                        
        /// Breadth first ordered traversal of a multi-root acyclic digraph. Outputs elements line by line with a best fit to line width
        /// using a knapsack solution 
        /// </summary>
        /// <param name="roots"></param>
        /// <returns></returns>
        public static IEnumerable<Node> Flatten(this IEnumerable<Node> roots)
        {
            var visitCount = new Dictionary<Node, int>();
            void FillVisitCountDictionary(IEnumerable<Node> nodes)
            {
                foreach (var node in nodes)
                {
                    visitCount[node] = node.BackEdges.Count;
                    FillVisitCountDictionary(node.Edges);
                }
            }
           
            FillVisitCountDictionary(roots);
            
            while (visitCount.Any())
            {
                //grab the nodes that have zero dependencies
                var zeros = visitCount.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key);
                
                //biggest to smallest
                // zeros.Sort((a, b) => a.Key.Value.Length.CompareTo(b.Key.Value.Length));
                foreach (var node in zeros)
                {
                    visitCount.Remove(node);
                    foreach (var edge in node.Edges)
                    {
                        visitCount[edge]--;
                    }

                    yield return node;
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