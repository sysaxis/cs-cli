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
            Assert.That(0, Is.EqualTo(count));
            Assert.That(null, Is.EqualTo(text));

            args = new string[] { "\"hello", "my", "dear", "friend\"", "---" };
            count = ArgsParser.UnquoteArgVal(args, out text);
            Assert.That(4, Is.EqualTo(count));
            Assert.That("hello my dear friend", Is.EqualTo(text));

            args = new string[] { "msg=\"hello", "friend\"" };
            count = ArgsParser.UnquoteArgVal(args, out text, offset: 4);
            Assert.That(2, Is.EqualTo(count));
            Assert.That("hello friend", Is.EqualTo(text));

            args = new string[] { "'Dwayne", "\"The", "Rock\"", "Johnson'" };
            count = ArgsParser.UnquoteArgVal(args, out text);
            Assert.That(4, Is.EqualTo(count));
            Assert.That("Dwayne \"The Rock\" Johnson", Is.EqualTo(text));

            args = new string[] { "i", "see", "what", "you", "'did", "there'" };
            count = ArgsParser.UnquoteArgVal(args, out text, startIndex: 4);
            Assert.That(2, Is.EqualTo(count));
            Assert.That("did there", Is.EqualTo(text));
        }

        private void AssertContains(ArgKvp[] actual, params ArgKvp[] expected)
        {
            Assert.That(expected.Length, Is.EqualTo(actual.Length));
            for (int k = 0; k < actual.Length; k++)
            {
                Assert.That(expected[k].Key, Is.EqualTo(actual[k].Key));
                Assert.That(expected[k].Value, Is.EqualTo(actual[k].Value));
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
            Assert.That(1, Is.EqualTo(args.Length));

            args = Parser.Parse("example1 example 2");
            Assert.That(3, Is.EqualTo(args.Length));

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

            args = Parser.Parse("200 -e 19 arg2");
            AssertContains(args, new ArgKvp
            {
                Key = "#1",
                Value = "200"
            }, new ArgKvp
            {
                Key = "e",
                Value = "19"
            },
            new ArgKvp
            {
                Key = "#2",
                Value = "arg2"
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
            Assert.That(5, Is.EqualTo(args.Length));

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