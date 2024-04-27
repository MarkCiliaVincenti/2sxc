﻿using System.Collections.Generic;
using ToSic.Sxc.Data;

namespace ToSic.Sxc.Tests.DataTests.DynStack
{
    internal class TypedStackTestData
    {
        public static object Anon1 => new
        {
            Key1 = "hello",
            Key2 = "goodbye",
            Deep1 = new
            {
                Sub1 = "hello deep1.sub",
            }
        };
        public static List<PropInfo> Anon1PropInfo => new()
        {
            new PropInfo("Key1", true, true, "hello"),
            new PropInfo("Key2", true, true, "goodbye"),
            new PropInfo("Deep1", true, true),
            new PropInfo("Deep1.Sub1", true, true, "hello"),
        };

        public static object Anon2 => new
        {
            Key1 = "hello 2",
            Part1 = "part1-text",
            Part2 = "test",
            Deep = new
            {
                Deeper = new
                {
                    Value = "hello deep.deeper.value",
                },
            },
        };

        public static string ValueNotTestable = "value-not-testable";


        public static List<PropInfo> StackOrder12PropInfo => new()
        {
            new PropInfo("Key1", true, true, "hello"),
            new PropInfo("dummy", false),
            new PropInfo("Part1", true, hasData: true, value: "part1-text"),
            new PropInfo("Deep1", true, hasData: true, value: ValueNotTestable),
            new PropInfo("Deep1.Sub1", true, true, "hello deep1.sub"),
            new PropInfo("Deep", true, hasData : true, value: ValueNotTestable),
            new PropInfo("Deep.Deeper", true, hasData : true, value: ValueNotTestable),
            new PropInfo("Deep.NotDeeper", false),
            new PropInfo("Deep.NotDeeper.ReallyNot", false),
            new PropInfo("Deep.Deeper.Value", true, hasData : true, value: "hello deep.deeper.value"),
            new PropInfo("Deep.Deeper.NotValue", false)
        };


        public static ITypedStack GetStackForKeysUsingAnon(DynAndTypedTestsBase parent)
        {
            var part1 = parent.Obj2Typed(Anon1);
            var part2 = parent.Obj2Typed(Anon2);
            var stack = parent.Factory.AsStack(new[] { part1, part2 });
            return stack;
        }
        public static ITypedStack GetStackForKeysUsingJson(DynAndTypedTestsBase parent)
        {
            var part1 = parent.Obj2Json2TypedStrict(Anon1);
            var part2 = parent.Obj2Json2TypedStrict(Anon2);
            var stack = parent.Factory.AsStack(new[] { part1, part2 });
            return stack;
        }

    }
}