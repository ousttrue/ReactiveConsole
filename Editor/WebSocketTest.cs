using NUnit.Framework;


namespace ReactiveConsole
{
    public class WebSocketTests
    {
        [Test]
        public void WebSocketTest()
        {
            var accepted = HttpSession.AcceptWebSocketKey(Utf8String.From("dGhlIHNhbXBsZSBub25jZQ=="));

            Assert.AreEqual(Utf8String.From("s3pPLMBiTxaQ9kYGzzhZRbK+xOo="), accepted);
        }
    }
}
