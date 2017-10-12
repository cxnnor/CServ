using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace SharpSRV
{
	public class HTTPserver
	{
		private readonly HttpListener lsnr = new HttpListener();
		private readonly Func<HttpListenerRequest, string> rspn;

		public HTTPserver(IReadOnlyCollection<string> prefixes, Func<HttpListenerRequest, string> method)
		{
			if (!HttpListener.IsSupported)
			{
				throw new NotSupportedException("OS not supported");
			}

			if (prefixes == null || prefixes.Count == 0)
			{
				throw new ArgumentException("uri prefix required");
			}

			if (method == null)
			{
				throw new ArgumentException("responder method required");
			}

			foreach (var s in prefixes)
			{
				lsnr.Prefixes.Add(s);
			}

			rspn = method;
			lsnr.Start();
		}

		public HTTPserver()
		{

		}

		public HTTPserver(Func<HttpListenerRequest, string> method, params string[] prefixes)
		   : this(prefixes, method)
		{
		}

		public void Run()
		{
			ThreadPool.QueueUserWorkItem(o =>
			{
				Console.WriteLine("Webserver running...");
				try
				{
					while (lsnr.IsListening)
					{
						ThreadPool.QueueUserWorkItem(c =>
						{
							var ctx = c as HttpListenerContext;
							try
							{
								if (ctx == null)
								{
									return;
								}

                                var rstr = rspn(ctx.Request);
								var buf = Encoding.UTF8.GetBytes(rstr);
								ctx.Response.ContentLength64 = buf.Length;
								ctx.Response.OutputStream.Write(buf, 0, buf.Length);
							}
							catch
							{
								// ignore
							}
							finally
							{

								if (ctx != null)
								{
									ctx.Response.OutputStream.Close();
								}
							}
						}, lsnr.GetContext());
					}
				}
				catch (Exception ex)
				{
					// ignore
				}
			});
		}

		public void Stop()
		{
			lsnr.Stop();
			lsnr.Close();
		}
	}

	internal class Program
	{
		public static string SendResponse(HttpListenerRequest request)
		{
            return string.Format("<HTML><BODY>{1} is successfully running version {2} <br>The current date and time is {0}</BODY></HTML>", DateTime.Now, Constants.name, Constants.version);
		}

		public static void Main(string[] args)
		{
			HTTPserver ws = new HTTPserver(SendResponse, "http://localhost:8080/test/");

			ws.Run();
            Console.WriteLine("Server started on localhost:8080. Press a key to quit.");
			Console.ReadKey();
			ws.Stop();

		}
	}
}