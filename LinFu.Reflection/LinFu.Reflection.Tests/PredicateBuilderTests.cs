using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using LinFu.Common;
using NUnit.Framework;

namespace LinFu.Reflection.Tests
{
    [TestFixture]
    public class PredicateBuilderTests : BaseFixture
    {
        private MethodInfo targetMethod;
        private PredicateBuilder builder;
        protected override void OnInit()
        {
            targetMethod = typeof(MethodFinderTargetDummy).GetMethod("DoSomething");
            builder = new PredicateBuilder();
        }
        protected override void OnTerm()
        {
            builder = null;
        }
        [Test]
        public void ShouldMatchMethodName()
        {
            builder.MethodName = "DoSomething";
            builder.MatchParameters = false;
            RunTest();
        }

        [Test]
        public void ShouldMatchReturnType()
        {
            builder.ReturnType = targetMethod.ReturnType;
            builder.MatchParameters = false;
            RunTest();
        }

        [Test]
        public void ShouldMatchParameterTypes()
        {
            builder.SetParameterTypes(targetMethod.GetParameters());
            RunTest();
        }

        [Test]
        public void ShouldMatchGenericParameters()
        {
            targetMethod = typeof(MethodFinderTargetDummy).GetMethod("DoSomethingGeneric");
            Assert.IsTrue(targetMethod.IsGenericMethodDefinition);

            Type typeArgument = typeof(int);
            targetMethod = targetMethod.MakeGenericMethod(typeArgument);

            builder.TypeArguments.Add(typeArgument);
            RunTest();
        }

        [Test]
        public void ShouldMatchPublicMethod()
        {
            builder.IsPublic = true;
            builder.MatchParameters = false;
            RunTest();
        }

        [Test]
        public void ShouldMatchProtectedMethod()
        {
            builder.IsProtected = false;
            builder.MatchParameters = false;
            RunTest();
        }

        [Test]
        public void ShouldMatchMethodBasedOnRuntimeArguments()
        {
            // The target method should have the following signature:
            // public void OverloadedMethod(int arg1, string arg2);
            Type[] argumentTypes = new Type[] { typeof(int), typeof(string) };
            targetMethod = typeof(MethodFinderTargetDummy)
                .GetMethod("OverloadedMethod", argumentTypes);

            // Specify the method name, and give two sample arguments
            // to use for the search
            builder.MethodName = "OverloadedMethod";
            builder.RuntimeArguments.Add(5);
            builder.RuntimeArguments.Add("test");
            builder.MatchRuntimeArguments = true;

            FindMatch();
        }



        [Test]
        public void ShouldMatchCovariantReturnType()
        {
            targetMethod = typeof(MethodFinderTargetDummy).GetMethod("ReturnConnection");

            builder.MethodName = "ReturnConnection";
            builder.ReturnType = typeof(SqlConnection);
            builder.MatchCovariantReturnType = true;

            FindMatch();
        }
        [Test]
        public void ShouldReturnMatchBasedOnReturnTypeAndParameterTypes()
        {
            targetMethod = typeof(MethodFinderTargetDummy).GetMethod("DoSomething");
            Type[] argumentTypes = new Type[] { typeof(int), typeof(int) };
            
            builder.MethodName = "DoSomething";
            builder.ReturnType = typeof(void);
            builder.ArgumentTypes.AddRange(argumentTypes);
            builder.MatchParameterTypes = true;

            FindMatch(.66);
        }

        private void RunTest()
        {
            Predicate<MethodInfo> predicate = builder.CreatePredicate();
            Assert.IsNotNull(predicate);

            foreach (Predicate<MethodInfo> current in predicate.GetInvocationList())
            {
                Assert.IsTrue(current(targetMethod));
            }
        }
        private void FindMatch()
        {
            FindMatch(.51);
        }
        private void FindMatch(double tolerance)
        {
            // The builder should give a list of predicates
            // that match the target method
            Predicate<MethodInfo> predicate = builder.CreatePredicate();
            FuzzyFinder<MethodInfo> finder = new FuzzyFinder<MethodInfo>();
            finder.Tolerance = tolerance;

            MethodInfo[] methods =
                typeof(MethodFinderTargetDummy).GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                            BindingFlags.Instance);

            // Perform the search
            MethodInfo resultingMethod = finder.Find(predicate, methods);
            Assert.IsNotNull(resultingMethod);

            // The resulting method and the target method should be the same method
            Assert.AreEqual(targetMethod, resultingMethod);
        }
    }   
}
