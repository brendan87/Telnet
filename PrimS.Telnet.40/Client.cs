namespace PrimS.Telnet
{
  using System;
  using System.Threading;

  //Referencing https://support.microsoft.com/kb/231866?wa=wsignin1.0 and http://www.codeproject.com/Articles/19071/Quick-tool-A-minimalistic-Telnet-library got me started

  /// <summary>
  /// Basic Telnet client
  /// </summary>
  public class Client : BaseClient
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="hostname">The hostname.</param>
    /// <param name="port">The port.</param>
    /// <param name="token">The token.</param>
    public Client(string hostname, int port, CancellationToken token)
      : base(new TcpByteStream(hostname, port), token)
    { }

    /// <summary>
    /// Tries to login asynchronously.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="loginTimeOutMs">The login time out ms.</param>
    /// <returns>True if successful.</returns>
    public bool TryLogin(string username, string password, int loginTimeOutMs)
    {
      try
      {
        if (this.IsTerminatedWith(loginTimeOutMs, ":"))
        {
          this.WriteLine(username);
          if (this.IsTerminatedWith(loginTimeOutMs, ":"))
          {
            this.WriteLine(password);
          }
          return this.IsTerminatedWith(loginTimeOutMs, ">");
        }
      }
      catch (Exception)
      {
        //NOP
      }
      return false;
    }

    private bool IsTerminatedWith(int loginTimeOutMs, string terminator)
    {
      return (this.TerminatedRead(terminator, TimeSpan.FromMilliseconds(loginTimeOutMs), 1)).TrimEnd().EndsWith(terminator);
    }

    /// <summary>
    /// Writes the line to the server.
    /// </summary>
    /// <param name="command">The command.</param>
    public void WriteLine(string command)
    {
      this.Write(string.Format("{0}\n", command));
    }

    /// <summary>
    /// Writes the specified command to the server.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns></returns>
    public void Write(string command)
    {
      if (this.byteStream.Connected && !this.internalCancellation.Token.IsCancellationRequested)
      {
        this.sendRateLimit.Wait(this.internalCancellation.Token);
        this.byteStream.Write(command);
        this.sendRateLimit.Release();
      }
    }

    /// <summary>
    /// Reads asynchronously from the stream.
    /// </summary>
    /// <returns>Any content retrieved.</returns>
    public string Read()
    {
      return this.Read(TimeSpan.FromMilliseconds(DefaultTimeOutMs));
    }

    /// <summary>
    /// Reads from the stream.
    /// </summary>
    /// <param name="timeout">The timeout.</param>
    /// <returns></returns>
    public string Read(TimeSpan timeout)
    {
      ByteStreamHandler handler = new ByteStreamHandler(this.byteStream, this.internalCancellation);
      return handler.Read(timeout);
    }

    /// <summary>
    /// Reads asynchronously from the stream, terminating as soon as the <see cref="terminator"/> is located.
    /// </summary>
    /// <param name="terminator">The terminator.</param>
    /// <returns></returns>
    public string TerminatedRead(string terminator)
    {
      return this.TerminatedRead(terminator, TimeSpan.FromMilliseconds(DefaultTimeOutMs));
    }

    /// <summary>
    /// Reads asynchronously from the stream, terminating as soon as the <see cref="terminator"/> is located.
    /// </summary>
    /// <param name="terminator">The terminator.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns></returns>
    public string TerminatedRead(string terminator, TimeSpan timeout)
    {
      return this.TerminatedRead(terminator, timeout, 1);
    }

    /// <summary>
    /// Reads asynchronously from the stream, terminating as soon as the <see cref="terminator"/> is located.
    /// </summary>
    /// <param name="terminator">The terminator.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <param name="millisecondSpin">The millisecond spin between each read from the stream.</param>
    /// <returns></returns>
    public string TerminatedRead(string terminator, TimeSpan timeout, int millisecondSpin)
    {
      DateTime endTimeout = DateTime.Now.Add(timeout);
      string s = string.Empty;
      while (!IsTerminatorLocated(terminator, s) && endTimeout >= DateTime.Now)
      {
        s += this.Read(TimeSpan.FromMilliseconds(1));
      }
      if (!IsTerminatorLocated(terminator, s))
      {
        System.Diagnostics.Debug.Print("Failed to terminate '{0}' with '{1)'", s, terminator);
      }
      return s;
    }
  }
}