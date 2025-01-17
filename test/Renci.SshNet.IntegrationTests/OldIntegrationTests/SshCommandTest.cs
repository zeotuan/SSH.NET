﻿using System.Diagnostics;

using Renci.SshNet.Common;

namespace Renci.SshNet.IntegrationTests.OldIntegrationTests
{
    /// <summary>
    /// Represents SSH command that can be executed.
    /// </summary>
    [TestClass]
    public class SshCommandTest : IntegrationTestBase
    {
        [TestMethod]
        public void Test_Run_SingleCommand()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                #region Example SshCommand RunCommand Result
                client.Connect();

                var testValue = Guid.NewGuid().ToString();
                var command = client.RunCommand(string.Format("echo {0}", testValue));
                var result = command.GetResult();
                result = result.Substring(0, result.Length - 1);    //  Remove \n character returned by command

                client.Disconnect();
                #endregion

                Assert.IsTrue(result.Equals(testValue));
            }
        }

        [TestMethod]
        public void Test_Execute_SingleCommand()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                #region Example SshCommand CreateCommand Execute
                client.Connect();

                var testValue = Guid.NewGuid().ToString();
                var command = string.Format("echo {0}", testValue);
                var cmd = client.CreateCommand(command);
                var result = cmd.Execute();
                result = result.Substring(0, result.Length - 1);    //  Remove \n character returned by command

                client.Disconnect();
                #endregion

                Assert.IsTrue(result.Equals(testValue));
            }
        }

        [TestMethod]
        public async Task Test_Execute_SingleCommandAsync()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                #region Example SshCommand CreateCommand ExecuteAsync
                using var cts = new CancellationTokenSource();
                await client.ConnectAsync(cts.Token);
                var testValue = Guid.NewGuid().ToString();
                using var cmd = client.CreateCommand($"echo {testValue}");
                await cmd.ExecuteAsync(cts.Token);
                client.Disconnect();
                Assert.IsTrue(cmd.GetResult().Trim().Equals(testValue));
                #endregion
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Test_Execute_SingleCommandAsync_WithCancelledToken()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                #region Example SshCommand CreateCommand ExecuteAsync With Cancelled Token
                using var cts = new CancellationTokenSource();
                await client.ConnectAsync(cts.Token);
                var command = $"/bin/sleep 5; echo {Guid.NewGuid().ToString()}";
                using var cmd = client.CreateCommand(command);
                cts.CancelAfter(100);
                await cmd.ExecuteAsync(cts.Token).ConfigureAwait(false);
                client.Disconnect();
                #endregion
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void Test_CancelAsync_Running_Command()
        {
            using var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password);
            #region Example SshCommand CancelAsync
            client.Connect();

            var testValue = Guid.NewGuid().ToString();
            var command = $"sleep 10s; echo {testValue}";
            using var cmd = client.CreateCommand(command);
            try
            {
                var asyncResult = cmd.BeginExecute();
                cmd.CancelAsync();
                cmd.EndExecute(asyncResult);
            }
            catch (OperationCanceledException)
            {
            }

            Assert.AreNotEqual(cmd.GetResult().Trim(), testValue);

            client.Disconnect();
            #endregion

        }

        [TestMethod]
        public void Test_Execute_OutputStream()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                #region Example SshCommand CreateCommand Execute OutputStream
                client.Connect();

                var cmd = client.CreateCommand("ls -l");   //  very long list
                var asynch = cmd.BeginExecute();

                var reader = new StreamReader(cmd.OutputStream);

                while (!asynch.IsCompleted)
                {
                    var result = reader.ReadToEnd();
                    if (string.IsNullOrEmpty(result))
                    {
                        continue;
                    }

                    Console.Write(result);
                }

                _ = cmd.EndExecute(asynch);

                client.Disconnect();
                #endregion

                Assert.Inconclusive();
            }
        }

        [TestMethod]
        public void Test_Execute_ExtendedOutputStream()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                #region Example SshCommand CreateCommand Execute ExtendedOutputStream

                client.Connect();
                var cmd = client.CreateCommand("echo 12345; echo 654321 >&2");
                var result = cmd.Execute();

                Console.Write(result);

                var reader = new StreamReader(cmd.ExtendedOutputStream);
                Console.WriteLine("DEBUG:");
                Console.Write(reader.ReadToEnd());

                client.Disconnect();

                #endregion

                Assert.Inconclusive();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(SshOperationTimeoutException))]
        public void Test_Execute_Timeout()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                #region Example SshCommand CreateCommand Execute CommandTimeout
                client.Connect();
                var cmd = client.CreateCommand("sleep 10s");
                cmd.CommandTimeout = TimeSpan.FromSeconds(5);
                cmd.Execute();
                client.Disconnect();
                #endregion
            }
        }

        [TestMethod]
        public void Test_Execute_Infinite_Timeout()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();
                var cmd = client.CreateCommand("sleep 10s");
                cmd.Execute();
                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_Execute_InvalidCommand()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();

                var cmd = client.CreateCommand(";");
                cmd.Execute();
                if (string.IsNullOrEmpty(cmd.GetError()))
                {
                    Assert.Fail("Operation should fail");
                }
                Assert.IsTrue(cmd.ExitStatus > 0);

                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_Execute_InvalidCommand_Then_Execute_ValidCommand()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();
                var cmd = client.CreateCommand(";");
                cmd.Execute();
                if (string.IsNullOrEmpty(cmd.GetError()))
                {
                    Assert.Fail("Operation should fail");
                }
                Assert.IsTrue(cmd.ExitStatus > 0);

                var result = ExecuteTestCommand(client);

                client.Disconnect();

                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public void Test_Execute_Command_with_ExtendedOutput()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();
                var cmd = client.CreateCommand("echo 12345; echo 654321 >&2");
                cmd.Execute();

                //var extendedData = Encoding.ASCII.GetString(cmd.ExtendedOutputStream.ToArray());
                var extendedData = new StreamReader(cmd.ExtendedOutputStream, Encoding.ASCII).ReadToEnd();
                client.Disconnect();

                Assert.AreEqual("12345\n", cmd.GetResult());
                Assert.AreEqual("654321\n", extendedData);
            }
        }

        [TestMethod]
        public void Test_Execute_Command_Reconnect_Execute_Command()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();
                var result = ExecuteTestCommand(client);
                Assert.IsTrue(result);

                client.Disconnect();
                client.Connect();
                result = ExecuteTestCommand(client);
                Assert.IsTrue(result);
                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_Execute_Command_ExitStatus()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                #region Example SshCommand RunCommand ExitStatus
                client.Connect();

                var cmd = client.RunCommand("exit 128");

                Console.WriteLine(cmd.ExitStatus);

                client.Disconnect();
                #endregion

                Assert.IsTrue(cmd.ExitStatus == 128);
            }
        }

        [TestMethod]
        public void Test_Execute_Command_Asynchronously()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();

                var cmd = client.CreateCommand("sleep 5s; echo 'test'");
                var asyncResult = cmd.BeginExecute(null, null);
                while (!asyncResult.IsCompleted)
                {
                    Thread.Sleep(100);
                }

                cmd.EndExecute(asyncResult);

                Assert.IsTrue(cmd.GetResult() == "test\n");

                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_Execute_Command_Asynchronously_With_Error()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();

                var cmd = client.CreateCommand("sleep 5s; ;");
                var asyncResult = cmd.BeginExecute(null, null);
                while (!asyncResult.IsCompleted)
                {
                    Thread.Sleep(100);
                }

                cmd.EndExecute(asyncResult);

                Assert.IsFalse(string.IsNullOrEmpty(cmd.GetError()));

                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_Execute_Command_Asynchronously_With_Callback()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();

                var callbackCalled = false;

                var cmd = client.CreateCommand("sleep 5s; echo 'test'");
                var asyncResult = cmd.BeginExecute(new AsyncCallback((s) =>
                {
                    callbackCalled = true;
                }), null);

                cmd.EndExecute(asyncResult);

                Thread.Sleep(100);

                Assert.IsTrue(callbackCalled);

                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_Execute_Command_Asynchronously_With_Callback_On_Different_Thread()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();

                var currentThreadId = Thread.CurrentThread.ManagedThreadId;
                int callbackThreadId = 0;

                var cmd = client.CreateCommand("sleep 5s; echo 'test'");
                var asyncResult = cmd.BeginExecute(new AsyncCallback((s) =>
                {
                    callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                }), null);

                cmd.EndExecute(asyncResult);

                Thread.Sleep(100);

                Assert.AreNotEqual(currentThreadId, callbackThreadId);

                client.Disconnect();
            }
        }

        /// <summary>
        /// Tests for Issue 563.
        /// </summary>
        [WorkItem(563), TestMethod]
        public void Test_Execute_Command_Same_Object_Different_Commands()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();
                var cmd = client.CreateCommand("echo 12345");
                cmd.Execute();
                Assert.AreEqual("12345\n", cmd.GetResult());
                cmd.Execute("echo 23456");
                Assert.AreEqual("23456\n", cmd.GetResult());
                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_Get_Result_Without_Execution()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();
                var cmd = client.CreateCommand("ls -l");

                Assert.IsTrue(string.IsNullOrEmpty(cmd.GetResult()));
                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_Get_Error_Without_Execution()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();
                var cmd = client.CreateCommand("ls -l");

                Assert.IsTrue(string.IsNullOrEmpty(cmd.GetError()));
                client.Disconnect();
            }
        }

        [WorkItem(703), TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_EndExecute_Before_BeginExecute()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();
                var cmd = client.CreateCommand("ls -l");
                cmd.EndExecute(null);
                client.Disconnect();
            }
        }

        /// <summary>
        ///A test for BeginExecute
        ///</summary>
        [TestMethod()]
        public void BeginExecuteTest()
        {
            string expected = "123\n";
            string result;

            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                #region Example SshCommand CreateCommand BeginExecute IsCompleted EndExecute

                client.Connect();

                var cmd = client.CreateCommand("sleep 15s;echo 123"); // Perform long running task

                var asynch = cmd.BeginExecute();

                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = cmd.EndExecute(asynch);
                client.Disconnect();

                #endregion

                Assert.IsNotNull(asynch);
                Assert.AreEqual(expected, result);
            }
        }

        [TestMethod]
        public void Test_Execute_Invalid_Command()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                #region Example SshCommand CreateCommand Error

                client.Connect();

                var cmd = client.CreateCommand(";");
                cmd.Execute();
                if (!string.IsNullOrEmpty(cmd.GetError()))
                {
                    Console.WriteLine(cmd.GetError());
                }

                client.Disconnect();

                #endregion

                Assert.Inconclusive();
            }
        }

        [TestMethod]

        public void Test_MultipleThread_100_MultipleConnections()
        {
            try
            {
                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 8
                };

                Parallel.For(0, 100, options,
                    () =>
                    {
                        var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password);
                        client.Connect();
                        return client;
                    },
                    (int counter, ParallelLoopState pls, SshClient client) =>
                    {
                        var result = ExecuteTestCommand(client);
                        Debug.WriteLine(string.Format("TestMultipleThreadMultipleConnections #{0}", counter));
                        Assert.IsTrue(result);
                        return client;
                    },
                    (SshClient client) =>
                    {
                        client.Disconnect();
                        client.Dispose();
                    }
                );
            }
            catch (Exception exp)
            {
                Assert.Fail(exp.ToString());
            }
        }

        [TestMethod]
        public void Test_MultipleThread_100_MultipleSessions()
        {
            using (var client = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password))
            {
                client.Connect();
                Parallel.For(0, 100,
                    (counter) =>
                    {
                        var result = ExecuteTestCommand(client);
                        Debug.WriteLine(string.Format("TestMultipleThreadMultipleConnections #{0}", counter));
                        Assert.IsTrue(result);
                    }
                );

                client.Disconnect();
            }
        }

        private static bool ExecuteTestCommand(SshClient s)
        {
            var testValue = Guid.NewGuid().ToString();
            var command = string.Format("echo {0}", testValue);
            var cmd = s.CreateCommand(command);
            var result = cmd.Execute();
            result = result.Substring(0, result.Length - 1);    //  Remove \n character returned by command
            return result.Equals(testValue);
        }
    }
}
