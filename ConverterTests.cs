using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Virinco.WATS.Interface;
using Xunit;
using Xunit.Abstractions;
using WATS.Testing;
using Virinco.WATS.Converter.Klippel;

namespace Virinco.WATS.Converter.Klippel.Tests
{
    public class ConverterTests : ConverterTestBase
    {
        public ConverterTests(ITestOutputHelper output) : base(output) { }
        protected override IReportConverter_v2 CreateConverter() => new KlippelLogConverter();

        // Klippel's Data folder has root .txt files (the main inputs) plus
        // matching subdirectories (component files read by the converter).
        // Only the root-level .txt files should be driven through the test runner.
        protected override IEnumerable<string> GetDataFiles()
        {
            var dataDir = Path.Combine(AppContext.BaseDirectory, DataDirectory);
            if (!Directory.Exists(dataDir))
                return Enumerable.Empty<string>();
            return Directory.GetFiles(dataDir, "*.txt", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f);
        }

        [Fact, Trait("TestMode", "ConvertOnly")]
        public void ConvertOnly_AllFiles() => RunAllFiles(TestMode.ConvertOnly);

        [Fact, Trait("TestMode", "ConvertAndValidate")]
        public void ConvertAndValidate_AllFiles() => RunAllFiles(TestMode.ConvertAndValidate);

        [Fact, Trait("TestMode", "ConvertAndSubmit"), Trait("RequiresServer", "true")]
        public void ConvertAndSubmit_AllFiles() => RunAllFiles(TestMode.ConvertAndSubmit);
    }
}
