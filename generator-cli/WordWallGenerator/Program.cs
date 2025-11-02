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

        // public override int GetHashCode()
        // {
        //     return Id.ToLower().GetHashCode();
        // }

        // public override bool Equals(object obj)
        // {
        //     if (obj is Node node)
        //     {
        //         return Value.ToLower().Equals(node.Value.ToLower());
        //     }
        //
        //     return false;
        // }

        public override string ToString()
        {
            return Value;
        }
    }

    public static class Program
    {
        static void Main(string[] args)
        {
            Option<FileInfo[]> sentenceOptions = new("--sentences")
            {
                Description = "Sentences containing a list of words. These will be guaranteed representable in the word wall",
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
            rootCommand.Options.Add(sentenceOptions);
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

            var sentenceFiles = result.GetRequiredValue<FileInfo[]>(sentenceOptions);
            var svgFile = result.GetRequiredValue(svgOutOption);
            var xSpace= result.GetRequiredValue(xSpaceOption);
            var ySpace= result.GetRequiredValue(ySpaceOption);
            var letterSize= result.GetRequiredValue(letterSizeOption);
            var maxWidth = result.GetRequiredValue(maxWidthOption);
            var toUpper = result.GetValue(toUpperOption);
            var caseInsensitive = result.GetValue(caseInsensitiveOption);
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
            IEnumerable<string> sentences = new List<string>();
            foreach (var file in sentenceFiles)
            {
                sentences = sentences.Concat(ReadLines(file.FullName));
            }

            var digraph = sentences
                //tokenize
                .Select(line => line.Split(" "))
                .ToDigraph(caseInsensitive || toUpper);
            var clockDigraph = ClockDigraph();

            var id = clockDigraph.Id;
            var count = 1;
            //naive (boring) Merge
            while (digraph.Contains(clockDigraph))
            {
                clockDigraph.Id = $"{id}{count}";
            }
            digraph.Add(clockDigraph);
            
            //TODO: serialize digraph for use in a visual editor 
            var lines = digraph
                .Flatten()
                .LineBreak((int)(maxWidth/xSpace), addFill, fillerWordsFile != null ? fillerWordsFile.FullName : string.Empty)
                .Select(line => string.Join("", line.Select(node => toUpper ? node.Value.ToUpper() : node.Value)))
                .ToList();

            SaveToSvg(svgFile.FullName, fontFamily, xSpace, ySpace, letterSize, lines);
            WriteToConsole(lines);
            
            var total = lines.Sum(line => line.Length);
            Console.WriteLine($"Total letters: {total}");

            Console.WriteLine("testing....");
            foreach(var sentence in sentences)
            {
                GetPositions(lines.ToArray(), sentence, caseInsensitive);
            }
            Console.WriteLine("Success");
        }

        public static Node ClockDigraph()
        {
            Node[] hours = new[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve" }
                .Select(s => new Node() {Id = s, Value = s})
                .ToArray();
            Node [] decades = new[] { "twenty", "thirty", "forty", "fifty", "sixty"}
                .Select(s => new Node() {Id = s, Value = s})
                .ToArray();
            Node [] teens = new [] {"ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" }
                .Select(s => new Node() {Id = s + "2", Value = s})
                .ToArray();
            Node [] digits = new[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" }
                .Select(s => new Node() {Id = s + "2", Value = s})
                .ToArray();
            
            var it = new Node()
            {
                Id = "it",
                Value = "it",
            };

            var @is = new Node()
            {
                Id = "is",
                Value = "is",
            };
            
            @is.BackEdges.Add(it);
            it.Edges.Add(@is);
            
            var oh = new Node()
            {
                Id = "oh",
                Value = "oh",
            };
            var quarter = new Node()
            {
                Id = "quarter",
                Value = "quarter",
            };
            var half = new Node()
            {
                Id = "half",
                Value = "half",
            };
            @is.Edges.Add(quarter);
            quarter.BackEdges.Add(@is);
            @is.Edges.Add(half);
            half.BackEdges.Add(@is);
            
            var past = new Node()
            {
                Id = "past",
                Value = "past",
            };
            var till = new Node()
            {
                Id = "till",
                Value = "till",
            };
            
            quarter.Edges.Add(past);
            past.BackEdges.Add(quarter);
            quarter.Edges.Add(till);
            till.BackEdges.Add(quarter);
            half.Edges.Add(past);
            past.BackEdges.Add(half);
            half.Edges.Add(till);
            till.BackEdges.Add(half);
            
            for (int h = 1; h <= 12; ++h)
            {
                var hour = hours[h - 1]; 
                
                hour.BackEdges.Add(@is);
                @is.Edges.Add(hour);
                
                hour.Edges.Add(oh);
                oh.BackEdges.Add(hour);
                
                quarter.Edges.Add(hour);
                hour.BackEdges.Add(quarter);
                
                half.Edges.Add(hour);
                hour.BackEdges.Add(half);
                
                for (int m = 0; m <= 59; ++m)
                {
                    //"it is one" is a complete sentence
                    if (m == 0)
                    {
                        continue;
                    }
                    //"it is one oh one"
                    if (m > 0 && m < 10)
                    {
                        var digit = digits[m - 1];
                        oh.Edges.Add(digit);
                        digit.BackEdges.Add(oh);
                        continue;
                    }

                    if (m >= 10 && m < 20)
                    {
                        var teen = teens[m - 10];
                        hour.Edges.Add(teen);
                        teen.BackEdges.Add(hour);
                        continue;
                    }

                    if (m >= 20)
                    {
                        var decade =  decades[(m - 20)/10];
                        hour.Edges.Add(decade);
                        decade.BackEdges.Add(hour);
                        if (m % 10 != 0)
                        {
                            var digit =  digits[(m - 20)%10-1];
                            decade.Edges.Add(digit);
                            digit.BackEdges.Add(decade); 
                        }
                       
                        continue;
                    }
                }
            }

            return it;
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

            var m = matrix.ToList();
            var total = m.Count;
            var progress = 0f;
            foreach (var line in m)
            {
                if (progress % 10 == 0)
                {
                    Console.WriteLine($"progress: {progress/total}");
                }
                progress++;
                
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
            //We want it to be random, but consistent across runs. 
            var random = new Random(42);
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
                                Console.Error.WriteLine($"no words smaller than {diff} available to fill, skipping remaining line fill");
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
                            Console.Error.WriteLine($"no words smaller than {diff} available to fill, skipping remaining line fill");
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

        public static void WriteToConsole(IEnumerable<string> lines)
        {
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                builder.Append(line);
                builder.AppendLine();
            }

            Console.WriteLine(builder.ToString());
        }
        public static void SaveToSvg(string filename, string fontFamily, float x_space_cm, float y_space_cm, float fontSize_cm, IEnumerable<string> lines)
        {
            var doc = new SvgDocument();
            
            float maxWidth_cm = 0;
            float x = 0;
            float y = 1;
            foreach (var line in lines)
            {
                foreach (var c in line)
                {
                    var character = new SvgText(c.ToString());
                    character.X = new SvgUnitCollection();
                    character.X.Add(new SvgUnit(SvgUnitType.Centimeter, x));
                    character.Y = new SvgUnitCollection();
                    character.Y.Add(new SvgUnit(SvgUnitType.Centimeter, y));

                    doc.Children.Add(character);

                    x += x_space_cm;
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

        public static int [] GetPositions(string[] matrix, string sentence, bool caseInsensitive)
        {
            var result = new List<int>();
            int i = 0, column = 0, letterIndex = 0;
            var tokens = sentence.Split(" ");
            
            for(int row = 0; row < matrix.Length && i < tokens.Length; row++)
            {
                var line = matrix[row];
                var word = tokens[i];
                
                var index = FirstIndexOf(line, word, column, caseInsensitive);
                if (index != -1)
                {
                    column = index + word.Length;
                    //stay on this row
                    row--;
                    //advance the word
                    i++;
                    //return the led index for addresing
                    for (int x = index; x < index + word.Length; ++x)
                    {
                        result.Add(letterIndex+x);
                    }
                }
                else
                {
                    column = 0;
                    //LEDs are seuential, keep track of the current led index to use in addressing
                    letterIndex += line.Length;
                }
            }

            if (i < tokens.Length)
            {
                //Not all words were processed
                throw new Exception($"Not all words could be found for sentence \"{sentence}\" at word {tokens[i]}");
            }
            
            return result.ToArray();
        }

        public static int FirstIndexOf(string input, string search, int startAt, bool caseInsensitive)
        {
            if (startAt >= input.Length)
            {
                return -1;
            }

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(search))
            {
                return -1;
            }


            if (caseInsensitive)
            {
                input = input.ToLower();
                search = input.ToLower();
            }

            for (int i = startAt; i < input.Length; i++)
            {
                var matchFound = true;
                for (int j = 0; j < search.Length; j++)
                {
                    if (j + i >= input.Length)
                    {
                        break;
                    }
                    
                    if (input[i+j] != search[j])
                    {
                        matchFound = false;
                        break;
                    }
                }

                if (matchFound)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}