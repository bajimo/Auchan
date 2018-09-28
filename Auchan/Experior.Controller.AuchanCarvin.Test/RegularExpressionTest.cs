using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace Experior.Controller.AuchanCarvin.Test
{
    [TestClass]
    public class RegularExpressionTest
    {
        Regex regexCCPA00LU00 = new Regex(@"^CCPA0[1-8]LU0[1|2]$"); // eg. CCPA01LU01, CCPA01LU02 - CCPA08LU01, CCPA08LU02
        Regex regexCCDE00LU00 = new Regex(@"^CCDE0[1-8]LU0[1|2]$"); // eg. CCDE01LU01, CCDE01LU02 - CCDE08LU01, CCDE08LU02
        Regex regexCCDE00WS00 = new Regex(@"^CCDE0[1-8]WS0[1|2]$"); // eg. CCDE01WS01, CCDE01WS02 - CCDE08WS01, CCDE08WS02
        Regex regexCCDE00CC00 = new Regex(@"^CCDE0[1-8]CC0[1|2]$"); // eg. CCDE01CC01, CCDE01CC02 - CCDE08CC01, CCDE08CC02

        [TestMethod]
        public void MatchCCPA00LU00Test()
        {
            // Check for all possible matches

            Match matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA01LU01");
            Assert.IsTrue(matchCCPA00LU00.Success);
            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA01LU02");
            Assert.IsTrue(matchCCPA00LU00.Success);

            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA02LU01");
            Assert.IsTrue(matchCCPA00LU00.Success);
            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA02LU02");
            Assert.IsTrue(matchCCPA00LU00.Success);

            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA03LU01");
            Assert.IsTrue(matchCCPA00LU00.Success);
            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA03LU02");
            Assert.IsTrue(matchCCPA00LU00.Success);

            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA04LU01");
            Assert.IsTrue(matchCCPA00LU00.Success);
            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA04LU02");
            Assert.IsTrue(matchCCPA00LU00.Success);

            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA05LU01");
            Assert.IsTrue(matchCCPA00LU00.Success);
            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA05LU02");
            Assert.IsTrue(matchCCPA00LU00.Success);

            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA06LU01");
            Assert.IsTrue(matchCCPA00LU00.Success);
            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA06LU02");
            Assert.IsTrue(matchCCPA00LU00.Success);

            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA07LU01");
            Assert.IsTrue(matchCCPA00LU00.Success);
            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA07LU02");
            Assert.IsTrue(matchCCPA00LU00.Success);

            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA08LU01");
            Assert.IsTrue(matchCCPA00LU00.Success);
            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA08LU02");
            Assert.IsTrue(matchCCPA00LU00.Success);

            // Check for non matches too

            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA09LU01"); // Aisle 9 doesn't exist
            Assert.IsFalse(matchCCPA00LU00.Success);

            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA01LU03"); // Position 2 doesn't exist
            Assert.IsFalse(matchCCPA00LU00.Success);

            matchCCPA00LU00 = regexCCPA00LU00.Match("CCPA01LU01EXTRACHARS");
            Assert.IsFalse(matchCCPA00LU00.Success);

            matchCCPA00LU00 = regexCCPA00LU00.Match("COMPLETELY-UNMATCHED");
            Assert.IsFalse(matchCCPA00LU00.Success);
        }

        [TestMethod]
        public void MatchCCDE00LU00Test()
        {
            // Check for all possible matches

            Match matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE01LU01");
            Assert.IsTrue(matchCCDE00LU00.Success);
            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE01LU02");
            Assert.IsTrue(matchCCDE00LU00.Success);

            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE02LU01");
            Assert.IsTrue(matchCCDE00LU00.Success);
            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE02LU02");
            Assert.IsTrue(matchCCDE00LU00.Success);

            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE03LU01");
            Assert.IsTrue(matchCCDE00LU00.Success);
            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE03LU02");
            Assert.IsTrue(matchCCDE00LU00.Success);

            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE04LU01");
            Assert.IsTrue(matchCCDE00LU00.Success);
            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE04LU02");
            Assert.IsTrue(matchCCDE00LU00.Success);

            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE05LU01");
            Assert.IsTrue(matchCCDE00LU00.Success);
            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE05LU02");
            Assert.IsTrue(matchCCDE00LU00.Success);

            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE06LU01");
            Assert.IsTrue(matchCCDE00LU00.Success);
            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE06LU02");
            Assert.IsTrue(matchCCDE00LU00.Success);

            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE07LU01");
            Assert.IsTrue(matchCCDE00LU00.Success);
            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE07LU02");
            Assert.IsTrue(matchCCDE00LU00.Success);

            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE08LU01");
            Assert.IsTrue(matchCCDE00LU00.Success);
            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE08LU02");
            Assert.IsTrue(matchCCDE00LU00.Success);

            // Check for non matches too

            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE09LU01"); // Aisle 9 doesn't exist
            Assert.IsFalse(matchCCDE00LU00.Success);

            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE01LU03"); // Position 2 doesn't exist
            Assert.IsFalse(matchCCDE00LU00.Success);

            matchCCDE00LU00 = regexCCDE00LU00.Match("CCDE01LU01EXTRACHARS");
            Assert.IsFalse(matchCCDE00LU00.Success);

            matchCCDE00LU00 = regexCCDE00LU00.Match("COMPLETELY-UNMATCHED");
            Assert.IsFalse(matchCCDE00LU00.Success);
        }

        [TestMethod]
        public void MatchCCDE00WS00Test()
        {
            // Check for all possible matches

            Match matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE01WS01");
            Assert.IsTrue(matchCCDE00WS00.Success);
            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE01WS02");
            Assert.IsTrue(matchCCDE00WS00.Success);

            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE02WS01");
            Assert.IsTrue(matchCCDE00WS00.Success);
            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE02WS02");
            Assert.IsTrue(matchCCDE00WS00.Success);

            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE03WS01");
            Assert.IsTrue(matchCCDE00WS00.Success);
            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE03WS02");
            Assert.IsTrue(matchCCDE00WS00.Success);

            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE04WS01");
            Assert.IsTrue(matchCCDE00WS00.Success);
            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE04WS02");
            Assert.IsTrue(matchCCDE00WS00.Success);

            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE05WS01");
            Assert.IsTrue(matchCCDE00WS00.Success);
            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE05WS02");
            Assert.IsTrue(matchCCDE00WS00.Success);

            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE06WS01");
            Assert.IsTrue(matchCCDE00WS00.Success);
            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE06WS02");
            Assert.IsTrue(matchCCDE00WS00.Success);

            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE07WS01");
            Assert.IsTrue(matchCCDE00WS00.Success);
            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE07WS02");
            Assert.IsTrue(matchCCDE00WS00.Success);

            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE08WS01");
            Assert.IsTrue(matchCCDE00WS00.Success);
            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE08WS02");
            Assert.IsTrue(matchCCDE00WS00.Success);

            // Check for non matches too

            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE09WS01"); // Aisle 9 doesn't exist
            Assert.IsFalse(matchCCDE00WS00.Success);

            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE01WS03"); // Position 2 doesn't exist
            Assert.IsFalse(matchCCDE00WS00.Success);

            matchCCDE00WS00 = regexCCDE00WS00.Match("CCDE01WS01EXTRACHARS");
            Assert.IsFalse(matchCCDE00WS00.Success);

            matchCCDE00WS00 = regexCCDE00WS00.Match("COMPLETELY-UNMATCHED");
            Assert.IsFalse(matchCCDE00WS00.Success);
        }

        [TestMethod]
        public void MatchCCDE00CC00Test()
        {
            // Check for all possible matches

            Match matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE01CC01");
            Assert.IsTrue(matchCCDE00CC00.Success);
            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE01CC02");
            Assert.IsTrue(matchCCDE00CC00.Success);

            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE02CC01");
            Assert.IsTrue(matchCCDE00CC00.Success);
            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE02CC02");
            Assert.IsTrue(matchCCDE00CC00.Success);

            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE03CC01");
            Assert.IsTrue(matchCCDE00CC00.Success);
            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE03CC02");
            Assert.IsTrue(matchCCDE00CC00.Success);

            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE04CC01");
            Assert.IsTrue(matchCCDE00CC00.Success);
            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE04CC02");
            Assert.IsTrue(matchCCDE00CC00.Success);

            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE05CC01");
            Assert.IsTrue(matchCCDE00CC00.Success);
            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE05CC02");
            Assert.IsTrue(matchCCDE00CC00.Success);

            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE06CC01");
            Assert.IsTrue(matchCCDE00CC00.Success);
            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE06CC02");
            Assert.IsTrue(matchCCDE00CC00.Success);

            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE07CC01");
            Assert.IsTrue(matchCCDE00CC00.Success);
            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE07CC02");
            Assert.IsTrue(matchCCDE00CC00.Success);

            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE08CC01");
            Assert.IsTrue(matchCCDE00CC00.Success);
            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE08CC02");
            Assert.IsTrue(matchCCDE00CC00.Success);

            // Check for non matches too

            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE09CC01"); // Aisle 9 doesn't exist
            Assert.IsFalse(matchCCDE00CC00.Success);

            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE01CC03"); // Position 2 doesn't exist
            Assert.IsFalse(matchCCDE00CC00.Success);

            matchCCDE00CC00 = regexCCDE00CC00.Match("CCDE01CC01EXTRACHARS");
            Assert.IsFalse(matchCCDE00CC00.Success);

            matchCCDE00CC00 = regexCCDE00CC00.Match("COMPLETELY-UNMATCHED");
            Assert.IsFalse(matchCCDE00CC00.Success);
        }
    }
}
