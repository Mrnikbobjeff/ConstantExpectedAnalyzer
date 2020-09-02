using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace ConstantExpectedAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void EmptyText_NoDiagnostics()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }
        [TestMethod]
        public void ConstantPassed_NoDiagnostics()
        {
            var test = @"
    using System;
    namespace System
    {
        [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
        public class ConstantExpectedAttribute : Attribute
        {   
        }
    }
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void ExpectConstant([ConstantExpected] int x) {}
            public void Invoke() => ExpectConstant(1);
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void MethodInvoked_SingleDiagnostic()
        {
            var test = @"
    using System;
    namespace System
    {
        [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
        public class ConstantExpectedAttribute : Attribute
        {   
        }
    }
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void ExpectConstant([ConstantExpected] int x) {}
            public void Invoke() => ExpectConstant(new Random().Next(0,1));
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "ConstantExpectedAnalyzer",
                Message = "Invocation uses non-constant parameters resulting in worse codegen",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 52)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ConstantExpectedAnalyzerAnalyzer();
        }
    }
}
