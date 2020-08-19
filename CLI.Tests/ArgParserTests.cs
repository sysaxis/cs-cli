using NUnit.Framework;

namespace CLI.Tests
{
    public class ArgParserTests
    {
        ArgsParser Parser { get; set; }

        [Test]
        public void UnquoteArgVal()
        {
            int count;
            string text;
            string[] args;

            args = new string[] { "text" };
            count = ArgsParser.UnquoteArgVal(args, out text);
            Assert.AreEqual(count, 0);
            Assert.AreEqual(text, null);

            args = new string[] { "\"hello", "my", "dear", "friend\"", "---" };
            count = ArgsParser.UnquoteArgVal(args, out text);
            Assert.AreEqual(count, 4);
            Assert.AreEqual(text, "hello my dear friend");

            args = new string[] { "msg=\"hello", "friend\"" };
            count = ArgsParser.UnquoteArgVal(args, out text, offset: 4);
            Assert.AreEqual(count, 2);
            Assert.AreEqual(text, "hello friend");

            args = new string[] { "'Dwayne", "\"The", "Rock\"", "Johnson'" };
            count = ArgsParser.UnquoteArgVal(args, out text);
            Assert.AreEqual(count, 4);
            Assert.AreEqual(text, "Dwayne \"The Rock\" Johnson");

            args = new string[] { "i", "see", "what", "you", "'did", "there'" };
            count = ArgsParser.UnquoteArgVal(args, out text, startIndex: 4);
            Assert.AreEqual(count, 2);
            Assert.AreEqual(text, "did there");
        }

        private void AssertContains(ArgKvp[] actual, params ArgKvp[] expected)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int k = 0; k < actual.Length; k++)
            {
                Assert.AreEqual(expected[k].Key, actual[k].Key);
                Assert.AreEqual(expected[k].Value, actual[k].Value);
            }
        }

        [Test]
        public void Parse()
        {
            Parser = new ArgsParser(new ArgsParserOptions
            {
                ArgPrefixes = new string[] { "--", "-" },
                KeyValOps = new string[] { "=" }
            });

            ArgKvp[] args;

            args = Parser.Parse("example");
            Assert.AreEqual(args.Length, 0);

            args = Parser.Parse("example1 example 2");
            Assert.AreEqual(args.Length, 0);

            args = Parser.Parse(" -a 12 --b=\"hello friend\"");
            AssertContains(args, new ArgKvp
            {
                Key = "a",
                Value = "12"
            }, new ArgKvp
            {
                Key = "b",
                Value = "hello friend"
            });

            args = Parser.Parse("-b hello -c friend");
            AssertContains(args, new ArgKvp
            {
                Key = "b",
                Value = "hello"
            }, new ArgKvp
            {
                Key = "c",
                Value = "friend"
            });

            args = Parser.Parse("--q='how come?'");
            AssertContains(args, new ArgKvp
            {
                Key = "q",
                Value = "how come?"
            });

            args = Parser.Parse("--myflag -o 12 -c abc");
            AssertContains(args, new ArgKvp
            {
                Key = "myflag",
                Value = null
            }, new ArgKvp
            {
                Key = "o",
                Value = "12"
            }, new ArgKvp
            {
                Key = "c",
                Value = "abc"
            });


            args = Parser.Parse("-o 12 -c abc --myflag");
            AssertContains(args, new ArgKvp
            {
                Key = "o",
                Value = "12"
            }, new ArgKvp
            {
                Key = "c",
                Value = "abc"
            }, new ArgKvp
            {
                Key = "myflag",
                Value = null
            });

            args = Parser.Parse("-o 12 -c abc -myflag");
            AssertContains(args, new ArgKvp
            {
                Key = "o",
                Value = "12"
            }, new ArgKvp
            {
                Key = "c",
                Value = "abc"
            }, new ArgKvp
            {
                Key = "myflag",
                Value = null
            });


            Parser = new ArgsParser(new ArgsParserOptions
            {
                ArgPrefixes = new string[] { "--", "-" }
            });

            args = Parser.Parse("-o 12 -c abc --myflag -d=12");
            AssertContains(args, new ArgKvp
            {
                Key = "o",
                Value = "12"
            }, new ArgKvp
            {
                Key = "c",
                Value = "abc"
            }, new ArgKvp
            {
                Key = "myflag",
                Value = null
            }, new ArgKvp
            {
                Key = "d=12",
                Value = null
            });

            Parser = new ArgsParser(new ArgsParserOptions
            {
                KeyValOps = new string[] { "=" }
            });

            args = Parser.Parse("-o 12 -c abc --myflag");
            Assert.AreEqual(args.Length, 0);

            args = Parser.Parse("-o=12 -c=abc --myflag=");
            AssertContains(args, new ArgKvp
            {
                Key = "-o",
                Value = "12"
            }, new ArgKvp
            {
                Key = "-c",
                Value = "abc"
            }, new ArgKvp
            {
                Key = "--myflag",
                Value = ""
            });


            Parser = new ArgsParser(new ArgsParserOptions
            {
                KeyValOps = new string[] { ":" },
                ArgSeparator = ','
            });

            args = Parser.Parse("o: 12, c: abc, myflag:");
            AssertContains(args, new ArgKvp
            {
                Key = "o",
                Value = "12"
            }, new ArgKvp
            {
                Key = "c",
                Value = "abc"
            }, new ArgKvp
            {
                Key = "myflag",
                Value = ""
            });
        }
    }
}