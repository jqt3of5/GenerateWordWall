using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WordWallGenerator;

namespace TestProject1
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestTree()
        {
            var nodes = new List<string>() { "I love Kira", "I love you" }
                .Select(line => line.Split(" "))
                .ToDigraph(true);
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(nodes.Select(n => n.Value), Contains.Item("I") );
            
        }
        
        [Test]
        public void TestTree1()
        {
            var nodes = new List<string>() { "I love Kira", "I hate you" }
                .Select(line => line.Split(" "))
                .ToDigraph(true);
            
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(nodes.Select(n => n.Value), Contains.Item("I") );
            
        }
        
        [Test]
        public void TestTree2()
        {
            var nodes = new List<string>() { "I love Kira", "we hate you" }
                .Select(line => line.Split(" "))
                .ToDigraph(true);
            
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(nodes.Select(n => n.Value), Contains.Item("I") );
            Assert.That(nodes.Select(n => n.Value), Contains.Item("we") );
            
        }
        
        [Test]
        public void TestTree3()
        {
            var nodes = new List<string>() { "I love Kira", "we hate you",  "I love you"  }
                .Select(line => line.Split(" "))
                .ToDigraph(true);
            
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(nodes.Select(n => n.Value), Contains.Item("I") );
            Assert.That(nodes.Select(n => n.Value), Contains.Item("we") );
            
        }
        
        [Test]
        public void TestTree4()
        {
            var nodes = new List<string>() { "I love Kira", "love conquers all", "conquers her land" }
                .Select(line => line.Split(" "))
                .ToDigraph(true);
            
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(nodes.Select(n => n.Value), Contains.Item("I") );
            
        }
        
        [Test]
        public void TestTree5()
        {
            var nodes = new List<string>() { "love conquers all", "I love Kira", "conquers her land" }
                .Select(line => line.Split(" "))
                .ToDigraph(true)
                .Flatten();
            
            Assert.That(nodes, Is.Not.Empty);
            
        }
        
        [Test]
        public void TestCycle()
        {
            var nodes = new List<string>() { "JT loves Kira", "Kira loves JT" }
                .Select(line => line.Split(" "))
                .ToDigraph(true)
                .Select(n => n.Value);
            
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(nodes, Contains.Item("JT") );
        }
        
        [Test]
        public void TestCycleFlatten()
        {
            var nodes = new List<string>() { "JT loves Kira", "Kira loves JT" }
                .Select(line => line.Split(" "))
                .ToDigraph(true)
                .Flatten();
            
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(nodes, Is.EquivalentTo(new []{"JT", "loves", "Kira", "loves2", "JT2"})); 
        }
        
        [Test]
        public void TestCycleFlattenReverse()
        {
            var nodes = new List<string>() { "Kira loves JT",  "JT loves Kira", }
                .Select(line => line.Split(" "))
                .ToDigraph(true)
                .Flatten();
            
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(nodes, Is.EquivalentTo(new []{"Kira", "loves", "JT", "loves2", "Kira2"})); 
        }
        
        [Test]
        public void TestCycleFlatten2()
        {
            var nodes = new List<string>() { "JT loves his Kira", "his Kira really wants to like JT" }
                .Select(line => line.Split(" "))
                .ToDigraph(true)
                .Flatten();
            
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(nodes, Is.EquivalentTo(new []{"JT", "loves", "his", "Kira", "really", "wants", "to", "like", "JT2"})); 
        }
        
        [Test]
        public void TestFlattenTree3()
        {
            var nodes = new List<string>() { "I love Kira", "we hate you",  "I love you"  }
                .Select(line => line.Split(" "))
                .ToDigraph(true)
                .Flatten();
            
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(nodes, Contains.Item("I") );
            Assert.That(nodes, Contains.Item("we") );
            Assert.That(nodes, Contains.Item("Kira") );
            Assert.That(nodes, Contains.Item("hate") );
            Assert.That(nodes, Contains.Item("love") );
            Assert.That(nodes, Contains.Item("you") );
            
        }

        [Test]
        public void TestSentances()
        {
            var digraph = Program.ReadLines("sentances.txt")
                //tokenize
                .Select(line => line.Split(" "))
                .ToDigraph(true);
            var str = digraph.Flatten().Select(n => n.Value);

            var tokens = Program.ReadLines("sentances.txt")
                //tokenize
                .Select(line => line.Split(" "));

            foreach (var line in tokens)
            {
                int index = 0;
                foreach (var token in line)
                {
                   int i = str.Skip(index).ToList().IndexOf(token);
                   if (i == -1)
                   {
                       i = str.Skip(index).ToList().IndexOf(token +"2");
                       if (i == -1)
                       {
                           i = str.Skip(index).ToList().IndexOf(token +"3");
                       }
                   }
                   Assert.That(i, Is.GreaterThan(-1), $"can't find word {token}");
                   index = i + index;
                }
            }
        }
        
        [Test]
        public void TestCycleFlatten3()
        {
            var nodes = new List<string>() { "JT loves his awesome Kira", "what loves a really super awesome dude" }
                .Select(line => line.Split(" "))
                .ToDigraph(true)
                .Flatten();
            
            Assert.That(nodes.ToArray(), Is.Not.Empty);
        }
        
        [Test]
        public void TestCycleFlatten4()
        {
            var nodes = new List<string>() { "A B C D", "X Y C A", "H I C", "Z U I", "P O H"}
                .Select(line => line.Split(" "))
                .ToDigraph(true)
                .Flatten();
            
            Assert.That(nodes.ToArray(), Is.Not.Empty);
        }
    }
}