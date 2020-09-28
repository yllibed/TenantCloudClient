using System;
using System.Net;

namespace Yllibed.TenantCloudClient
{
	public class TcClientException : Exception
	{
		public HttpStatusCode HttpStatus { get; }

		public TcClientException(HttpStatusCode httpStatus, string message, Exception? innerException = null) : base(message, innerException)
		{
			HttpStatus = httpStatus;
		}
	}
}
