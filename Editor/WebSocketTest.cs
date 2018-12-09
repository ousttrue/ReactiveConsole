using NUnit.Framework;


namespace ReactiveConsole
{
    public class WebSocketTests
    {
        [Test]
        public void WebSocketTest()
        {
            var accepted = HttpSession.AcceptWebSocketKey(Utf8Bytes.From("dGhlIHNhbXBsZSBub25jZQ=="));

            Assert.AreEqual(Utf8Bytes.From("s3pPLMBiTxaQ9kYGzzhZRbK+xOo="), accepted);
        }
    }
}
