namespace Renci.SshNet.IntegrationTests
{
    /// <summary>
    /// The SSH client integration tests
    /// </summary>
    [TestClass]
    public class SshClientTests : IntegrationTestBase, IDisposable
    {
        private readonly SshClient _sshClient;

        public SshClientTests()
        {
            _sshClient = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password);
            _sshClient.Connect();
        }

        [TestMethod]
        public void Echo_Command_with_all_characters()
        {
            var response = _sshClient.RunCommand("echo $'test !@#$%^&*()_+{}:,./<>[];\\|'");

            Assert.AreEqual("test !@#$%^&*()_+{}:,./<>[];\\|\n", response.GetResult());
        }

        [TestMethod]
        public void Send_InputStream_to_Command()
        {
            var inputByteArray = Encoding.UTF8.GetBytes("Hello world!");

            // Make the server echo back the input file with "cat"
            using (var command = _sshClient.CreateCommand("cat"))
            {
                var asyncResult = command.BeginExecute();

                using (var inputStream = command.CreateInputStream())
                {
                    inputStream.Write(inputByteArray, 0, inputByteArray.Length);
                }

                command.EndExecute(asyncResult);

                Assert.AreEqual("Hello world!", command.GetResult());
                Assert.AreEqual(string.Empty, command.GetError());
            }
        }

        [TestMethod]
        public void Send_InputStream_to_Command_OneByteAtATime()
        {
            var inputByteArray = Encoding.UTF8.GetBytes("Hello world!");

            // Make the server echo back the input file with "cat"
            using (var command = _sshClient.CreateCommand("cat"))
            {
                var asyncResult = command.BeginExecute();

                using (var inputStream = command.CreateInputStream())
                {
                    for (var i = 0; i < inputByteArray.Length; i++)
                    {
                        inputStream.WriteByte(inputByteArray[i]);
                    }
                }

                command.EndExecute(asyncResult);

                Assert.AreEqual("Hello world!", command.GetResult());
                Assert.AreEqual(string.Empty, command.GetError());
            }
        }

        [TestMethod]
        public void Send_InputStream_to_Command_InputStreamNotInUsingBlock_StillWorks()
        {
            var inputByteArray = Encoding.UTF8.GetBytes("Hello world!");

            // Make the server echo back the input file with "cat"
            using (var command = _sshClient.CreateCommand("cat"))
            {
                var asyncResult = command.BeginExecute();

                var inputStream = command.CreateInputStream();
                for (var i = 0; i < inputByteArray.Length; i++)
                {
                    inputStream.WriteByte(inputByteArray[i]);
                }

                command.EndExecute(asyncResult);

                Assert.AreEqual("Hello world!", command.GetResult());
                Assert.AreEqual(string.Empty, command.GetError());
            }
        }

        [TestMethod]
        public void CreateInputStream_BeforeBeginExecute_ThrowsInvalidOperationException()
        {
            var command = _sshClient.CreateCommand("ls");

            Assert.ThrowsException<InvalidOperationException>(command.CreateInputStream);
        }

        [TestMethod]
        public void CreateInputStream_AfterEndExecute_ThrowsInvalidOperationException()
        {
            var command = _sshClient.CreateCommand("ls");
            var asyncResult = command.BeginExecute();
            command.EndExecute(asyncResult);

            Assert.ThrowsException<InvalidOperationException>(command.CreateInputStream);
        }

        public void Dispose()
        {
            _sshClient.Disconnect();
            _sshClient.Dispose();
        }
    }
}
