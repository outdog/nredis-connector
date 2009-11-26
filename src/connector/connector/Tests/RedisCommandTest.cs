namespace Connector.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using NUnit.Framework;

    [TestFixture]
    public class RedisCommandTest
    {
        [Test]
        public void SetCommandWithNormalResult()
        {
            using (var connection = ConnectionMock.Create("+OK", "SET" , "foo", "3"))
            {
                var f = new CommandFactory(connection);
                RedisCommand command = f.Set("foo", "3");
                command.Exec();
                connection.Verify();
            }
        }
        [Test]
        public void GetCommandWithNormalResult()
        {
            using (var connection = ConnectionMock.Create("+baz", "GET", "foo"))
            {
                var f = new CommandFactory(connection);
                var command = f.Get("foo");
                command.Exec();
                Assert.That(Encoding.ASCII.GetString(command.Result), Is.EqualTo("baz"));
            }
        }
        [Test, ExpectedException(typeof(RedisException))]
        public void SetCommandWithError()
        {
            using (var connection = ConnectionMock.Create("-ERR Get off", "set", "foo", "3")) 
            {
                var f = new CommandFactory(connection);
                RedisCommand command = f.Set("foo", "3");
                command.Exec();
            }
        }
    }

    internal class ConnectionMock : RedisConnection
    {
        private readonly string _expectedCommand;

        private readonly BinaryReader _readerMock;

        private readonly BinaryWriter _writerMock;

        private readonly MemoryStream _incomingStream;

        public static ConnectionMock Create(string result, params string[] command)
        {
            var args = String.Join("_", command.Select(q => String.Format("${0}_{1}", q.Length, q)).ToArray());
            
            var commandString = String.Format("*{0}_{1}_", command.Length, args);
            return new ConnectionMock(commandString, result);
        }

        public ConnectionMock(string expectedCommand, string result)
        {
            _expectedCommand = expectedCommand;
            _readerMock = new BinaryReader(new MemoryStream(Encoding.ASCII.GetBytes(result.Replace("_", "\r\n"))));
            this._incomingStream = new MemoryStream();
            _writerMock = new BinaryWriter(this._incomingStream);
        }

        public void Verify()
        {
            var writtenCommand = Encoding.ASCII.GetString(_incomingStream.ToArray()).Replace("\r\n", "_");
            Assert.That(writtenCommand, Is.EqualTo(_expectedCommand));
        }

        public override System.IO.BinaryReader Reader
        {
            get
            {
                return _readerMock;
            }
        }

        public override System.IO.BinaryWriter Writer
        {
            get
            {
                return _writerMock;
            }
        }
    }
}