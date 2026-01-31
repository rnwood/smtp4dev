using System;
using LumiSoft.Net;
using LumiSoft.Net.IMAP;
using Xunit;

namespace New.LumiSoft.Net.Tests
{
    public class IMAP_Search_Key_Tests
    {
        [Fact]
        public void HEADER_With_MESSAGE_ID_Should_Parse_Correctly()
        {
            // This is the failing case from the issue
            string searchCommand = "HEADER MESSAGE-ID <20260106224430.Horde.Ok6GdEyWhiijOVj8ClvE4U8@example.local>";
            
            var reader = new StringReader(searchCommand);
            var searchKey = IMAP_Search_Key.ParseKey(reader);
            
            Assert.NotNull(searchKey);
            Assert.IsType<IMAP_Search_Key_Header>(searchKey);
            
            var headerKey = (IMAP_Search_Key_Header)searchKey;
            Assert.Equal("MESSAGE-ID", headerKey.FieldName);
            Assert.Equal("<20260106224430.Horde.Ok6GdEyWhiijOVj8ClvE4U8@example.local>", headerKey.Value);
        }

        [Fact]
        public void HEADER_With_Quoted_MESSAGE_ID_Should_Parse_Correctly()
        {
            // Test with quoted value
            string searchCommand = "HEADER MESSAGE-ID \"<20260106224430.Horde.Ok6GdEyWhiijOVj8ClvE4U8@example.local>\"";
            
            var reader = new StringReader(searchCommand);
            var searchKey = IMAP_Search_Key.ParseKey(reader);
            
            Assert.NotNull(searchKey);
            Assert.IsType<IMAP_Search_Key_Header>(searchKey);
            
            var headerKey = (IMAP_Search_Key_Header)searchKey;
            Assert.Equal("MESSAGE-ID", headerKey.FieldName);
            Assert.Equal("<20260106224430.Horde.Ok6GdEyWhiijOVj8ClvE4U8@example.local>", headerKey.Value);
        }
        
        [Fact]
        public void HEADER_With_Simple_Value_Should_Parse_Correctly()
        {
            // Test with a simple value
            string searchCommand = "HEADER Subject test";
            
            var reader = new StringReader(searchCommand);
            var searchKey = IMAP_Search_Key.ParseKey(reader);
            
            Assert.NotNull(searchKey);
            Assert.IsType<IMAP_Search_Key_Header>(searchKey);
            
            var headerKey = (IMAP_Search_Key_Header)searchKey;
            Assert.Equal("Subject", headerKey.FieldName);
            Assert.Equal("test", headerKey.Value);
        }
    }
}
